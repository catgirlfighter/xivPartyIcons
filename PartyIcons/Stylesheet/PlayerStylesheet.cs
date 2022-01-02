using System;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using PartyIcons.Entities;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons.Stylesheet
{
    public static class PlayerStylesheet
    {
        private const ushort _fallbackColor = 1;

        public static ushort GetRoleColor(GenericRole role)
        {
            switch (role)
            {
                case GenericRole.Tank:
                    return 37;

                case GenericRole.Melee:
                    return 524;

                case GenericRole.Ranged:
                    return 32;

                case GenericRole.Healer:
                    return 42;

                default:
                    return _fallbackColor;
            }
        }

        public static ushort GetRoleColor(RoleId roleId)
        {
            return GetRoleInd(roleId) switch
            {
                RoleIdUtils.ROLE_TANK => GetRoleColor(GenericRole.Tank),
                RoleIdUtils.ROLE_HEALER => GetRoleColor(GenericRole.Healer),
                RoleIdUtils.ROLE_MELEE => GetRoleColor(GenericRole.Melee),
                RoleIdUtils.ROLE_RANGED => GetRoleColor(GenericRole.Ranged),
                _ => _fallbackColor,
            };
        }

        public static string GetRoleIconset(RoleId roleId, IconSetId iconSetId)
        {
            switch (iconSetId)
            {
                case IconSetId.Framed:
                    return "Framed";

                case IconSetId.GlowingGold:
                    return "Glowing";

                case IconSetId.GlowingColored:
                    return GetRoleInd(roleId) switch
                    {
                        RoleIdUtils.ROLE_TANK => "Blue",
                        RoleIdUtils.ROLE_HEALER => "Green",
                        RoleIdUtils.ROLE_MELEE => "Red",
                        RoleIdUtils.ROLE_RANGED => "Orange",
                        _ => "Grey"
                    };

                default:
                    throw new ArgumentException($"Unknown icon set id: {iconSetId}");
            }
        }

        public static string GetGenericRoleIconset(GenericRole role, IconSetId iconSetId)
        {
            return iconSetId switch
            {
                IconSetId.Framed => "Framed",
                IconSetId.GlowingGold => "Glowing",
                IconSetId.GlowingColored => role switch
                {
                    GenericRole.Tank => "Blue",
                    GenericRole.Melee => "Red",
                    GenericRole.Ranged => "Orange",
                    GenericRole.Healer => "Green",
                    _ => "Grey",
                },
                _ => throw new ArgumentException($"Unknown icon set id: {iconSetId}"),
            };
        }

        public static string GetRoleName(RoleId roleId, bool eastern)
        {
            return roleId switch
            {
                RoleId.MT => "MT",
                RoleId.OT => eastern ? "ST" : "OT",
                _ => roleId.ToString(),
            };
        }

        public static SeString GetRolePlate(GenericRole genericRole, bool eastern)
        {
            if (genericRole < GenericRole.Crafter)
            {
                return genericRole switch
                {
                    GenericRole.Tank => SeStringUtils.Text(BoxedCharacterString("T")),
                    GenericRole.Melee => SeStringUtils.Text(BoxedCharacterString(eastern ? "D" : "M")),
                    GenericRole.Ranged => SeStringUtils.Text(BoxedCharacterString(eastern ? "D" : "R")),
                    GenericRole.Healer => SeStringUtils.Text(BoxedCharacterString("H")),
                    _ => throw new Exception($"unknown GenericRole({genericRole})")
                };
            }
            else
            {
                return "";
            }
        }

        public static int GetRoleInd(RoleId roleId)
        {
            if (roleId == RoleId.Undefined)
                return -1;

            return ((int)roleId - 1) / RoleIdUtils.ROLE_COUNT;
        }

        public static SeString GetRolePlate(RoleId roleId, bool eastern)
        {
            if (roleId == RoleId.Undefined) return string.Empty;
            if (eastern)
            {
                if (roleId == RoleId.OT) return SeStringUtils.Text(BoxedCharacterString("ST"));
                if (GetRoleInd(roleId) > RoleIdUtils.ROLE_HEALER) return SeStringUtils.Text(BoxedCharacterString("D" + ((int)roleId - (int)RoleId.H8).ToString()));
            }
            return SeStringUtils.Text(BoxedCharacterString(roleId.ToString()));
        }

        public static SeString GetRolePlateNumber(RoleId roleId, bool eastern)
        {
            if (roleId == RoleId.Undefined)
                return SeStringUtils.Text("");

            if (eastern)
            {
                int roleNum;
                if (roleId > RoleId.H8)
                    roleNum = roleId - RoleId.H8;
                else
                    roleNum = ((int)roleId - 1) % RoleIdUtils.ROLE_COUNT + 1;
                return SeStringUtils.Text(BoxedCharacterString(roleNum.ToString()));
            }
            else
            {
                int roleNum = ((int)roleId - 1) % RoleIdUtils.ROLE_COUNT + 1;
                return SeStringUtils.Text(BoxedCharacterString(roleNum.ToString()));
            }
        }

        public static SeString GetPartySlotNumber(uint number, GenericRole genericRole)
        {
            return SeStringUtils.Text(BoxedCharacterString(number.ToString()));
        }

        public static SeString GetRoleChatPrefix(RoleId roleId, bool eastern)
        {
            return GetRolePlate(roleId, eastern);
        }

        public static ushort GetRoleChatColor(RoleId roleId)
        {
            return GetRoleColor(roleId);
        }

        public static SeString GetGenericRoleChatPrefix(ClassJob classJob, bool eastern)
        {
            return GetRolePlate(JobExtensions.GetRole((Job)classJob.RowId), eastern);
        }

        public static ushort GetGenericRoleChatColor(ClassJob classJob)
        {
            return GetRoleColor(JobExtensions.GetRole((Job)classJob.RowId));
        }


        public static SeString GetJobChatPrefix(ClassJob classJob)
        {
            if (true)
            {
                return new SeString(
                    new UIGlowPayload(GetGenericRoleChatColor(classJob)),
                    new UIForegroundPayload(GetGenericRoleChatColor(classJob)),
                    new TextPayload(classJob.Abbreviation),
                    UIForegroundPayload.UIForegroundOff,
                    UIGlowPayload.UIGlowOff
                );
            }
        }

        public static ushort GetJobChatColor(ClassJob classJob)
        {
            return GetRoleColor(JobExtensions.GetRole((Job)classJob.RowId));
        }

        public static string BoxedCharacterString(string str)
        {
            var builder = new StringBuilder(str.Length);
            foreach (var ch in str.ToLower())
            {
                builder.Append(ch switch
                {
                    _ when (ch >= 'a' && ch <= 'z') => (char)(ch + 57360),
                    _ when (ch >= '0' && ch <= '9') => (char)(ch + 57439),

                    _ => ch,
                });
            }

            return builder.ToString();
        }
    }
}