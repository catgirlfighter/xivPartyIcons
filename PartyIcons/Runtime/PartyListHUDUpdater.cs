using System;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyIcons.Utils;

namespace PartyIcons.Runtime
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

        private bool _previousInParty = false;

        public PartyListHUDUpdater(Plugin plugin, PartyListHUDView view, RoleTracker roleTracker, Configuration configuration)
        {
            _view = view;
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
        }

        private void OnNetworkMessage(IntPtr dataptr, ushort opcode, uint sourceactorid, uint targetactorid, NetworkMessageDirection direction)
        {
            if (direction == NetworkMessageDirection.ZoneDown && opcode == 0x2ac && targetactorid == _clientState.LocalPlayer?.ObjectId)
            {
                PluginLog.Debug("PartyListHUDUpdater Forcing update due to zoning");
                UpdatePartyListHUD();
            }
        }

        private void OnUpdate(Framework framework)
        {
            var inParty = _partyList.Any();
            if (!inParty && _previousInParty)
            {
                PluginLog.Debug("No longer in party, reverting party list HUD changes");
                _view.RevertSlotNumbers();
            }

            _previousInParty = inParty;
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
                    _view.SetPartyMemberRole(index.Value, role);
                }
            }
        }
    }
}