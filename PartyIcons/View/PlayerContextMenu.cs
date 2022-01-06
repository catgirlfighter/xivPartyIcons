using System;
using Dalamud.Logging;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using XivCommon;
using XivCommon.Functions.ContextMenu;

namespace PartyIcons.View
{
    public sealed class PlayerContextMenu : IDisposable
    {
        private readonly XivCommonBase _base;
        private readonly RoleTracker _roleTracker;
        private readonly Configuration _configuration;

        public PlayerContextMenu(XivCommonBase @base, RoleTracker roleTracker, Configuration configuration)
        {
            _base = @base;
            _roleTracker = roleTracker;
            _configuration = configuration;
        }

        public void Enable()
        {
            _base.Functions.ContextMenu.OpenContextMenu += OnOpenContextMenu;
        }

        public void Disable()
        {
            _base.Functions.ContextMenu.OpenContextMenu -= OnOpenContextMenu;
        }

        public void Dispose()
        {
            Disable();
        }

        private void OnOpenContextMenu(ContextMenuOpenArgs args)
        {
            if (!IsMenuValid(args))
            {
                return;
            }
            
            if (_roleTracker.TryGetSuggestedRole(args.Text?.TextValue, args.ObjectWorld, out var role))
            {
                var roleName = PlayerStylesheet.GetRoleName(role, _configuration.EasternNamingConvention);
                args.Items.Add(new NormalContextMenuItem($"Assign to {roleName} (suggested)", (args) => OnAssignRole(args, role)));
            }
            
            if (_roleTracker.TryGetAssignedRole(args.Text?.TextValue, args.ObjectWorld, out var currentRole))
            {
                var swappedRole = RoleIdUtils.Counterpart(currentRole);
                var swappedRoleName = PlayerStylesheet.GetRoleName(swappedRole, _configuration.EasternNamingConvention);
                args.Items.Add(new NormalContextMenuItem($"Party role swap to {swappedRoleName}", (args) => OnAssignRole(args, swappedRole)));
            }

            args.Items.Add(new NormalContextSubMenuItem("Assign Party Role", args =>
            {
                foreach (var role in _roleTracker.GetAssignedRoleSet())
                {
                    args.Items.Add(new NormalContextMenuItem(">" + PlayerStylesheet.GetRoleName(role, _configuration.EasternNamingConvention), (args) => OnAssignRole(args, role)));
                }
                foreach (var role in _roleTracker.GetFirstUnassignedRoleSet())
                {
                    args.Items.Add(new NormalContextMenuItem(PlayerStylesheet.GetRoleName(role, _configuration.EasternNamingConvention), (args) => OnAssignRole(args, role)));
                }
                args.Items.Add(new NormalContextMenuItem("Return", (args) => { }));
            }));
        }
        
        private void OnAssignRole(ContextMenuItemSelectedArgs args, RoleId roleId)
        {
            _roleTracker.OccupyRole(args.Text?.TextValue, args.ObjectWorld, roleId);
            _roleTracker.CalculateUnassignedPartyRoles();
        }     
        private bool IsMenuValid(BaseContextMenuArgs args)
        {
            switch (args.ParentAddonName)
            {
                case null: // Nameplate/Model menu
                case "LookingForGroup":
                case "PartyMemberList":
                case "FriendList":
                case "FreeCompany":
                case "SocialList":
                case "ContactList":
                case "ChatLog":
                case "_PartyList":
                case "LinkShell":
                case "CrossWorldLinkshell":
                case "ContentMemberList": // Eureka/Bozja/...
                    return args.Text != null && args.ObjectWorld != 0 && args.ObjectWorld != 65535;

                default:
                    return false;
            }
        }
    }
}