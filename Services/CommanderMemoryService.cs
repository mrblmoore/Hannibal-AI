using System.Collections.Generic;
using HannibalAI.Battle;
using HannibalAI.Config;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Services
{
    public class CommanderMemoryService
    {
        private readonly Dictionary<string, float> _commanderMemories;
        private readonly ModConfig _config;

        public CommanderMemoryService(ModConfig config)
        {
            _config = config ?? new ModConfig();
            _commanderMemories = new Dictionary<string, float>();
        }

        public void UpdateMemory(string commanderId, float memoryValue)
        {
            if (string.IsNullOrEmpty(commanderId))
                return;

            if (!_commanderMemories.ContainsKey(commanderId))
            {
                _commanderMemories[commanderId] = memoryValue;
            }
            else
            {
                _commanderMemories[commanderId] += memoryValue;
                _commanderMemories[commanderId] = ClampMemory(_commanderMemories[commanderId]);
            }
        }

        public float GetMemory(string commanderId)
        {
            if (string.IsNullOrEmpty(commanderId))
                return 0f;

            return _commanderMemories.TryGetValue(commanderId, out var memory) ? memory : 0f;
        }

        public void DecayMemory(string commanderId, float deltaTime)
        {
            if (string.IsNullOrEmpty(commanderId) || !_commanderMemories.ContainsKey(commanderId))
                return;

            float decayAmount = _config.CommanderMemoryDecayRate * deltaTime;
            _commanderMemories[commanderId] -= decayAmount;
            _commanderMemories[commanderId] = ClampMemory(_commanderMemories[commanderId]);
        }

        private float ClampMemory(float memory)
        {
            if (memory > _config.CommanderMemoryMaxValue)
                return _config.CommanderMemoryMaxValue;

            if (memory < _config.CommanderMemoryMinValue)
                return _config.CommanderMemoryMinValue;

            return memory;
        }

        public void ResetMemory(string commanderId)
        {
            if (string.IsNullOrEmpty(commanderId))
                return;

            if (_commanderMemories.ContainsKey(commanderId))
            {
                _commanderMemories[commanderId] = _config.CommanderMemoryMinValue;
            }
        }

        public Dictionary<string, float> GetAllMemories()
        {
            return _commanderMemories;
        }
    }
}
