using System.Collections.Generic;
using HannibalAI.Battle;
using HannibalAI.Config;

namespace HannibalAI.Services
{
    public class CommanderMemoryService
    {
        private readonly Dictionary<string, float> _commanderMemory;
        private readonly ModConfig _config;

        public CommanderMemoryService()
        {
            _commanderMemory = new Dictionary<string, float>();
            _config = ModConfig.Instance;
        }

        public void RecordCommanderInteraction(string commanderId)
        {
            if (string.IsNullOrEmpty(commanderId))
                return;

            if (!_commanderMemory.ContainsKey(commanderId))
            {
                _commanderMemory[commanderId] = 0f;
            }

            _commanderMemory[commanderId] += 1f;
            CapMemoryValue(commanderId);
        }

        public float GetCommanderMemory(string commanderId)
        {
            if (string.IsNullOrEmpty(commanderId) || !_commanderMemory.ContainsKey(commanderId))
                return 0f;

            return _commanderMemory[commanderId];
        }

        public void DecayMemoryOverTime()
        {
            var keys = new List<string>(_commanderMemory.Keys);
            foreach (var commanderId in keys)
            {
                _commanderMemory[commanderId] -= _config.CommanderMemoryDecayRate;
                if (_commanderMemory[commanderId] < _config.CommanderMemoryMinValue)
                {
                    _commanderMemory[commanderId] = _config.CommanderMemoryMinValue;
                }
            }
        }

        private void CapMemoryValue(string commanderId)
        {
            if (_commanderMemory[commanderId] > _config.CommanderMemoryMaxValue)
            {
                _commanderMemory[commanderId] = _config.CommanderMemoryMaxValue;
            }
        }
    }
}
