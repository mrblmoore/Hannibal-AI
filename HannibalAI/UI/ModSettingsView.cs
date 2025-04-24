using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using HannibalAI.Config;
using HannibalAI.Services;

namespace HannibalAI.UI
{
    public class ModSettingsView : ScreenBase
    {
        private readonly Mission _mission;
        private readonly AIService _aiService;
        private GauntletLayer _gauntletLayer;
        private ModSettingsVM _dataSource;
        private ModConfig _config;

        public ModSettingsView(Mission mission, AIService aiService)
        {
            _mission = mission;
            _aiService = aiService;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            try
            {
                _config = ModConfig.Load();
                _dataSource = new ModSettingsVM(_config);
                _gauntletLayer = new GauntletLayer(100);
                _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                AddLayer(_gauntletLayer);
                _gauntletLayer.LoadMovie("ModSettings", _dataSource);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error showing settings: {ex.Message}");
            }
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();
            RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
            _dataSource = null;
        }
    }

    public class ModSettingsVM : ViewModel
    {
        private readonly ModConfig _config;
        private string _apiKey;
        private string _logLevel;
        private bool _enableDebugMode;
        private int _updateInterval;
        private bool _terrainAnalysis;
        private bool _weatherAnalysis;
        private bool _formationAnalysis;
        private bool _saveBattleSnapshots;
        private bool _detailedLogging;
        private bool _saveReplayData;

        public ModSettingsVM(ModConfig config)
        {
            _config = config;
            _apiKey = config.APIKey;
            _logLevel = config.LogLevel;
            _enableDebugMode = config.EnableDebugMode;
            _updateInterval = config.BattleAnalysis.UpdateIntervalSeconds;
            _terrainAnalysis = config.BattleAnalysis.TerrainAnalysisEnabled;
            _weatherAnalysis = config.BattleAnalysis.WeatherAnalysisEnabled;
            _formationAnalysis = config.BattleAnalysis.FormationAnalysisEnabled;
            _saveBattleSnapshots = config.Debug.SaveBattleSnapshots;
            _detailedLogging = config.Debug.DetailedLogging;
            _saveReplayData = config.Debug.SaveReplayData;
        }

        [DataSourceProperty]
        public string APIKey
        {
            get => _apiKey;
            set
            {
                if (value != _apiKey)
                {
                    _apiKey = value;
                    _config.APIKey = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public string LogLevel
        {
            get => _logLevel;
            set
            {
                if (value != _logLevel)
                {
                    _logLevel = value;
                    _config.LogLevel = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public bool EnableDebugMode
        {
            get => _enableDebugMode;
            set
            {
                if (value != _enableDebugMode)
                {
                    _enableDebugMode = value;
                    _config.EnableDebugMode = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public int UpdateInterval
        {
            get => _updateInterval;
            set
            {
                if (value != _updateInterval)
                {
                    _updateInterval = value;
                    _config.BattleAnalysis.UpdateIntervalSeconds = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public bool TerrainAnalysis
        {
            get => _terrainAnalysis;
            set
            {
                if (value != _terrainAnalysis)
                {
                    _terrainAnalysis = value;
                    _config.BattleAnalysis.TerrainAnalysisEnabled = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public bool WeatherAnalysis
        {
            get => _weatherAnalysis;
            set
            {
                if (value != _weatherAnalysis)
                {
                    _weatherAnalysis = value;
                    _config.BattleAnalysis.WeatherAnalysisEnabled = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public bool FormationAnalysis
        {
            get => _formationAnalysis;
            set
            {
                if (value != _formationAnalysis)
                {
                    _formationAnalysis = value;
                    _config.BattleAnalysis.FormationAnalysisEnabled = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public bool SaveBattleSnapshots
        {
            get => _saveBattleSnapshots;
            set
            {
                if (value != _saveBattleSnapshots)
                {
                    _saveBattleSnapshots = value;
                    _config.Debug.SaveBattleSnapshots = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public bool DetailedLogging
        {
            get => _detailedLogging;
            set
            {
                if (value != _detailedLogging)
                {
                    _detailedLogging = value;
                    _config.Debug.DetailedLogging = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public bool SaveReplayData
        {
            get => _saveReplayData;
            set
            {
                if (value != _saveReplayData)
                {
                    _saveReplayData = value;
                    _config.Debug.SaveReplayData = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }
    }
} 