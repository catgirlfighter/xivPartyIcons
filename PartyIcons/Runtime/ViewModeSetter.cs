using System.Linq;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using PartyIcons.View;
using PartyIcons.PluginServiceResources;

namespace PartyIcons.Runtime
{
    public class ViewModeSetter
    {
        private ClientState _clientState { get; set; }
        private DataManager _dataManager { get; set; }
        private ChatGui     _chatGui     { get; set; }

        private readonly NameplateView       _nameplateView;
        private readonly Configuration       _configuration;
        private readonly ChatNameUpdater     _chatNameUpdater;
        private readonly PartyListHUDUpdater _partyListHudUpdater;

        private ExcelSheet<ContentFinderCondition>? _contentFinderConditionsSheet;

        public ViewModeSetter(Plugin plugin, NameplateView nameplateView, Configuration configuration, ChatNameUpdater chatNameUpdater, PartyListHUDUpdater partyListHudUpdater)
        {
            _nameplateView = nameplateView;
            _configuration = configuration;
            _chatNameUpdater = chatNameUpdater;
            _partyListHudUpdater = partyListHudUpdater;
            _clientState = plugin.ClientState;
            _dataManager = plugin.DataManager;
            _chatGui = plugin.ChatGui;
        }

        public void Enable()
        {
            _contentFinderConditionsSheet = _dataManager.GameData?.GetExcelSheet<ContentFinderCondition>();

            ForceRefresh();
            _clientState.TerritoryChanged += OnTerritoryChanged;
        }

        public void ForceRefresh()
        {
            _nameplateView.OthersMode = _configuration.NameplateOthers;
            _chatNameUpdater.OthersMode = _configuration.ChatOthers;

            OnTerritoryChanged(null, 0);
        }

        public void Disable()
        {
            _clientState.TerritoryChanged -= OnTerritoryChanged;
        }

        public void Dispose()
        {
            Disable();
        }

        private void OnTerritoryChanged(object? sender, ushort e)
        {
            var content = _contentFinderConditionsSheet?.FirstOrDefault(t => t.TerritoryType.Row == _clientState.TerritoryType);
            if (content == null)
            {
                PluginLog.Information($"Content null {_clientState.TerritoryType}");
                _nameplateView.PartyMode = _configuration.NameplateOverworld;
                _chatNameUpdater.PartyMode = _configuration.ChatOverworld;
                return;
            }

            if (_configuration.ChatContentMessage)
            {
                _chatGui.Print($"Entering [{content.ContentMemberType.Row}-{content.RowId}] {content.Name}");
            }

            var memberType = content.ContentMemberType.Row;
            if (content.RowId == 16 || content.RowId == 15)
            {
                // Praetorium and Castrum Meridianum
                memberType = 2;
            }

            PluginLog.Debug($"Territory changed {content.Name} (id {content.RowId} type {content.ContentType.Row}, terr {_clientState.TerritoryType}, memtype {content.ContentMemberType.Row}, overriden {memberType})");

            switch (memberType)
            {
                case 2:
                    _nameplateView.PartyMode = _configuration.NameplateDungeon;
                    _chatNameUpdater.PartyMode = _configuration.ChatDungeon;
                    break;

                case 3:
                    _nameplateView.PartyMode = _configuration.NameplateRaid;
                    _chatNameUpdater.PartyMode = _configuration.ChatRaid;
                    break;

                case 4:
                    _nameplateView.PartyMode = _configuration.NameplateAllianceRaid;
                    _chatNameUpdater.PartyMode = _configuration.ChatAllianceRaid;
                    break;

                case 7:
                    _nameplateView.PartyMode = _configuration.NameplatePvP;
                    _chatNameUpdater.PartyMode = _configuration.ChatPvP;
                    break;

                default:
                    _nameplateView.PartyMode = _configuration.NameplateDungeon;
                    _chatNameUpdater.PartyMode = _configuration.ChatDungeon;
                    break;
            }

            _partyListHudUpdater.UpdateHUD = _nameplateView.PartyMode == NameplateMode.RoleLetters;
        }
    }
}