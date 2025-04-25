using System;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using HannibalAI.Command;
using HannibalAI.Services;
using HannibalAI.Config;
using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class AICommander
    {
        private readonly AIService _aiService;
        private readonly FallbackService _fallbackService;
        private readonly ModConfig _config;

        public int CommanderId { get; private set; }
        public string Name { get; private set; }
        public float Experience { get; private set; }

        public AICommander(int commanderId, string name, float experience)
        {
            CommanderId = commanderId;
            Name = name;
            Experience = experience;
            _config = ModConfig.Instance;
            _aiService = new AIService(_config.AIEndpoint, _config.APIKey);
            _fallbackService = new FallbackService();
        }

        public async Task<AIDecision> MakeDecisionAsync(BattleSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            try
            {
                // Try to get decision from AI service first
                var decision = await _aiService.GetDecisionAsync(snapshot);
                if (decision != null && decision.Commands != null && decision.Commands.Length > 0)
                {
                    return decision;
                }

                // Fall back to local decision making if AI service fails
                return await _fallbackService.GetDecisionAsync(snapshot);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error making AI decision: {ex.Message}");
                return await _fallbackService.GetDecisionAsync(snapshot);
            }
        }

        public AIDecision MakeDecision(BattleSnapshot snapshot)
        {
            // For synchronous calls, we'll wait for the async operation
            return MakeDecisionAsync(snapshot).GetAwaiter().GetResult();
        }

        public AICommand MakeDecision(BattleSnapshot snapshot)
        {
            // TODO: Implement decision-making logic
            return new MoveFormationCommand(new Vec3(0, 0, 0), 1.0f);
        }
    }
} 