# Hannibal AI - Mount & Blade II: Bannerlord Mod

An advanced AI mod for Mount & Blade II: Bannerlord that enhances battle tactics and unit control using intelligent decision-making algorithms.

## Features

- Intelligent battle analysis and tactical decision making
- Dynamic formation control based on battlefield conditions
- Adaptive unit positioning and movement
- Weather and terrain analysis for tactical advantages
- Customizable AI behavior through configuration

## Requirements

- Mount & Blade II: Bannerlord (latest version)
- .NET Framework 4.7.2 or higher

## Installation

1. Download the latest release from the releases page
2. Extract the contents to your Bannerlord Modules folder:
   ```
   {GAME_PATH}/Modules/HannibalAI/
   ```
3. Copy `hannibal_ai_config.template.json` to `hannibal_ai_config.json` and configure your settings
4. Launch the game and enable the HannibalAI mod in the launcher

## Configuration

The mod can be configured through the `hannibal_ai_config.json` file. Here are the key settings:

### AI Service Configuration
- `AIEndpoint`: The endpoint URL for the AI service
- `APIKey`: Your API key for accessing the service
- `LogLevel`: Logging detail level (Debug, Info, Warning, Error)

### Unit Preferences
Configure preferred formations and behavior for different unit types:
```json
"PreferredFormations": {
  "Infantry": "Line",
  "Ranged": "Loose",
  "Cavalry": "Column",
  "HorseArcher": "Scatter"
}
```

### Battle Analysis Settings
- `UpdateIntervalSeconds`: How often the AI updates its analysis
- `TerrainAnalysisEnabled`: Consider terrain in decision making
- `WeatherAnalysisEnabled`: Consider weather conditions
- `FormationAnalysisEnabled`: Analyze formation effectiveness

## Usage

The mod automatically enhances AI behavior during battles. No manual intervention is required, but you can:

1. Configure unit preferences in the config file
2. Monitor AI decisions through the log file
3. Fine-tune aggressiveness levels per unit type
4. Enable debug features for detailed analysis

## Debug Features

Enable debug features in the config file:
```json
"Debug": {
  "SaveBattleSnapshots": false,
  "DetailedLogging": false,
  "SaveReplayData": false
}
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Mount & Blade II: Bannerlord development team
- Contributors and testers
- Community feedback and support

## Support

For issues and feature requests, please use the GitHub issues tracker. 