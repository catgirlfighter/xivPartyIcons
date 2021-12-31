using System;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Plugin;
using PartyIcons.Api;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.Utils;
using PartyIcons.View;
using Dalamud.Data;
using XivCommon;
using SigScanner = Dalamud.Game.SigScanner;

namespace PartyIcons
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] private DalamudPluginInterface? FInterface { get; set; }
        [PluginService] private ClientState? FClientState { get; set; }
        [PluginService] private Framework? FFramework { get; set; }
        [PluginService] private CommandManager? FCommandManager { get; set; }
        [PluginService] private ObjectTable? FObjectTable { get; set; }
        [PluginService] private GameGui? FGameGui { get; set; }
        [PluginService] private ChatGui? FChatGui { get; set; }
        [PluginService] private PartyList? FPartyList { get; set; }
        [PluginService] private SigScanner? FSigScanner { get; set; }
        [PluginService] private DataManager? FDataManager { get; set; }
        [PluginService] private GameNetwork? FGameNetwork { get; set; }
        [PluginService] private Condition? FCondition { get; set; }
        [PluginService] private ToastGui? FToastGui { get; set; }

        public DalamudPluginInterface Interface;
        public ClientState ClientState;
        public Framework Framework;
        public CommandManager CommandManager;
        public ObjectTable ObjectTable;
        public GameGui GameGui;
        public ChatGui ChatGui;
        public PartyList PartyList;
        public SigScanner SigScanner;
        public DataManager DataManager;
        public GameNetwork GameNetwork;
        public Condition Condition;
        public ToastGui ToastGui;
        public string Name => "PartyIcons";

        private const string commandName = "/ppi";
        public PluginAddressResolver Address { get; }
        private XivCommonBase Base { get; }
        private Configuration Configuration { get; }

        private readonly PartyListHUDView _partyHUDView;
        private readonly PartyListHUDUpdater _partyListHudUpdater;
        private readonly PlayerContextMenu _contextMenu;
        private readonly PluginUI _ui;
        private readonly NameplateUpdater _nameplateUpdater;
        private readonly NPCNameplateFixer _npcNameplateFixer;
        private readonly NameplateView _nameplateView;
        private readonly RoleTracker _roleTracker;
        private readonly ViewModeSetter _modeSetter;
        private readonly ChatNameUpdater _chatNameUpdater;
        private readonly PlayerStylesheet _playerStylesheet;

        public Plugin()
        {
            //turning nullables into not nullables for public use
            if (FInterface != null) Interface = FInterface; else throw new Exception("Interface is not Assigned");
            if (FClientState != null) ClientState = FClientState; else throw new Exception("ClientState is not Assigned");
            if (FFramework != null) Framework = FFramework; else throw new Exception("Framework is not Assigned");
            if (FCommandManager != null) CommandManager = FCommandManager; else throw new Exception("CommandManager is not Assigned");
            if (FObjectTable != null) ObjectTable = FObjectTable; else throw new Exception("ObjectTable is not Assigned");
            if (FGameGui != null) GameGui = FGameGui; else throw new Exception("GameGui is not Assigned");
            if (FChatGui != null) ChatGui = FChatGui; else throw new Exception("ChatGui is not Assigned");
            if (FPartyList != null) PartyList = FPartyList; else throw new Exception("PartyList is not Assigned");
            if (FSigScanner != null) SigScanner = FSigScanner; else throw new Exception("SigScanner is not Assigned");
            if (FDataManager != null) DataManager = FDataManager; else throw new Exception("DataManager is not Assigned");
            if (FGameNetwork != null) GameNetwork = FGameNetwork; else throw new Exception("GameNetwork is not Assigned");
            if (FCondition != null) Condition = FCondition; else throw new Exception("Condition is not Assigned");
            if (FToastGui != null) ToastGui = FToastGui; else throw new Exception("ToastGui is not Assigned");

            Configuration = Interface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(Interface);
            Configuration.OnSave += OnConfigurationSave;

            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "opens configuration window; \"reset\" or \"r\" resets all assignments; \"debug\" prints debugging info."
            });

            Address = new PluginAddressResolver();
            Address.Setup(SigScanner);

            _playerStylesheet = new PlayerStylesheet(Configuration);

            _ui = new PluginUI(Configuration, _playerStylesheet);
            Interface.Inject(_ui);

            Base = new XivCommonBase(Hooks.ContextMenu);
            XivApi.Initialize(this, Address);

            SeStringUtils.Initialize();

            _partyHUDView = new PartyListHUDView(GameGui, _playerStylesheet);

            _roleTracker = new RoleTracker(this, Configuration);
            Interface.Inject(_roleTracker);

            _nameplateView = new NameplateView(this, _roleTracker, Configuration, _playerStylesheet, _partyHUDView);
            Interface.Inject(_nameplateView);

            _chatNameUpdater = new ChatNameUpdater(this, _roleTracker, _playerStylesheet);
            Interface.Inject(_chatNameUpdater);

            _partyListHudUpdater = new PartyListHUDUpdater(this, _partyHUDView, _roleTracker, Configuration);
            Interface.Inject(_partyListHudUpdater);

            _nameplateUpdater = new NameplateUpdater(Address, _nameplateView, Base);
            _npcNameplateFixer = new NPCNameplateFixer(_nameplateView);

            _contextMenu = new PlayerContextMenu(Base, _roleTracker, _playerStylesheet);
            Interface.Inject(_contextMenu);

            //_ui.Initialize();
            Interface.UiBuilder.Draw += _ui.DrawSettingsWindow;
            Interface.UiBuilder.OpenConfigUi += _ui.OpenSettings;

            _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;

            _modeSetter = new ViewModeSetter(this, _nameplateView, Configuration, _chatNameUpdater, _partyListHudUpdater);
            Interface.Inject(_modeSetter);

            _partyListHudUpdater.Enable();
            _modeSetter.Enable();
            _roleTracker.Enable();
            _nameplateUpdater.Enable();
            _npcNameplateFixer.Enable();
            _chatNameUpdater.Enable();
            _contextMenu.Enable();
        }

        public void Dispose()
        {
            _roleTracker.OnAssignedRolesUpdated -= OnAssignedRolesUpdated;

            _partyHUDView.Dispose();
            _partyListHudUpdater.Dispose();
            _chatNameUpdater.Dispose();
            _contextMenu.Dispose();
            _nameplateUpdater.Dispose();
            _npcNameplateFixer.Dispose();
            _roleTracker.Dispose();
            _modeSetter.Dispose();
            Interface.UiBuilder.Draw -= _ui.DrawSettingsWindow;
            Interface.UiBuilder.OpenConfigUi -= _ui.OpenSettings;
            _ui.Dispose();

            SeStringUtils.Dispose();
            XivApi.DisposeInstance();

            CommandManager.RemoveHandler(commandName);
            Configuration.OnSave -= OnConfigurationSave;
        }

        private void OnConfigurationSave()
        {
            _modeSetter.ForceRefresh();
            _nameplateUpdater.ForceRefresh();
        }

        private void OnAssignedRolesUpdated()
        {
            _nameplateUpdater.ForceRefresh();
        }

        private void OnCommand(string command, string arguments)
        {
            arguments = arguments.Trim().ToLower();

            if (arguments == "" || arguments == "config")
            {
                _ui.OpenSettings();
            }
            else if (arguments == "reset" || arguments == "r")
            {
                _roleTracker.ResetOccupations();
                _roleTracker.ResetAssignments();
                _roleTracker.CalculateUnassignedPartyRoles();
                ChatGui.Print("Occupations are reset, roles are auto assigned.");
            }
            else if (arguments == "dbg state")
            {
                ChatGui.Print($"Current mode is {_nameplateView.PartyMode}, party count {PartyList.Length}");
                ChatGui.Print(_roleTracker.DebugDescription());
            }
            else if (arguments == "dbg party")
            {
                foreach (var member in PartyList)
                {
                    var index = _partyHUDView.GetPartySlotIndex(member.ObjectId);
                    ChatGui.Print($"Party member index {index} name {member.Name} worldid {member.World.Id}");
                }

                ChatGui.Print(_partyHUDView.GetDebugInfo());
            }
        }
    }
}