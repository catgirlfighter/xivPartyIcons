using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using PartyIcons.Api;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.Utils;
using System.Linq;

namespace PartyIcons.View
{
    public sealed class NameplateView : IDisposable
    {

        private readonly Configuration _configuration;
        //private readonly PlayerStylesheet _stylesheet;
        private readonly RoleTracker _roleTracker;
        private readonly PartyListHUDView _partyListHudView;
        private readonly ObjectTable _objectTable;
        private readonly ClientState _clientState;


        private readonly IconSet _iconSet;
        private string iconPrefix = "   ";
        private string statIconPrefix = "   ";
        private readonly int[] _ignorableStates = { 061521, 061522, 061523, 061540, 061542, 061543, 061544, 061547 };
        private readonly float[] _iconSetScale = { 1.2f, 0.8f, 0.75f };
        private readonly Vector2[] _iconSetOffset = { new Vector2(-2, -5), new Vector2(3, 0), new Vector2(3, 2) };

        public NameplateMode PartyMode { get; set; }
        public NameplateMode OthersMode { get; set; }


        public NameplateView(Plugin plugin, RoleTracker roleTracker, Configuration configuration, PartyListHUDView partyListHudView)
        {
            _roleTracker = roleTracker;
            _configuration = configuration;
            //_stylesheet = stylesheet;
            _partyListHudView = partyListHudView;
            _iconSet = new IconSet();
            _objectTable = plugin.ObjectTable;
            _clientState = plugin.ClientState;
        }

        public void Dispose()
        {
        }

        public void SetupDefault(XivApi.SafeNamePlateObject npObject)
        {
            npObject.SetIconScale(1f);
            npObject.SetNameScale(0.5f);
        }

        public void PresetupForPC(XivApi.SafeNamePlateObject npObject, int originalIconId)
        {
            var mode = GetModeForNameplate(npObject);
            switch (mode)
            {
                case NameplateMode.JobIcon:
                case NameplateMode.JobIconAndPartySlot:
                case NameplateMode.RoleLetters:
                case NameplateMode.JobIconAndRoleLettersUncolored:
                    switch (_configuration.SizeMode)
                    {
                        case NameplateSizeMode.Basic:
                        case NameplateSizeMode.Custom:
                            iconPrefix = "   ";
                            statIconPrefix = "   ";
                            break;

                        case NameplateSizeMode.Large:
                            iconPrefix = "    ";
                            statIconPrefix = " ";
                            break;

                        case NameplateSizeMode.Larger:
                            iconPrefix = "      ";
                            statIconPrefix = "  ";
                            break;
                    }
                    break;

                default:
                    iconPrefix = "   ";
                    statIconPrefix = "   ";
                    break;
            }
        }
        public void SetupForPC(XivApi.SafeNamePlateObject npObject, int originalIconId)
        {
            bool offset = true;
            float nameScale = 0.5f;
            float iconScale;
            Vector2 coords;
            var mode = GetModeForNameplate(npObject);
            var adj = mode != NameplateMode.RoleLetters;
            if (mode == NameplateMode.Default)
            {
                iconScale = 1f;
                coords = new Vector2(0, 0);
            }
            else if (!IsIgnorableStatus(originalIconId) || !adj)
            {
                iconScale = 1f;
                coords = new Vector2(10, 0);
            }
            else
            {
                iconScale = _iconSetScale[(int)_configuration.IconSetId];
                coords = new Vector2(13, 2);// + _iconSetOffset[(int)_configuration.IconSetId];
            }

            switch (mode)
            {
                case NameplateMode.Default:
                    break;

                case NameplateMode.JobIconAndName:
                    coords += adj ? _iconSetOffset[(int)_configuration.IconSetId] : new Vector2(0, 0);
                    break;

                case NameplateMode.JobIcon:
                case NameplateMode.JobIconAndPartySlot:
                case NameplateMode.RoleLetters:
                case NameplateMode.JobIconAndRoleLettersUncolored:
                    switch (_configuration.SizeMode)
                    {
                        case NameplateSizeMode.Basic:
                        case NameplateSizeMode.Custom:
                            coords += adj ? _iconSetOffset[(int)_configuration.IconSetId] : new Vector2(0, 0);
                            iconScale *= 1f;
                            nameScale = 0.5f;
                            break;

                        case NameplateSizeMode.Large:
                            coords += (adj ? _iconSetOffset[(int)_configuration.IconSetId] * 1.5f : new Vector2(0, 0)) + new Vector2(-8, -18);
                            iconScale *= 1.5f;
                            nameScale = 0.75f;
                            break;

                        case NameplateSizeMode.Larger:
                            coords += (adj ? _iconSetOffset[(int)_configuration.IconSetId] * 2.5f : new Vector2(0, 0)) + new Vector2(-20, -45);
                            iconScale *= 2.5f;
                            nameScale = 1f;
                            break;
                    }
                    break;
            }

            if (offset)
                npObject.AdjustIconPosition((short)coords.X, (short)coords.Y);
            else
               npObject.SetIconPosition((short)coords.X, (short)coords.Y);
            npObject.SetIconScale(iconScale);
            npObject.SetNameScale(nameScale);
        }

        public void NameplateDataForPC(
            XivApi.SafeNamePlateObject npObject,
            ref bool isPrefixTitle,
            ref bool displayTitle,
            ref IntPtr title,
            ref IntPtr name,
            ref IntPtr fcName,
            ref int iconID
        )
        {
            var namePlateInfo = npObject.NamePlateInfo;
            var uid = namePlateInfo == null ? 0 : namePlateInfo.Data.ObjectID.ObjectID;
            var mode = GetModeForNameplate(npObject);

            if (_configuration.HideLocalPlayerNameplate && uid == _clientState.LocalPlayer?.ObjectId)
            {
                switch (mode)
                {
                    case NameplateMode.Default:
                    case NameplateMode.JobIconAndName:
                    case NameplateMode.JobIcon:
                    case NameplateMode.JobIconAndPartySlot:
                        name = SeStringUtils.emptyPtr;
                        fcName = SeStringUtils.emptyPtr;
                        displayTitle = false;
                        iconID = -1;
                        return;

                    case NameplateMode.RoleLetters:
                        if (!_configuration.TestingMode && (namePlateInfo == null || !namePlateInfo.IsPartyMember()))
                        {
                            name = SeStringUtils.emptyPtr;
                            fcName = SeStringUtils.emptyPtr;
                            displayTitle = false;
                            iconID = -1;
                            return;
                        }
                        break;
                }
            }

            var playerCharacter = _objectTable.SearchById(uid) as PlayerCharacter;
            if (playerCharacter == null)
            {
                return;
            }

            var hasRole = _roleTracker.TryGetAssignedRole(playerCharacter.Name.TextValue, playerCharacter.HomeWorld.Id, out var roleId);
            switch (mode)
            {
                case NameplateMode.Default:
                    break;

                case NameplateMode.JobIconAndName:
                    name = GetStateNametext(_configuration.ShowPlayerStatus ? iconID : -1, iconPrefix, statIconPrefix, SeStringUtils.SeStringFromPtr(name));
                    iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1);
                    break;

                case NameplateMode.JobIcon:
                    name = GetStateNametext(_configuration.ShowPlayerStatus ? iconID : -1, iconPrefix, statIconPrefix);
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1);
                    break;

                case NameplateMode.JobIconAndPartySlot:
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    var partySlot = _partyListHudView.GetPartySlotIndex(namePlateInfo == null ? 0 : namePlateInfo.Data.ObjectID.ObjectID) + 1;
                    if (partySlot != null || hasRole)
                    {
                        var genericRole = JobExtensions.GetRole((Job)(namePlateInfo == null ? 0 : namePlateInfo.GetJobID()));
                        SeString? text;// = SeStringUtils.Text(" ");
                        if (partySlot == null)
                            text = SeStringUtils.Color(PlayerStylesheet.GetRolePlateNumber(roleId, _configuration.EasternNamingConvention), PlayerStylesheet.GetRoleColor(genericRole));
                        else
                            text = SeStringUtils.Color(PlayerStylesheet.GetPartySlotNumber(partySlot.Value, genericRole), PlayerStylesheet.GetRoleColor(genericRole));
                        name = GetStateNametext(_configuration.ShowPlayerStatus ? iconID : -1, iconPrefix, statIconPrefix, text);
                        iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1);
                    }
                    else
                    {
                        name = GetStateNametext(_configuration.ShowPlayerStatus ? iconID : -1, iconPrefix, statIconPrefix);
                        iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1);
                    }
                    break;

                case NameplateMode.RoleLetters:
                    if (hasRole)
                    {
                        var text = SeStringUtils.Color(PlayerStylesheet.GetRolePlate(roleId, _configuration.EasternNamingConvention), PlayerStylesheet.GetRoleColor(roleId));
                        name = GetStateNametext(-1, _configuration.ShowPlayerStatus ? iconPrefix : null, _configuration.ShowPlayerStatus ? statIconPrefix : null, text);
                    }
                    else
                    {
                        var genericRole = JobExtensions.GetRole((Job)(namePlateInfo == null ? 0 : namePlateInfo.GetJobID()));
                        var text = SeStringUtils.Color(PlayerStylesheet.GetRolePlate(genericRole, _configuration.EasternNamingConvention), PlayerStylesheet.GetRoleColor(genericRole));
                        name = GetStateNametext(-1, _configuration.ShowPlayerStatus ? iconPrefix : null, _configuration.ShowPlayerStatus ? statIconPrefix : null, text);
                    }
                    if (!_configuration.ShowPlayerStatus) iconID = -1;

                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    break;

                case NameplateMode.JobIconAndRoleLettersUncolored:
                    if (hasRole)
                    {
                        name = GetStateNametext(iconID, iconPrefix, statIconPrefix, PlayerStylesheet.GetRolePlate(roleId, _configuration.EasternNamingConvention));
                    }
                    else
                    {
                        var genericRole = JobExtensions.GetRole((Job)(namePlateInfo == null ? 0 : namePlateInfo.GetJobID()));
                        name = GetStateNametext(iconID, iconPrefix, statIconPrefix, PlayerStylesheet.GetRolePlate(genericRole, _configuration.EasternNamingConvention));
                    }

                    iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1);
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    break;
            }
        }

        private int GetClassIcon(XivApi.SafeNamePlateInfo? info, int def = -1)
        {
            if (info == null || def != -1 && !_ignorableStates.Contains(def))
                return def;

            var genericRole = JobExtensions.GetRole((Job)info.GetJobID());
            var iconSet = PlayerStylesheet.GetGenericRoleIconset(genericRole, _configuration.IconSetId);
            return _iconSet.GetJobIcon(iconSet, info.GetJobID());
        }

        private bool IsIgnorableStatus(int statusIcon)
        {
            return statusIcon == -1 || _ignorableStates.Contains(statusIcon);
        }

        private SeString GetStateNametextS(int iconId, string? prefix = null, string? statprefix = null, SeString? append = null)
        {

            SeString? val = iconId switch
            {
                //061521 - party leader
                //061522 - party member
                061523 => SeStringUtils.Icon(BitmapFontIcon.NewAdventurer, statprefix),
                061540 => SeStringUtils.Icon(BitmapFontIcon.Mentor, statprefix),
                061542 => SeStringUtils.Icon(BitmapFontIcon.MentorPvE, statprefix),
                061543 => SeStringUtils.Icon(BitmapFontIcon.MentorCrafting, statprefix),
                061544 => SeStringUtils.Icon(BitmapFontIcon.MentorPvP, statprefix),
                061547 => SeStringUtils.Icon(BitmapFontIcon.Returner, statprefix),
                _ => null,
            };

            return append == null ? val ?? SeString.Empty : val == null ? prefix == null ? append : SeStringUtils.Text(prefix).Append(append) : val.Append(append);
        }

        private IntPtr GetStateNametext(int iconId, string? prefix = null, string? statprefix = null, SeString? append = null)
        {
            return SeStringUtils.SeStringToPtr(GetStateNametextS(iconId, prefix, statprefix, append));
        }

        private NameplateMode GetModeForNameplate(XivApi.SafeNamePlateObject npObject)
        {
            var namePlateInfo = npObject.NamePlateInfo;
            var uid = namePlateInfo == null ? 0 : namePlateInfo.Data.ObjectID.ObjectID;
            if (_configuration.TestingMode || namePlateInfo != null && namePlateInfo.IsPartyMember() || uid == _clientState.LocalPlayer?.ObjectId)
            {
                return PartyMode;
            }
            else
            {
                return OthersMode;
            }
        }
    }
}