using System;
using System.Linq;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using PartyIcons.Stylesheet;
using PartyIcons.View;
using PartyIcons.Utils;

namespace PartyIcons.Runtime
{
    public sealed class ChatNameUpdater : IDisposable
    {
        private readonly ClientState _clientState;
        private readonly PartyList _partyList;
        private readonly ObjectTable _objectTable;
        private readonly ChatGui _chatGui;
        private readonly Configuration _configuration;

        private readonly RoleTracker _roleTracker;
        //private readonly PlayerStylesheet _stylesheet;

        public ChatMode PartyMode { get; set; }
        public ChatMode OthersMode { get; set; }

        public ChatNameUpdater(Plugin plugin, RoleTracker roleTracker, Configuration configuration)
        {
            _roleTracker = roleTracker;
            //_stylesheet = stylesheet;
            _clientState = plugin.ClientState;
            _partyList = plugin.PartyList;
            _objectTable = plugin.ObjectTable;
            _chatGui = plugin.ChatGui;
            _configuration = configuration;
        }

        public void Enable()
        {
            _chatGui.ChatMessage += OnChatMessage;
        }

        private void OnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled)
        {
            if (type == XivChatType.Say || type == XivChatType.Party || type == XivChatType.Alliance || type == XivChatType.Shout || type == XivChatType.Yell)
            {
                Parse(type, ref sender);
            }
        }

        public void Disable()
        {
            _chatGui.ChatMessage -= OnChatMessage;
        }

        public void Dispose()
        {
            Disable();
        }

        private PlayerPayload GetPlayerPayload(SeString sender)
        {
            var playerPayload = sender.Payloads.FirstOrDefault(p => p is PlayerPayload) as PlayerPayload;
            if (playerPayload == null)
            {
                if (_clientState.LocalPlayer == null)
                    playerPayload = new PlayerPayload("", 0);
                else
                    playerPayload = new PlayerPayload(_clientState.LocalPlayer.Name.TextValue, _clientState.LocalPlayer.HomeWorld.Id);
            }

            return playerPayload;
        }

        private bool CheckIfPlayerPayloadInParty(PlayerPayload playerPayload)
        {
            foreach (var member in _partyList)
            {
                if (member.Name.ToString() == playerPayload.PlayerName && member.World.Id == playerPayload.World.RowId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool GetAndRemovePartyNumberPrefix(XivChatType type, SeString sender, out string prefix)
        {
            if (type == XivChatType.Party || type == XivChatType.Alliance)
            {
                var playerNamePayload = sender.Payloads.FirstOrDefault(p => p is TextPayload) as TextPayload;
                if (playerNamePayload == null)
                {
                    prefix = "";
                    return false;
                }
                prefix = playerNamePayload.Text.Substring(0, 1);
                playerNamePayload.Text = playerNamePayload.Text.Substring(1);

                return true;
            }
            else
            {
                prefix = "";
                return false;
            }
        }

        private void RemoveExistingForeground(SeString str)
        {
            str.Payloads.RemoveAll(p => p.Type == PayloadType.UIForeground);
        }

        private ClassJob? FindSenderJob(PlayerPayload playerPayload)
        {
            ClassJob? senderJob = null;
            foreach (var member in _partyList)
            {
                if (member.Name.ToString() == playerPayload.PlayerName && member.World.Id == playerPayload.World.RowId)
                {
                    senderJob = member.ClassJob.GameData;
                    break;
                }
            }

            if (senderJob == null)
            {
                foreach (var obj in _objectTable)
                {
                    if (obj is PlayerCharacter pc && pc.Name.ToString() == playerPayload.PlayerName && pc.HomeWorld.Id == playerPayload.World.RowId)
                    {
                        senderJob = pc.ClassJob.GameData;
                        break;
                    }
                }
            }

            return senderJob;
        }

        private void Parse(XivChatType chatType, ref SeString sender)
        {
            var playerPayload = GetPlayerPayload(sender);

            var mode = CheckIfPlayerPayloadInParty(playerPayload) ? PartyMode : OthersMode;
            if (mode == ChatMode.Role && _roleTracker.TryGetAssignedRole(playerPayload.PlayerName, playerPayload.World.RowId, out var roleId))
            {
                RemoveExistingForeground(sender);
                GetAndRemovePartyNumberPrefix(chatType, sender, out _);

                var prefixString = new SeString();
                prefixString.Append(new UIForegroundPayload(PlayerStylesheet.GetRoleChatColor(roleId)));
                prefixString.Append(PlayerStylesheet.GetRoleChatPrefix(roleId, _configuration.EasternNamingConvention));
                prefixString.Append(new TextPayload(" "));

                sender.Payloads.InsertRange(0, prefixString.Payloads);
                sender.Payloads.Add(UIForegroundPayload.UIForegroundOff);
            }
            else if (mode != ChatMode.GameDefault)
            {
                var senderJob = FindSenderJob(playerPayload);
                if (senderJob == null || senderJob.RowId == 0)
                {
                    return;
                }

                RemoveExistingForeground(sender);
                GetAndRemovePartyNumberPrefix(chatType, sender, out var numberPrefix);

                var prefixString = new SeString();
                switch (mode)
                {
                    case ChatMode.Job:
                        prefixString.Append(new UIForegroundPayload(PlayerStylesheet.GetJobChatColor(senderJob)));
                        if (numberPrefix.Length > 0)
                        {
                            prefixString.Append(new TextPayload(numberPrefix));
                        }
                        prefixString.Append(PlayerStylesheet.GetJobChatPrefix(senderJob).Payloads);
                        prefixString.Append(new TextPayload(" "));
                        break;

                    case ChatMode.Role:
                        prefixString.Append(new UIForegroundPayload(PlayerStylesheet.GetGenericRoleChatColor(senderJob)));
                        if (numberPrefix.Length > 0)
                        {
                            prefixString.Append(new TextPayload(numberPrefix));
                        }
                        prefixString.Append(PlayerStylesheet.GetGenericRoleChatPrefix(senderJob, _configuration.EasternNamingConvention).Payloads);
                        prefixString.Append(new TextPayload(" "));
                        break;

                    case ChatMode.OnlyColor:
                        prefixString.Append(new UIForegroundPayload(PlayerStylesheet.GetGenericRoleChatColor(senderJob)));
                        if (numberPrefix.Length > 0)
                        {
                            prefixString.Append(new TextPayload(numberPrefix));
                        }
                        prefixString.Append(new TextPayload(" "));
                        break;

                    default:
                        throw new ArgumentException($"unknown ChatMode({mode})");
                }

                sender.Payloads.InsertRange(0, prefixString.Payloads);
                sender.Payloads.Add(UIForegroundPayload.UIForegroundOff);
            }
        }
    }
}