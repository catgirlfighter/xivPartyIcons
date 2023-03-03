using System;
using XivCommon;
using Dalamud.Game.Gui.Toast;
using PartyNamplates.Entities;
using PartyNamplates.Runtime;
using PartyNamplates.Stylesheet;

namespace PartyNamplates.View
{
    //xivcommon removed support of context menus in 5.0. It was buggy anyway, so loooking for an alternative for now
    /*
    public sealed class PlayerContextMenu : IDisposable
    {
        private readonly XivCommonBase _base;
        private readonly RoleTracker _roleTracker;
        private readonly Configuration _configuration;
        private readonly ToastGui _toastGui;

        public PlayerContextMenu(Plugin plugin, XivCommonBase xivCommonBase, RoleTracker roleTracker, Configuration configuration)
        {
            _base = xivCommonBase;
            _roleTracker = roleTracker;
            _configuration = configuration;
            _toastGui = plugin.ToastGui;
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
                args.Items.Add(new NormalContextMenuItem("Reset", (args) => { _roleTracker.ResetOccupations(); _toastGui.ShowNormal($"Occupations reset"); }));
                args.Items.Add(new NormalContextMenuItem("_return", (args) => { }));
            }));
        }
        
        private void OnAssignRole(ContextMenuItemSelectedArgs args, RoleId roleId)
        {
            _roleTracker.OccupyRole(args.Text?.TextValue, args.ObjectWorld, roleId);
            _roleTracker.CalculateUnassignedPartyRoles();
        }     
        private static bool IsMenuValid(BaseContextMenuArgs args)
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
    */
}