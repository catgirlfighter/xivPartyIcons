using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using PartyNamplates.Entities;
using PartyNamplates.View;

namespace PartyNamplates
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public event Action? OnSave;

        public int Version { get; set; } = 1;
        public bool ChatContentMessage = true;
        public bool HideLocalPlayerNameplate = true;
        public bool TestingMode = true;
        public bool EasternNamingConvention = false;
        public bool DisplayRoleInPartyList = false;
        public bool ShowPlayerStatus = true;
        public bool AvatarAnnouncementsInChat = false;

        public IconSetMode IconSetId { get; set; } = IconSetMode.GlowingColored;
        public NameplateSizeMode SizeMode { get; set; } = NameplateSizeMode.Large;
        public NameplateMode NameplateOverworld { get; set; } = NameplateMode.JobIconAndName;
        public NameplateMode NameplateAllianceRaid { get; set; } = NameplateMode.JobIcon;
        public NameplateMode NameplateDungeon { get; set; } = NameplateMode.JobIcon;
        public NameplateMode NameplateRaid { get; set; } = NameplateMode.RoleLetters;
        public NameplateMode NameplateOthers { get; set; } = NameplateMode.JobIconAndName;
        public NameplateMode NameplatePvP { get; set; } = NameplateMode.JobIconAndRoleLettersUncolored;
        public ChatMode ChatOverworld { get; set; } = ChatMode.GameDefault;
        public ChatMode ChatAllianceRaid { get; set; } = ChatMode.GameDefault;
        public ChatMode ChatDungeon { get; set; } = ChatMode.GameDefault;
        public ChatMode ChatRaid { get; set; } = ChatMode.GameDefault;
        public ChatMode ChatOthers { get; set; } = ChatMode.GameDefault;
        public ChatMode ChatPvP { get; set; } = ChatMode.GameDefault;
        public Dictionary<string, RoleId> StaticAssignments { get; set; } = new();

        private DalamudPluginInterface? _interface;

        public void Initialize(DalamudPluginInterface @interface)
        {
            _interface = @interface;
        }

        public void Save()
        {
            if (_interface != null)
            {
                _interface.SavePluginConfig(this);
                OnSave?.Invoke();
            }
        }
    }
}