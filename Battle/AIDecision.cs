using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class AIDecision
    {
        public string DecisionType { get; set; }
        public Vec3 TargetPosition { get; set; }
        public float Confidence { get; set; }
        public string Reasoning { get; set; }

        public AIDecision(string decisionType, Vec3 targetPosition, float confidence, string reasoning)
        {
            DecisionType = decisionType;
            TargetPosition = targetPosition;
            Confidence = confidence;
            Reasoning = reasoning;
        }
    }
} 