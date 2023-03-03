using System;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyNamplates.Utils;

namespace PartyNamplates.Runtime
{
    public sealed class PartyListHUDUpdater : IDisposable
    {
        public bool UpdateHUD = false;

        private readonly PartyList _partyList;
        private readonly Framework _framework;
        private readonly GameNetwork _gameNetwork;
        private readonly ClientState _clientState;

        private readonly Configuration _configuration;
        private readonly PartyListHUDView _view;
        private readonly RoleTracker _roleTracker;

        private int _previousPartySize = 0;

        public PartyListHUDUpdater(Plugin plugin, PartyListHUDView partyListHUDView, RoleTracker roleTracker, Configuration configuration)
        {
            _view = partyListHUDView;
            _roleTracker = roleTracker;
            _configuration = configuration;
            _partyList = plugin.PartyList;
            _framework = plugin.Framework;
            _gameNetwork = plugin.GameNetwork;
            _clientState = plugin.ClientState;
        }

        public void Enable()
        {
            _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;
            _framework.Update += OnUpdate;
            _gameNetwork.NetworkMessage += OnNetworkMessage;
        }

        public void Dispose()
        {
            _gameNetwork.NetworkMessage -= OnNetworkMessage;
            _framework.Update -= OnUpdate;
            _roleTracker.OnAssignedRolesUpdated -= OnAssignedRolesUpdated;
        }

        private void OnAssignedRolesUpdated()
        {
            PluginLog.Debug("PartyListHUDUpdater forcing update due to assignments update");
            UpdatePartyListHUD();
            //_roleTracker.CalculateUnassignedPartyRoles();
        }

        private void OnNetworkMessage(IntPtr dataptr, ushort opcode, uint sourceactorid, uint targetactorid, NetworkMessageDirection direction)
        {
            if (direction == NetworkMessageDirection.ZoneDown && opcode == 0x2ac && targetactorid == _clientState.LocalPlayer?.ObjectId)
            {
                PluginLog.Debug("PartyListHUDUpdater Forcing update due to zoning");
                UpdatePartyListHUD();
                PluginLog.Log($"OnNetworkMessage");
                 //_roleTracker.CalculateUnassignedPartyRoles();
            }
        }

        private void OnUpdate(Framework framework)
        {
            var partySize = _partyList.Count();
            if (partySize == 0 && _previousPartySize > 0)
            {
                PluginLog.Debug("No longer in party, reverting party list HUD changes");
                _view.RevertSlotNumbers();
            }

            if (partySize != _previousPartySize)
            {
                PluginLog.Log($"OnUpdate");
                //_roleTracker.CalculateUnassignedPartyRoles();
            }

            _previousPartySize = partySize;
        }

        private void UpdatePartyListHUD()
        {
            if (!_configuration.DisplayRoleInPartyList)
            {
                return;
            }

            if (!UpdateHUD)
            {
                return;
            }

            PluginLog.Debug("Updating party list HUD");
            foreach (var member in _partyList)
            {
                var index = _view.GetPartySlotIndex(member.ObjectId);
                if (index != null && _roleTracker.TryGetAssignedRole(member.Name.ToString(), member.World.Id, out var role))
                {
                    PluginLog.Debug($"Updating party list hud: member {member.Name} index {index} to {role}");
                    if (index != null) _view.SetPartyMemberRole(index.Value, role);
                }
            }
        }
    }
}