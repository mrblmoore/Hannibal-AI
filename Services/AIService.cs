using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using HannibalAI.Battle;
using HannibalAI.Config;
using TaleWorlds.Library;

namespace HannibalAI.Services
{
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly ModConfig _config;
        private readonly CommanderMemoryService _memoryService;

        public AIService(string endpoint, string apiKey)
        {
            _httpClient = new HttpClient();
            _endpoint = endpoint;
            _apiKey = apiKey;
            _config = ModConfig.Instance;
            _memoryService = new CommanderMemoryService();
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

                var request = new
                {
                    model = _config.AIService.ModelVersion,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = BuildSystemPrompt(commanderProfile)
                        },
                        new
                        {
                            role = "user",
                            content = JsonConvert.SerializeObject(new
                            {
                                battle_state = snapshot,
                                commander_traits = commanderProfile.Traits,
                                recent_battles = recentBattles,
                                terrain = snapshot.Terrain,
                                weather = snapshot.Weather
                            })
                        }
                    },
                    max_tokens = _config.AIService.MaxTokens,
                    temperature = _config.AIService.Temperature
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json"
                );

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.PostAsync(_endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var aiResponse = JsonConvert.DeserializeObject<AIResponse>(responseContent);

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
    }

    public class AIDecision
    {
        public string Action { get; set; }
        public AICommand[] Commands { get; set; }
        public string Reasoning { get; set; }
        public List<string> TacticsUsed { get; set; }
    }

    public class AICommand
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public object[] Parameters { get; set; }
    }

    internal class AIResponse
    {
        public Choice[] choices { get; set; }
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
} 