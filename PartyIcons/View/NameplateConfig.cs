using System;
using System.Numerics;
using System.Collections.Generic;
using PartyNamplates.View;

namespace PartyNamplates.View
{
    [Flags]
    public enum NameplateConfigFlag
    {
        None = 0,
        HideTitle = 1,
        HideFreeCompany = 2,
        ShowJobIcon = 4,
        ShowStatus = 8,
        ShowStatusLimited = 16,
        ColorCoded = 32,
    }

    public enum NameplateConfigUse
    {
        UseDefault = 0,
        UseOverworld = 1,
        UseConfig = 2
    }
    public readonly record struct NameplateConfig(
        IconSetMode IconSet,
        float IconScale,
        Vector2 IconOffset,
        float NameScale,
        string Format,
        NameplateConfigFlag Flags);
    public readonly record struct NameplateConfigSet(
        NameplateConfigUse UseParty, 
        NameplateConfigUse UseOther, 
        NameplateConfig PartyConfig, 
        NameplateConfigFlag OtherConfig,
        ChatMode ChatMode
        );

    //A  - ADVENTURER STATUS
    //N  - PLAYER CHARATER NAME
    //PN - PARTY NUMBER
    //J  - JOB NAME (SHORT)
    //R  - ROLE
    //RN - ROLENUMBER
    //RJ - ROLENUMBER JAPAN STYLE
    //II - ICON ID

    public abstract class NameplateFormat
    {
        private readonly int _index;
        public int Index => _index;
        public NameplateFormat(int index) 
        {
            _index = index;
        }
    }

    public class NameplateFormatPlainText : NameplateFormat
    {
        public NameplateFormatPlainText(int index, string value) : base(index)
        {
        }
    }


    public class NameplateFormatSet : NameplateFormatPlainText
    {
        public NameplateFormatSet(int index, string value): base(index, value)
        {
        }
    }

    public abstract class NameplateFormatCondition : NameplateFormatSet
    {
        public NameplateFormatCondition(int index, string value, NameplateFormatSet parent) : base(index, value)
        {
        }
    }

    public class NameplateFormatConditionTrue : NameplateFormatCondition
    {
        public NameplateFormatConditionTrue(int index, string value, NameplateFormatSet parent) : base(index, value, parent)
        {
        }
    }

    public class NameplateFormatConditionFalse : NameplateFormatCondition
    {
        public NameplateFormatConditionFalse(int index, string value, NameplateFormatSet parent) : base(index, value, parent)
        {
        }
    }

    public static class NameplateConfigDefaults
    {
        public static readonly Dictionary<int, NameplateConfig> confCache = new();
        public static readonly float[] DefaultIconScales = { 1.2f, 1f, 0.8f, 1f };
        public static readonly Vector2[] DefaultIconOffsets = { new Vector2(10f, 0f), new Vector2(10f, 0f), new Vector2(13f, 3f), new Vector2(10f, 0f) };

        public static readonly NameplateConfig Overworld = new()
        {
            IconSet = IconSetMode.Framed,
            IconScale = 0.9f,
            NameScale = 1f,
            IconOffset = new Vector2(10f, 0f),
            Format = "   [A][N]",
            Flags = NameplateConfigFlag.ShowStatus
        };

        public static readonly NameplateConfig Dungeon = new()
        {
            IconSet = IconSetMode.Framed,
            IconScale = DefaultIconScales[(int)IconSetMode.Framed],
            NameScale = 1f,
            IconOffset = new Vector2(10f, 0f),
            Format = "   [PN][A]",
            Flags = NameplateConfigFlag.HideTitle | NameplateConfigFlag.HideFreeCompany | NameplateConfigFlag.ShowStatusLimited | NameplateConfigFlag.ColorCoded
        };

        public static readonly NameplateConfig Raid = new()
        {
            IconSet = IconSetMode.Framed,
            IconScale = DefaultIconScales[(int)IconSetMode.Framed],
            NameScale = 1f,
            IconOffset = new Vector2(10f, 0f),
            Format = "   [R][RN][A]",
            Flags = NameplateConfigFlag.HideTitle | NameplateConfigFlag.HideFreeCompany | NameplateConfigFlag.ShowStatusLimited | NameplateConfigFlag.ColorCoded
        };

        public static readonly NameplateConfig Alliance = new()
        {
            IconSet = IconSetMode.Framed,
            IconScale = DefaultIconScales[(int)IconSetMode.Framed],
            NameScale = 1f,
            IconOffset = new Vector2(10f, 0f),
            Format = "   [PN][A]",
            Flags = NameplateConfigFlag.HideTitle | NameplateConfigFlag.HideFreeCompany | NameplateConfigFlag.ShowStatusLimited
        };

        public static readonly NameplateConfig PvP = new()
        {
            IconSet = IconSetMode.None,
            IconScale = DefaultIconScales[(int)IconSetMode.None],
            NameScale = 1f,
            IconOffset = new Vector2(10f, 0f),
            Format = "   [J][PN][A]",
            Flags = NameplateConfigFlag.HideTitle | NameplateConfigFlag.HideFreeCompany | NameplateConfigFlag.ShowStatusLimited
        };

        /* Default, JobIconAndName, JobIcon, JobIconAndPartySlot, RoleLetters, JobIconAndRoleLettersUncolored */
        public static readonly NameplateConfig?[] NameplateConfigByViewMode = { null, Overworld, Alliance, Dungeon, Raid, PvP };
    }
}
