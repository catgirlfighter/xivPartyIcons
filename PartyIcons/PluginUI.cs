using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using ImGuiScene;
using PartyNamplates.Entities;
using PartyNamplates.Stylesheet;
using PartyNamplates.View;

namespace PartyNamplates
{
    class PluginUI : IDisposable
    {
        [PluginService] private DalamudPluginInterface? Interface { get; set; }

        private readonly Configuration _configuration;

        private bool _settingsVisible = false;
        private Vector2 _windowSize;
        private string _occupationNewName = "Character Name@World";
        private RoleId _occupationNewRole = RoleId.Undefined;

        public bool SettingsVisible
        {
            get { return this._settingsVisible; }
            set { this._settingsVisible = value; }
        }

        private Dictionary<NameplateMode, TextureWrap> _nameplateExamples;

        public PluginUI(Configuration configuration)
        {
            this._configuration = configuration;
            _nameplateExamples = new Dictionary<NameplateMode, TextureWrap>();
        }

        public void Dispose()
        {
        }

        public void OpenSettings()
        {
            SettingsVisible = true;
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            if (_windowSize == default)
            {
                _windowSize = new Vector2(1000, 800);
            }

            ImGui.SetNextWindowSize(_windowSize, ImGuiCond.Always);
            if (ImGui.Begin("PartyIcons", ref this._settingsVisible))
            {
                if (ImGui.BeginTabBar("##tabbar"))
                {
                    if (ImGui.BeginTabItem("General##general"))
                    {
                        DrawGeneralSettings();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Nameplates"))
                    {
                        DrawNameplateSettings();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Static Assignments##static_assignments"))
                    {
                        DrawStaticAssignmentsSettings();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }

            _windowSize = ImGui.GetWindowSize();
            ImGui.End();
        }

        private void DrawGeneralSettings()
        {
            var testingMode = _configuration.TestingMode;
            if (ImGui.Checkbox("##testingMode", ref testingMode))
            {
                _configuration.TestingMode = testingMode;
                _configuration.Save();
            }
            ImGui.SameLine();
            ImGui.Text("Enable testing mode");
            ImGuiHelpTooltip("Applies settings to any player, contrary to only the ones that are in the party");

            var chatContentMessage = _configuration.ChatContentMessage;
            if (ImGui.Checkbox("##chatmessage", ref chatContentMessage))
            {
                _configuration.ChatContentMessage = chatContentMessage;
                _configuration.Save();
            }
            ImGui.SameLine();
            ImGui.Text("Display chat message when entering duty");
            ImGuiHelpTooltip("Can be used to determine the duty type before fully loading in");

            var avatarAnnouncementsInChat = _configuration.AvatarAnnouncementsInChat;
            if (ImGui.Checkbox("##avatarsinchat", ref avatarAnnouncementsInChat))
            {
                _configuration.AvatarAnnouncementsInChat = avatarAnnouncementsInChat;
                _configuration.Save();
            }
            ImGui.SameLine();
            ImGui.Text("Show Avatar announcements in party chat");
            ImGuiHelpTooltip("Announcements from avatars in trust dungeons will be shown in party chat");

            var easternNamingConvention = _configuration.EasternNamingConvention;
            if (ImGui.Checkbox("##easteannaming", ref easternNamingConvention))
            {
                _configuration.EasternNamingConvention = easternNamingConvention;
                _configuration.Save();
            }
            ImGui.SameLine();
            ImGui.Text("Eastern role naming convention");
            ImGuiHelpTooltip("Use japanese data center role naming convention (MT ST D1-D4 H1-2)");

            var displayRoleInPartyList = _configuration.DisplayRoleInPartyList;
            if (ImGui.Checkbox("##displayrolesinpartylist", ref displayRoleInPartyList))
            {
                _configuration.DisplayRoleInPartyList = displayRoleInPartyList;
                _configuration.Save();
            }
            ImGui.SameLine();
            ImGui.Text("Replace party numbers with role in Party List");
            ImGuiHelpTooltip("EXPERIMENTAL. Only works when nameplates set to 'Role letters'", true);
        }

        private void DrawNameplateSettings()
        {
            var hideLocalNameplate = _configuration.HideLocalPlayerNameplate;
            if (ImGui.Checkbox("##hidelocal", ref hideLocalNameplate))
            {
                _configuration.HideLocalPlayerNameplate = hideLocalNameplate;
                _configuration.Save();
            }
            ImGui.SameLine();
            ImGui.Text("Hide own nameplate");
            ImGuiHelpTooltip("You can turn your own nameplate on and also turn this\nsetting own to only use nameplate to display own raid position.\nIf you don't want your position displayed with this setting you can\nsimply disable your nameplates in the Character settings.");

            var showPlayerStatus = _configuration.ShowPlayerStatus;
            if (ImGui.Checkbox("##showplayerstatus", ref showPlayerStatus))
            {
                _configuration.ShowPlayerStatus = showPlayerStatus;
                _configuration.Save();
            }

            ImGui.SameLine();
            ImGui.Text("Show Player Status");
            ImGuiHelpTooltip("Display player status, or at least if it's a new adventurer or a mentor if possible");

            ImGui.Dummy(new Vector2(0f, 25f));

            var iconSetId = _configuration.IconSetId;
            ImGui.Text("Icon Set:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(300);
            if (ImGui.BeginCombo("##icon_set", IconSetIdToString(iconSetId)))
            {
                foreach (var id in Enum.GetValues<IconSetMode>())
                {
                    if (ImGui.Selectable(IconSetIdToString(id) + "##icon_set_" + id))
                    {
                        _configuration.IconSetId = id;
                        _configuration.Save();
                    }
                }
                ImGui.EndCombo();
            }

            var iconSizeMode = _configuration.SizeMode;
            ImGui.Text("Nameplate Size:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(300);
            if (ImGui.BeginCombo("##icon_size", iconSizeMode.ToString()))
            {
                foreach (var mode in Enum.GetValues<NameplateSizeMode>())
                {
                    if (ImGui.Selectable(mode + "##icon_set_" + mode))
                    {
                        _configuration.SizeMode = mode;
                        _configuration.Save();
                    }
                }
                ImGui.EndCombo();
            }
            ImGuiHelpTooltip("Affects all presets, except Game Default and Small Job Icon");

            ImGui.Dummy(new Vector2(0, 25f));
            ImGui.Text("Dungeon:");
            ImGuiHelpTooltip("Modes used for your party while in dungeon");
            NameplateModeSection("##np_dungeon", () => _configuration.NameplateDungeon, (mode) => _configuration.NameplateDungeon = mode);
            ImGui.SameLine();
            ChatModeSection("##chat_dungeon", () => _configuration.ChatDungeon, (mode) => _configuration.ChatDungeon = mode);
            ImGui.Dummy(new Vector2(0, 15f));

            ImGui.Text("Raid:");
            ImGuiHelpTooltip("Modes used for your party while in raid");
            NameplateModeSection("##np_raid", () => _configuration.NameplateRaid, (mode) => _configuration.NameplateRaid = mode);
            ImGui.SameLine();
            ChatModeSection("##chat_raid", () => _configuration.ChatRaid, (mode) => _configuration.ChatRaid = mode);
            ImGui.Dummy(new Vector2(0, 15f));

            ImGui.Text("Alliance Raid party:");
            ImGuiHelpTooltip("Modes used for your party while in alliance raid");
            NameplateModeSection("##np_alliance", () => _configuration.NameplateAllianceRaid, (mode) => _configuration.NameplateAllianceRaid = mode);
            ImGui.SameLine();
            ChatModeSection("##chat_alliance", () => _configuration.ChatAllianceRaid, (mode) => _configuration.ChatAllianceRaid = mode);
            ImGui.Dummy(new Vector2(0, 15f));

            ImGui.Text("Overworld party:");
            ImGuiHelpTooltip("Modes used for your party while not in duty");
            NameplateModeSection("##np_overworld", () => _configuration.NameplateOverworld, (mode) => _configuration.NameplateOverworld = mode);
            ImGui.SameLine();
            ChatModeSection("##chat_overworld", () => _configuration.ChatOverworld, (mode) => _configuration.ChatOverworld = mode);
            ImGui.Dummy(new Vector2(0, 15f));

            ImGui.Text("Other player characters:");
            ImGuiHelpTooltip("Modes used for non-party players");
            NameplateModeSection("##np_others", () => _configuration.NameplateOthers, (mode) => _configuration.NameplateOthers = mode);
            ImGui.SameLine();
            ChatModeSection("##chat_others", () => _configuration.ChatOthers, (mode) => _configuration.ChatOthers = mode);
            ImGui.Dummy(new Vector2(0, 15f));

            ImGui.Text("Frontline:");
            ImGuiHelpTooltip("Modes used for your party in Frontline");
            NameplateModeSection("##np_pvp", () => _configuration.NameplatePvP, (mode) => _configuration.NameplatePvP = mode);
            ImGui.SameLine();
            ChatModeSection("##chat_pvp", () => _configuration.ChatPvP, (mode) => _configuration.ChatPvP = mode);
            ImGui.Dummy(new Vector2(0, 25f));
            ImGui.TextWrapped("Please note that it usually takes a some time for nameplates to reload, especially for own character nameplate");
        }

        private void DrawStaticAssignmentsSettings()
        {
            ImGui.TextWrapped("Name should include world name, separated by @. Experimental option");
            ImGui.Dummy(new Vector2(0f, 25f));

            foreach (var kv in new Dictionary<string, RoleId>(_configuration.StaticAssignments))
            {
                if (ImGui.Button("x##remove_occupation_" + kv.Key))
                {
                    _configuration.StaticAssignments.Remove(kv.Key);
                    _configuration.Save();
                    continue;
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(200);
                if (ImGui.BeginCombo("##role_combo_" + kv.Key, PlayerStylesheet.GetRoleName(_configuration.StaticAssignments[kv.Key], _configuration.EasternNamingConvention)))
                {
                    foreach (var roleId in Enum.GetValues<RoleId>())
                    {
                        if (ImGui.Selectable(PlayerStylesheet.GetRoleName(roleId, _configuration.EasternNamingConvention) + "##role_combo_option_" + kv.Key + "_" + roleId))
                        {
                            _configuration.StaticAssignments[kv.Key] = roleId;
                            _configuration.Save();
                        }
                    }

                    ImGui.EndCombo();
                }

                ImGui.SameLine();
                ImGui.Text(kv.Key);
            }

            if (ImGui.Button("+##add_occupation"))
            {
                _configuration.StaticAssignments[_occupationNewName] = _occupationNewRole;
                _configuration.Save();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.BeginCombo("##new_role_combo", PlayerStylesheet.GetRoleName(_occupationNewRole, _configuration.EasternNamingConvention)))
            {
                foreach (var roleId in Enum.GetValues<RoleId>())
                {
                    if (ImGui.Selectable(PlayerStylesheet.GetRoleName(roleId, _configuration.EasternNamingConvention) + "##new_role_combo_option_" + "_" + roleId))
                    {
                        _occupationNewRole = roleId;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.InputText("##new_role_name", ref _occupationNewName, 32);
        }

        private void CollapsibleExampleImage(NameplateMode mode, TextureWrap tex)
        {
            if (ImGui.CollapsingHeader(NameplateModeToString(mode)))
            {
                ImGui.Image(tex.ImGuiHandle, new Vector2(tex.Width, tex.Height));
            }
        }

        private void ImGuiHelpTooltip(string tooltip, bool experimental = false)
        {
            ImGui.SameLine();

            if (experimental)
            {
                ImGui.TextColored(new Vector4(0.8f, 0.0f, 0.0f, 1f), "!");
            }
            else
            {
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "?");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
        }

        private void ChatModeSection(string label, Func<ChatMode> getter, Action<ChatMode> setter)
        {
            ImGui.Text("Chat name: ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(400f);
            if (ImGui.BeginCombo(label, ChatModeToString(getter())))
            {
                foreach (var mode in Enum.GetValues<ChatMode>())
                {
                    if (ImGui.Selectable(ChatModeToString(mode), mode == getter()))
                    {
                        setter(mode);
                        _configuration.Save();
                    }
                }
                ImGui.EndCombo();
            }
        }

        private string ChatModeToString(ChatMode mode)
        {
            return mode switch
            {
                ChatMode.GameDefault => "Game Default",
                ChatMode.Role => "Role",
                //ChatMode.Job => "Job abbreviation",
                ChatMode.NameColor => "Color only",
                _ => throw new ArgumentException(),
            };
        }

        private void NameplateModeSection(string label, Func<NameplateMode> getter, Action<NameplateMode> setter)
        {
            ImGui.Text("Nameplate: ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(400f);
            if (ImGui.BeginCombo(label, NameplateModeToString(getter())))
            {
                foreach (var mode in Enum.GetValues<NameplateMode>())
                {
                    if (ImGui.Selectable(NameplateModeToString(mode), mode == getter()))
                    {
                        setter(mode);
                        _configuration.Save();
                    }
                }
                ImGui.EndCombo();
            }
        }

        private static string IconSetIdToString(IconSetMode id)
        {
            return id switch
            {
                IconSetMode.None => "None",
                IconSetMode.Framed => "Framed, role colored",
                IconSetMode.GlowingColored => "Glowing, role colored",
                _ => throw new Exception($"unknown IconSetId({id})")
            };
        }

        private static string NameplateModeToString(NameplateMode mode)
        {
            return mode switch
            {
                NameplateMode.Default => "Game default",
                NameplateMode.JobIcon => "Job Icon only",
                NameplateMode.JobIconAndName => "Small Job Icon and Name",
                NameplateMode.JobIconAndPartySlot => "Job Icon and Party Number",
                NameplateMode.RoleLetters => "Role Letters",
                NameplateMode.JobIconAndRoleLettersUncolored => "Job Icon + Role Letters, uncolorcoded",
                _ => throw new Exception($"unknown NameplateMode({mode})"),
            };
        }
    }
}