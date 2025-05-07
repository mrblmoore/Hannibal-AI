using Newtonsoft.Json;

namespace HannibalAI.Config
{
    public class ModConfig
    {
        [JsonProperty]
        public bool Debug { get; set; } = false;

        [JsonProperty]
        public float CommanderMemoryMaxValue { get; set; } = 1.0f;

        [JsonProperty]
        public float CommanderMemoryMinValue { get; set; } = 0.0f;

        [JsonProperty]
        public float CommanderMemoryDuration { get; set; } = 300f; // in seconds

        [JsonProperty]
        public float CommanderMemoryDecayRate { get; set; } = 0.001f; // units per second

        [JsonProperty]
        public bool EnableAIEnhancements { get; set; } = true;

        [JsonProperty]
        public float AggressivenessMultiplier { get; set; } = 1.0f;

        [JsonProperty]
        public float CautionMultiplier { get; set; } = 1.0f;
    }
}
