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
    public sealed class PlayerStylesheet
    {
        private readonly Configuration _configuration;
        private readonly ushort _fallbackColor = 1;

        public PlayerStylesheet(Configuration configuration)
        {
            _configuration = configuration;
        }

        public ushort GetRoleColor(GenericRole role)
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

        public ushort GetRoleColor(RoleId roleId)
        {
            switch (roleId)
            {
                case RoleId.MT:
                case RoleId.OT:
                    return GetRoleColor(GenericRole.Tank);

                case RoleId.M1:
                case RoleId.M2:
                    return GetRoleColor(GenericRole.Melee);

                case RoleId.R1:
                case RoleId.R2:
                    return GetRoleColor(GenericRole.Ranged);

                case RoleId.H1:
                case RoleId.H2:
                    return GetRoleColor(GenericRole.Healer);

                default:
                    return _fallbackColor;
            }
        }

        public string GetRoleIconset(RoleId roleId)
        {
            switch (_configuration.IconSetId)
            {
                case IconSetId.Framed:
                    return "Framed";

                case IconSetId.GlowingGold:
                    return "Glowing";

                case IconSetId.GlowingColored:
                    return roleId switch
                    {
                        RoleId.MT => "Blue",
                        RoleId.OT => "Blue",
                        RoleId.M1 => "Red",
                        RoleId.M2 => "Red",
                        RoleId.R1 => "Orange",
                        RoleId.R2 => "Orange",
                        RoleId.H1 => "Green",
                        RoleId.H2 => "Green",
                        _ => "Grey",
                    };

                default:
                    throw new ArgumentException($"Unknown icon set id: {_configuration.IconSetId}");
            }
        }

        public string GetGenericRoleIconset(GenericRole role)
        {
            return _configuration.IconSetId switch
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
                _ => throw new ArgumentException($"Unknown icon set id: {_configuration.IconSetId}"),
            };
        }

        public string GetRoleName(RoleId roleId)
        {
            return roleId switch
            {
                RoleId.MT => "MT",
                RoleId.OT => _configuration.EasternNamingConvention ? "ST" : "OT",
                _ => roleId.ToString(),
            };
        }

        public SeString GetRolePlate(GenericRole genericRole)
        {
            if (genericRole <= GenericRole.Healer)
            {
                //ushort color = colored ? GetRoleColor(genericRole) : (ushort)0;
                return genericRole switch
                {
                    GenericRole.Tank => SeStringUtils.Text(BoxedCharacterString("T")),
                    GenericRole.Melee => SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "M")),
                    GenericRole.Ranged => SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "R")),
                    GenericRole.Healer => SeStringUtils.Text(BoxedCharacterString("H")),
                    _ => throw new Exception($"unknown GenericRole({genericRole})")
                };
            }
            else
            {
                return "";
            }
        }

        public SeString GetRolePlate(RoleId roleId, bool colored = true)
        {
            //ushort color = colored ? GetRoleColor(roleId) : (ushort)0;
            return roleId switch
            {
                RoleId.MT => SeStringUtils.Text(BoxedCharacterString("MT")),
                RoleId.OT => SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "ST" : "OT")),
                RoleId.M1 or RoleId.M2 => SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "M") + GetRolePlateNumber(roleId)),
                RoleId.R1 or RoleId.R2 => SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "R") + GetRolePlateNumber(roleId)),
                RoleId.H1 or RoleId.H2 => SeStringUtils.Text(BoxedCharacterString("H") + GetRolePlateNumber(roleId)),
                _ => string.Empty,
            };
        }

        public SeString GetRolePlateNumber(RoleId roleId)
        {
            if (_configuration.EasternNamingConvention)
            {
                return roleId switch
                {
                    RoleId.MT => SeStringUtils.Text(BoxedCharacterString("1")),
                    RoleId.OT => SeStringUtils.Text(BoxedCharacterString("2")),
                    RoleId.H1 => SeStringUtils.Text(BoxedCharacterString("1")),
                    RoleId.H2 => SeStringUtils.Text(BoxedCharacterString("2")),
                    RoleId.M1 => SeStringUtils.Text(BoxedCharacterString("1")),
                    RoleId.M2 => SeStringUtils.Text(BoxedCharacterString("2")),
                    RoleId.R1 => SeStringUtils.Text(BoxedCharacterString("3")),
                    RoleId.R2 => SeStringUtils.Text(BoxedCharacterString("4")),
                    _ => SeStringUtils.Text("")
                };
            }
            else
            {
                return roleId switch
                {
                    RoleId.MT => SeStringUtils.Text(BoxedCharacterString("1")),
                    RoleId.OT => SeStringUtils.Text(BoxedCharacterString("2")),
                    RoleId.H1 => SeStringUtils.Text(BoxedCharacterString("1")),
                    RoleId.H2 => SeStringUtils.Text(BoxedCharacterString("2")),
                    RoleId.M1 => SeStringUtils.Text(BoxedCharacterString("1")),
                    RoleId.M2 => SeStringUtils.Text(BoxedCharacterString("2")),
                    RoleId.R1 => SeStringUtils.Text(BoxedCharacterString("1")),
                    RoleId.R2 => SeStringUtils.Text(BoxedCharacterString("2")),
                    _ => SeStringUtils.Text("")
                };
            }
        }

        public SeString GetPartySlotNumber(uint number, GenericRole genericRole)
        {
            return SeStringUtils.Text(BoxedCharacterString(number.ToString()));
        }

        public SeString GetRoleChatPrefix(RoleId roleId)
        {
            return GetRolePlate(roleId);
        }

        public ushort GetRoleChatColor(RoleId roleId)
        {
            return GetRoleColor(roleId);
        }

        public SeString GetGenericRoleChatPrefix(ClassJob classJob)
        {
            return GetRolePlate(JobExtensions.GetRole((Job)classJob.RowId));
        }

        public ushort GetGenericRoleChatColor(ClassJob classJob)
        {
            return GetRoleColor(JobExtensions.GetRole((Job)classJob.RowId));
        }


        public SeString GetJobChatPrefix(ClassJob classJob)
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

        public ushort GetJobChatColor(ClassJob classJob)
        {
            return GetRoleColor(JobExtensions.GetRole((Job)classJob.RowId));
        }

        public string BoxedCharacterString(string str)
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