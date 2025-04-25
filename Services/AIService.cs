using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using HannibalAI.Battle;
using HannibalAI.Config;
using HannibalAI.Command;
using TaleWorlds.Library;
using Newtonsoft.Json.Linq;

namespace HannibalAI.Services
{
    public class CommanderContext
    {
        public string CommanderId { get; set; }
        public Dictionary<string, float> Traits { get; set; }
        public List<string> RecentTactics { get; set; }
        public float BattleExperience { get; set; }
    }

    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly ModConfig _config;
        private readonly CommanderMemoryService _memoryService;
        private readonly CommanderContext _context;

        public AIService(string endpoint, string apiKey)
        {
            _httpClient = new HttpClient();
            _endpoint = endpoint;
            _apiKey = apiKey;
            _config = ModConfig.Instance;
            _memoryService = new CommanderMemoryService();
            _context = new CommanderContext();
        }

        public async Task<AIDecision> ProcessBattleSnapshot(BattleSnapshot snapshot)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    return await FallbackService.Instance.GetDecision(snapshot);
                }

                var commanderProfile = _memoryService.GetCommanderProfile(snapshot.CommanderId);
                var recentBattles = _memoryService.GetRecentBattles(snapshot.CommanderId, 3);

                var request = new AIRequest
                {
                    BattleSnapshot = snapshot,
                    CommanderContext = _context
                };

                var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings 
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Converters = new List<JsonConverter> { new Vec3Converter() }
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.PostAsync(_endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var aiResponse = JsonConvert.DeserializeObject<AIResponse>(responseContent, new JsonSerializerSettings 
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Converters = new List<JsonConverter> { new Vec3Converter() }
                });

                var decision = ParseAIResponse(aiResponse);
                
                // Record the tactics used for learning
                if (decision != null && decision.Commands != null)
                {
                    var tacticsUsed = decision.Commands
                        .Where(c => !string.IsNullOrEmpty(c.Value))
                        .Select(c => c.Value.ToLower())
                        .ToList();

                    // We'll update the outcome later when the battle ends
                    decision.TacticsUsed = tacticsUsed;
                }

                return decision;
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error in AI processing: {ex.Message}");
                return await FallbackService.Instance.GetDecision(snapshot);
            }
        }

        public AIDecision ProcessBattleSnapshotSync(BattleSnapshot snapshot)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    return FallbackService.Instance.GetDecisionSync(snapshot);
                }

                var commanderProfile = _memoryService.GetCommanderProfile(snapshot.CommanderId);
                var recentBattles = _memoryService.GetRecentBattles(snapshot.CommanderId, 3);

                var request = new AIRequest
                {
                    BattleSnapshot = snapshot,
                    CommanderContext = _context
                };

                var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings 
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Converters = new List<JsonConverter> { new Vec3Converter() }
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                var response = _httpClient.PostAsync(_endpoint, content).Result;
                response.EnsureSuccessStatusCode();

                var responseContent = response.Content.ReadAsStringAsync().Result;
                var aiResponse = JsonConvert.DeserializeObject<AIResponse>(responseContent, new JsonSerializerSettings 
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Converters = new List<JsonConverter> { new Vec3Converter() }
                });

                var decision = ParseAIResponse(aiResponse);
                
                if (decision != null && decision.Commands != null)
                {
                    var tacticsUsed = decision.Commands
                        .Where(c => !string.IsNullOrEmpty(c.Value))
                        .Select(c => c.Value.ToLower())
                        .ToList();

                    decision.TacticsUsed = tacticsUsed;
                }

                return decision;
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error in AI processing: {ex.Message}");
                return FallbackService.Instance.GetDecisionSync(snapshot);
            }
        }

        private string BuildSystemPrompt(CommanderProfile profile)
        {
            var traits = profile.Traits;
            var personality = new List<string>();

            if (traits["Aggression"] > 0.7f) personality.Add("highly aggressive");
            else if (traits["Aggression"] < 0.3f) personality.Add("cautious");
            
            if (traits["Innovation"] > 0.7f) personality.Add("innovative");
            else if (traits["Innovation"] < 0.3f) personality.Add("traditional");
            
            if (traits["Adaptability"] > 0.7f) personality.Add("highly adaptable");
            else if (traits["Adaptability"] < 0.3f) personality.Add("rigid");

            var personalityDesc = personality.Count > 0 
                ? $"You are a {string.Join(", ", personality)} commander. "
                : "You are a balanced commander. ";

            return $"You are a military commander AI making tactical decisions in a medieval battle. " +
                   personalityDesc +
                   "Analyze the battle situation and provide specific commands for formations and movements. " +
                   "Consider the terrain, weather, and your past battle experiences. " +
                   "Your decisions should reflect your personality traits and learned tactics.";
        }

        private AIDecision ParseAIResponse(AIResponse response)
        {
            try
            {
                var content = response.choices[0].message.content;
                var decision = JsonConvert.DeserializeObject<AIDecision>(content);

                if (decision?.Commands == null || decision.Commands.Length == 0)
                {
                    throw new Exception("Invalid AI decision format");
                }

                return decision;
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error parsing AI response: {ex.Message}");
                throw;
            }
        }

        public void RecordBattleOutcome(string commanderId, BattleSnapshot finalSnapshot, AIDecision lastDecision, bool victory)
        {
            var outcome = new BattleOutcome
            {
                Result = victory ? "Victory" : "Defeat",
                PlayerCasualties = CountCasualties(finalSnapshot.PlayerUnits),
                EnemyCasualties = CountCasualties(finalSnapshot.EnemyUnits),
                TacticsUsed = lastDecision?.TacticsUsed ?? new List<string>()
            };

            _memoryService.RecordBattleOutcome(commanderId, outcome);
        }

        private int CountCasualties(List<UnitData> units)
        {
            return units?.Count(u => u.Health <= 0) ?? 0;
        }

        public async Task<AIDecision> GetDecisionAsync(BattleSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            try
            {
                // This is a placeholder - implement your AI service call here
                // For now, we'll return a basic decision
                return new AIDecision
                {
                    Action = "Default",
                    Reasoning = "Using default strategy",
                    Commands = Array.Empty<AICommand>()
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    internal class AIResponse
    {
        public List<Choice> choices { get; set; }
    }

    internal class Choice
    {
        public Message message { get; set; }
    }

    internal class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class Vec3Converter : JsonConverter<Vec3>
    {
        public override Vec3 ReadJson(JsonReader reader, Type objectType, Vec3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            return new Vec3(
                obj["X"].Value<float>(),
                obj["Y"].Value<float>(),
                obj["Z"].Value<float>()
            );
        }

        public override void WriteJson(JsonWriter writer, Vec3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WritePropertyName("Z");
            writer.WriteValue(value.Z);
            writer.WriteEndObject();
        }
    }

    public class AIRequest
    {
        public BattleSnapshot BattleSnapshot { get; set; }
        public CommanderContext CommanderContext { get; set; }
    }

    public class BattleCommand
    {
        public string Type { get; set; }
        public int FormationIndex { get; set; }
        public Vec3 TargetPosition { get; set; }
        public string AdditionalData { get; set; }
    }
} 