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
        private const string _iconPrefix = "   ";
        private readonly int[] _ignorableStates = { 061521, 061522, 061523, 061540, 061542, 061543, 061544, 061547 };

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

        public void SetupForPC(XivApi.SafeNamePlateObject npObject, int oldIconId)
        {
            var nameScale = 0.75f;
            var iconScale = 1f;
            var iconOffset = new Vector2(0, 0);

            var mode = GetModeForNameplate(npObject);
            switch (mode)
            {
                case NameplateMode.Default:
                    SetupDefault(npObject);
                    return;

                case NameplateMode.JobIconAndName:

                    if (_configuration.IconSetId == IconSetId.Framed)
                    {
                        if (!IsIgnorableStatus(oldIconId))
                        {
                            SetupDefault(npObject);
                            npObject.AdjustIconPosition(10, 0);
                            return;
                        }
                        npObject.SetIconScale(0.75f);
                        npObject.SetNameScale(0.5f);
                        npObject.AdjustIconPosition(14, 4);
                    }
                    else
                    {
                        SetupDefault(npObject);
                        npObject.AdjustIconPosition(10, 0);
                    }
                    return;

                case NameplateMode.JobIcon:
                    nameScale = 0.75f;

                    switch (_configuration.SizeMode)
                    {
                        case NameplateSizeMode.Smaller:
                            iconOffset = new Vector2(9, 50);
                            iconScale = 1.5f;
                            break;

                        case NameplateSizeMode.Medium:
                            iconOffset = new Vector2(-12, 24);
                            iconScale = 3f;
                            break;

                        case NameplateSizeMode.Bigger:
                            iconOffset = new Vector2(-27, -12);
                            iconScale = 4f;
                            break;

                        case NameplateSizeMode.Tiny:
                            iconOffset = new Vector2(15, 74);
                            iconScale = 1f;
                            nameScale = 0.5f;
                            break;
                    }
                    break;

                case NameplateMode.JobIconAndPartySlot:
                    switch (_configuration.SizeMode)
                    {
                        case NameplateSizeMode.Smaller:
                            iconOffset = new Vector2(12, 62);
                            iconScale = 1.2f;
                            nameScale = 0.6f;
                            break;

                        case NameplateSizeMode.Medium:
                            iconOffset = new Vector2(-14, 41);
                            iconScale = 2.3f;
                            nameScale = 1f;
                            break;

                        case NameplateSizeMode.Bigger:
                            iconOffset = new Vector2(-32, 15);
                            iconScale = 3f;
                            nameScale = 1.5f;
                            break;

                        case NameplateSizeMode.Tiny:
                            iconOffset = new Vector2(15, 74);
                            iconScale = 1f;
                            nameScale = 0.5f;
                            break;
                    }
                    break;

                case NameplateMode.RoleLetters:
                case NameplateMode.JobIconAndRoleLettersUncolored:
                    if (_configuration.ShowPlayerStatus || mode == NameplateMode.JobIconAndRoleLettersUncolored)
                        switch (_configuration.SizeMode)
                        {
                            case NameplateSizeMode.Smaller:
                            case NameplateSizeMode.Tiny:
                                iconOffset = new Vector2(15, 74);
                                iconScale = 1f;
                                break;
                            case NameplateSizeMode.Medium:
                                iconOffset = new Vector2(0, 53);
                                iconScale = 1.5f;
                                break;
                            case NameplateSizeMode.Bigger:
                                iconOffset = new Vector2(-15, 34);
                                iconScale = 2f;
                                break;
                        }
                    else iconScale = 0f;

                    nameScale = _configuration.SizeMode switch
                    {
                        NameplateSizeMode.Smaller => 0.5f,
                        NameplateSizeMode.Medium => 1f,
                        NameplateSizeMode.Bigger => 1.5f,
                        NameplateSizeMode.Tiny => 0.5f,
                        _ => throw new Exception($"unknown NameplateSizeMode({_configuration.SizeMode})")
                    };
                    break;

            }

            if (GetModeForNameplate(npObject) != NameplateMode.RoleLetters
                && _configuration.IconSetId == IconSetId.Framed
                && (!_configuration.ShowPlayerStatus || IsIgnorableStatus(oldIconId)))
            {
                iconScale *= 0.75f;
                iconOffset.Y += 4;
                iconOffset.X += 2;
            }

            npObject.SetIconPosition((short)iconOffset.X, (short)iconOffset.Y);
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
                    name = GetStateNametext(_configuration.ShowPlayerStatus ? iconID : -1, _iconPrefix, SeStringUtils.SeStringFromPtr(name));
                    iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1);
                    break;

                case NameplateMode.JobIcon:
                    name = GetStateNametext(iconID);
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1);
                    break;

                case NameplateMode.JobIconAndPartySlot:
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    var partySlot = _partyListHudView.GetPartySlotIndex(namePlateInfo == null ? 0 : namePlateInfo.Data.ObjectID.ObjectID) + 1;
                    if (partySlot != null)
                    {
                        var genericRole = JobExtensions.GetRole((Job)(namePlateInfo == null ? 0 : namePlateInfo.GetJobID()));
                        var text = SeStringUtils.Color(PlayerStylesheet.GetPartySlotNumber(partySlot.Value, genericRole), PlayerStylesheet.GetRoleColor(genericRole));
                        name = GetStateNametext(_configuration.ShowPlayerStatus ? iconID : -1, _iconPrefix, text);
                        iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1);
                    }
                    else
                    {
                        name = GetStateNametext(_configuration.ShowPlayerStatus ? iconID : -1);
                        iconID = GetClassIcon(npObject.NamePlateInfo, _configuration.ShowPlayerStatus ? iconID : -1);
                    }
                    break;

                case NameplateMode.RoleLetters:
                    if (hasRole)
                    {
                        var text = SeStringUtils.Color(PlayerStylesheet.GetRolePlate(roleId, _configuration.EasternNamingConvention), PlayerStylesheet.GetRoleColor(roleId));
                        name = GetStateNametext(-1, _configuration.ShowPlayerStatus ? String.Concat(_iconPrefix," ") : null, text);
                    }
                    else
                    {
                        var genericRole = JobExtensions.GetRole((Job)(namePlateInfo == null ? 0 : namePlateInfo.GetJobID()));
                        var text = SeStringUtils.Color(PlayerStylesheet.GetRolePlate(genericRole, _configuration.EasternNamingConvention), PlayerStylesheet.GetRoleColor(genericRole));
                        name = GetStateNametext(-1, _configuration.ShowPlayerStatus ? String.Concat(_iconPrefix, " ") : null, text);
                    }

                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    break;

                case NameplateMode.JobIconAndRoleLettersUncolored:
                    if (hasRole)
                    {
                        name = GetStateNametext(iconID, _iconPrefix, PlayerStylesheet.GetRolePlate(roleId, _configuration.EasternNamingConvention));
                    }
                    else
                    {
                        var genericRole = JobExtensions.GetRole((Job)(namePlateInfo == null ? 0 : namePlateInfo.GetJobID()));
                        name = GetStateNametext(iconID, _iconPrefix, PlayerStylesheet.GetRolePlate(genericRole, _configuration.EasternNamingConvention));
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

        private SeString GetStateNametextS(int iconId, string? prefix = _iconPrefix, SeString? append = null)
        {

            SeString? val = iconId switch
            {
                //061521 - party leader
                //061522 - party member
                061523 => SeStringUtils.Icon(BitmapFontIcon.NewAdventurer, prefix),
                061540 => SeStringUtils.Icon(BitmapFontIcon.Mentor, prefix),
                061542 => SeStringUtils.Icon(BitmapFontIcon.MentorPvE, prefix),
                061543 => SeStringUtils.Icon(BitmapFontIcon.MentorCrafting, prefix),
                061544 => SeStringUtils.Icon(BitmapFontIcon.MentorPvP, prefix),
                061547 => SeStringUtils.Icon(BitmapFontIcon.Returner, prefix),
                _ => null
            };

            return append == null ? val ?? SeString.Empty : val == null ? prefix == null ? append : SeStringUtils.Text(prefix).Append(append) : val.Append(append);
        }

        private IntPtr GetStateNametext(int iconId, string? prefix = _iconPrefix, SeString? append = null)
        {
            return SeStringUtils.SeStringToPtr(GetStateNametextS(iconId, prefix, append));
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