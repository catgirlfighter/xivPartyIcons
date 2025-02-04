﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using PartyNamplates.Entities;

namespace PartyNamplates.Runtime
{
    public sealed class RoleTracker : IDisposable
    {
        public event Action<string, RoleId>? OnRoleOccupied;
        public event Action<string, RoleId>? OnRoleSuggested;
        public event Action? OnAssignedRolesUpdated;

        private readonly Framework _framework;
        private readonly ChatGui _chatGui;
        private readonly ClientState _clientState;
        private readonly Condition _condition;
        private readonly PartyList _partyList;
        private readonly ToastGui _toastGui;

        private readonly Configuration _configuration;

        //private bool _currentlyInParty;
        //private uint _territoryId;
        private int _previousStateHash;

        private readonly List<(RoleId, string)> _occupationMessages = new();
        private readonly List<(RoleId, Regex)> _suggestionRegexes = new();

        private readonly Dictionary<string, RoleId> _occupiedRoles = new();
        private readonly Dictionary<string, RoleId> _assignedRoles = new();
        private readonly Dictionary<string, RoleId> _suggestedRoles = new();
        private readonly HashSet<RoleId> _unassignedRoles = new();
        private bool _prevEastern = false;

        public RoleTracker(Plugin plugin, Configuration configuration)
        {
            _framework = plugin.Framework;
            _chatGui = plugin.ChatGui;
            _clientState = plugin.ClientState;
            _condition = plugin.Condition;
            _partyList = plugin.PartyList;
            _toastGui = plugin.ToastGui;
            _configuration = configuration;
            foreach (var role in Enum.GetValues<RoleId>())
            {
                var roleIdentifier = role.ToString().ToLower();
                var regex = new Regex($"\\W{roleIdentifier}\\W");

                _occupationMessages.Add((role, $" {roleIdentifier} "));
                _suggestionRegexes.Add((role, regex));
            }

            _occupationMessages.Add((RoleId.OT, " st "));
            _suggestionRegexes.Add((RoleId.OT, new Regex("\\Wst\\W")));

            for (var i = 1; i < RoleIdUtils.ROLE_COUNT + 1; i++)
            {
                var roleId = RoleId.M1 + i - 1;
                _occupationMessages.Add((roleId, $" d{i} "));
                _suggestionRegexes.Add((roleId, new Regex($"\\Wd{i}\\W")));
            }
        }

        public void Enable()
        {
            _chatGui.ChatMessage += OnChatMessage;
            _framework.Update += FrameworkOnUpdate;
        }

        public void Disable()
        {
            _chatGui.ChatMessage -= OnChatMessage;
            _framework.Update -= FrameworkOnUpdate;
        }

        public void Dispose()
        {
            Disable();
        }

        public bool TryGetSuggestedRole(string? name, uint worldId, out RoleId roleId)
        {
            if (name == null)
            {
                roleId = 0;
                return false;
            }
            else
                return _suggestedRoles.TryGetValue(PlayerId(name, worldId), out roleId);
        }

        public bool TryGetAssignedRole(string? name, uint worldId, out RoleId roleId)
        {
            if (name == null)
            {
                roleId = 0;
                return false;
            }
            else
                return _assignedRoles.TryGetValue(PlayerId(name, worldId), out roleId);
        }

        public HashSet<RoleId> GetAssignedRoleSet()
        {
            HashSet<RoleId> result = new();
            foreach (var val in _assignedRoles)
                result.Add(val.Value);
            return result;
        }

        public HashSet<RoleId> GetFirstUnassignedRoleSet()
        {
            HashSet<RoleId> result = new();
            foreach (var role in Enum.GetValues<GenericRole>())
            {
                var val = FindUnassignedRoleForGenericRole(role);
                if (val != RoleId.Undefined)
                    result.Add(val);
            }
            return result;
        }

        public void OccupyRole(string? name, uint world, RoleId roleId)
        {
            if (name == null || name == "")
                return;
 
            var val = _occupiedRoles.FirstOrDefault(x => x.Value == roleId);
            if (val.Key != null) _occupiedRoles.Remove(val.Key);

            _occupiedRoles[PlayerId(name, world)] = roleId;
            OnRoleOccupied?.Invoke(name, roleId);
            _toastGui.ShowNormal($"{name} occupation assigned to {roleId}");
        }

        public void SuggestRole(string name, uint world, RoleId roleId)
        {
            _suggestedRoles[PlayerId(name, world)] = roleId;
            OnRoleSuggested?.Invoke(name, roleId);
        }

        public void ResetOccupations()
        {
            PluginLog.Debug("Resetting occupation");
            _occupiedRoles.Clear();
        }

        public void ResetAssignments()
        {
            PluginLog.Debug("Resetting assignments");
            _assignedRoles.Clear();
            _unassignedRoles.Clear();

            foreach (var role in Enum.GetValues<RoleId>())
            {
                if (role != default)
                {
                    _unassignedRoles.Add(role);
                }
            }
        }

        public void CalculateUnassignedPartyRoles()
        {
            try
            {
                ResetAssignments();

                PluginLog.Debug($"Assigning current occupations ({_occupiedRoles.Count})");
                foreach (var kv in _occupiedRoles)
                {
                    PluginLog.Debug($"{kv.Key} == {kv.Value} as per occupation");

                    _assignedRoles[kv.Key] = kv.Value;
                    _unassignedRoles.Remove(kv.Value);
                }

                PluginLog.Debug($"Assigning static assignments ({_configuration.StaticAssignments.Count})");
                foreach (var kv in _configuration.StaticAssignments)
                {
                    foreach (var member in _partyList)
                    {
                        var playerId = PlayerId(member);
                        if (_assignedRoles.ContainsKey(playerId))
                        {
                            PluginLog.Debug($"{PlayerId(member)} has already been assigned a role");
                            continue;
                        }

                        var playerDescription = $"{member.Name}@{member?.World?.GameData?.Name}";
                        if (kv.Key.Equals(playerDescription))
                        {
                            var applicableRoles = GetApplicableRolesForGenericRole(JobRoleExtensions.RoleFromByte(member?.ClassJob?.GameData?.Role ?? 0), _configuration.EasternNamingConvention);
                            if (applicableRoles.Contains(kv.Value))
                            {
                                PluginLog.Debug($"{playerId} == {kv.Value} as per static assignments {playerDescription}");
                                _assignedRoles[playerId] = kv.Value;
                            }
                            else
                            {
                                PluginLog.Debug($"Skipping static assignment - applicable roles {string.Join(", ", applicableRoles)}, static role - {kv.Value}");
                            }
                        }
                    }
                }

                PluginLog.Debug("Assigning the rest");
                foreach (var member in _partyList)
                {
                    if (_assignedRoles.ContainsKey(PlayerId(member)))
                    {
                        PluginLog.Debug($"{PlayerId(member)} has already been assigned a role");
                        continue;
                    }

                    RoleId roleToAssign = FindUnassignedRoleForGenericRole(JobRoleExtensions.RoleFromByte(member?.ClassJob?.GameData?.Role ?? 0));
                    if (roleToAssign != default)
                    {
                        PluginLog.Debug($"{PlayerId(member)} == {roleToAssign} as per first available");
                        _assignedRoles[PlayerId(member)] = roleToAssign;
                        _unassignedRoles.Remove(roleToAssign);
                    }
                }

                OnAssignedRolesUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                PluginLog.Log($"CalculateUnassignedPartyRoles: {ex.Message}");
            }
        }

        public string DebugDescription()
        {
            var sb = new StringBuilder();
            sb.Append($"Assignments:\n");
            foreach (var kv in _assignedRoles)
            {
                sb.Append($"Role {kv.Value} assigned to {kv.Key}\n");
            }

            sb.Append($"\nOccupations:\n");
            foreach (var kv in _occupiedRoles)
            {
                sb.Append($"Role {kv.Value} occupied by {kv.Key}\n");
            }

            sb.Append("\nUnassigned roles:\n");

            foreach (var k in _unassignedRoles)
            {
                sb.Append(" " + k);
            }

            return sb.ToString();
        }

        private void FrameworkOnUpdate(Framework framework)
        {
            if (!_condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance] && _partyList.Length == 0 && _occupiedRoles.Any())
            {
                PluginLog.Debug("Resetting occupations, no longer in a party");
                ResetOccupations();
                return;
            }

            if (_prevEastern != _configuration.EasternNamingConvention)
            {
                _previousStateHash = default;
                _prevEastern = _configuration.EasternNamingConvention;
            }

            var partyHash = 17;
            foreach (var member in _partyList)
            {
                unchecked
                {
                    //partyHash = partyHash * 23 + (int)member.ObjectId;
                    partyHash = partyHash * 40 + (int)member.ClassJob.Id/*(int)member.ObjectId*/;
                }
            }

            if (partyHash != _previousStateHash)
            {
                PluginLog.Debug($"Party hash changed ({partyHash}, prev {_previousStateHash}), recalculating roles");
                CalculateUnassignedPartyRoles();
            }

            _previousStateHash = partyHash;
        }

        private static string PlayerId(string name, uint worldId)
        {
            return $"{name}@{worldId}";
        }

        private static string PlayerId(PartyMember? member)
        {
            return $"{member?.Name?.TextValue}@{member?.World.Id}";
        }

        private RoleId FindUnassignedRoleForGenericRole(GenericRole role)
        {
            var applicableRoles = GetApplicableRolesForGenericRole(role, _configuration.EasternNamingConvention);
            return applicableRoles.FirstOrDefault(r => _unassignedRoles.Contains(r));
        }

        private static IEnumerable<RoleId> GetApplicableRolesForGenericRole(GenericRole role, bool eastern)
        {
            if (eastern)
                return role switch
                {
                    GenericRole.Tank => new[] { RoleId.MT, RoleId.OT, RoleId.T3, RoleId.T4, RoleId.T5, RoleId.T6, RoleId.T7, RoleId.T8 },
                    GenericRole.Healer => new[] { RoleId.H1, RoleId.H2, RoleId.H3, RoleId.H4, RoleId.H5, RoleId.H6, RoleId.H7, RoleId.H8 },
                    GenericRole.Melee or GenericRole.Ranged => new[] { RoleId.M1, RoleId.M2, RoleId.M3, RoleId.M4, RoleId.M5, RoleId.M6, RoleId.M7, RoleId.M8,
                            RoleId.R1, RoleId.R2, RoleId.R3, RoleId.R4, RoleId.R5, RoleId.R6, RoleId.R7, RoleId.R8 },
                    _ => new[] { RoleId.Undefined },
                };
            else
                return role switch
                {
                    GenericRole.Tank => new[] { RoleId.MT, RoleId.OT, RoleId.T3, RoleId.T4, RoleId.T5, RoleId.T6, RoleId.T7, RoleId.T8 },
                    GenericRole.Healer => new[] { RoleId.H1, RoleId.H2, RoleId.H3, RoleId.H4, RoleId.H5, RoleId.H6, RoleId.H7, RoleId.H8 },
                    GenericRole.Melee => new[] { RoleId.M1, RoleId.M2, RoleId.M3, RoleId.M4, RoleId.M5, RoleId.M6, RoleId.M7, RoleId.M8 },
                    GenericRole.Ranged => new[] { RoleId.R1, RoleId.R2, RoleId.R3, RoleId.R4, RoleId.R5, RoleId.R6, RoleId.R7, RoleId.R8 },
                    _ => new[] { RoleId.Undefined },
                };
        }

        private void OnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled)
        {
            if (type == XivChatType.Party || type == XivChatType.CrossParty || type == XivChatType.Say)
            {
                string? playerName = null;
                uint? playerWorld = null;

                var playerPayload = sender.Payloads.FirstOrDefault(p => p is PlayerPayload) as PlayerPayload;
                if (playerPayload == null)
                {
                    playerName = _clientState.LocalPlayer?.Name.TextValue;
                    playerWorld = _clientState.LocalPlayer?.HomeWorld.Id;
                }
                else
                {
                    playerName = playerPayload?.PlayerName;
                    playerWorld = playerPayload?.World.RowId;
                }

                if (playerName == null || !playerWorld.HasValue)
                {
                    PluginLog.Debug($"Failed to get player data from {senderid}, {sender} ({sender.Payloads})");
                    return;
                }

                var text = message.TextValue.Trim().ToLower();
                var paddedText = $" {text} ";

                var roleToOccupy = RoleId.Undefined;
                var occupationTainted = false;
                var roleToSuggest = RoleId.Undefined;
                var suggestionTainted = false;

                foreach (var tuple in _occupationMessages)
                {
                    if (tuple.Item2.Equals(paddedText))
                    {
                        PluginLog.Debug($"Message contained role occupation ({playerName}@{playerWorld} - {text}, detected role {tuple.Item1})");

                        if (roleToOccupy == RoleId.Undefined)
                        {
                            roleToOccupy = tuple.Item1;
                        }
                        else
                        {
                            PluginLog.Debug($"Multiple role occupation matches, aborting");
                            occupationTainted = true;
                            break;
                        }
                    }
                }

                foreach (var tuple in _suggestionRegexes)
                {
                    if (tuple.Item2.IsMatch(paddedText))
                    {
                        PluginLog.Debug($"Message contained role suggestion ({playerName}@{playerWorld}: {text}, detected {tuple.Item1}");

                        if (roleToSuggest == RoleId.Undefined)
                        {
                            roleToSuggest = tuple.Item1;
                        }
                        else
                        {
                            PluginLog.Debug("Multiple role suggesting matches, aborting");
                            suggestionTainted = true;
                            break;
                        }
                    }
                }

                if (!occupationTainted && roleToOccupy != RoleId.Undefined)
                {
                    OccupyRole(playerName, playerWorld.Value, roleToOccupy);

                    PluginLog.Debug($"Recalculating assignments due to new occupations");
                    CalculateUnassignedPartyRoles();
                }
                else if (!suggestionTainted && roleToSuggest != RoleId.Undefined)
                {
                    SuggestRole(playerName, playerWorld.Value, roleToSuggest);
                }
            }
        }
    }
}