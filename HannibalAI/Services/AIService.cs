using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HannibalAI.Battle;
using System.Collections.Generic;
using System.Linq;

namespace HannibalAI.Services
{
    public class AIService
    {
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly CommanderMemoryService _memoryService;

        public AIService(string endpoint, string apiKey)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _memoryService = new CommanderMemoryService();
        }

        public async Task<string> ProcessBattleSnapshot(BattleSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                {
                    LogError("Cannot process null battle snapshot");
                    return null;
                }

                var json = snapshot.ToJson();
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(_endpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    LogError($"API request failed with status code: {response.StatusCode}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var aiResponse = JsonConvert.DeserializeObject<AIResponse>(responseContent);
                
                return aiResponse?.Command;
            }
            catch (Exception ex)
            {
                LogError($"Error processing battle snapshot: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private void LogError(string message)
        {
            try
            {
                File.AppendAllText("hannibal_ai_errors.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
            catch
            {
                // Ignore logging errors
            }
        }

        private async Task<CommanderContext> GetCommanderContext(BattleSnapshot snapshot)
        {
            // Get commander ID from the battle data
            var commanderId = GetCommanderId(snapshot);
            var memory = _memoryService.GetCommanderMemory(commanderId);
            
            // Get recent encounters and successful tactics
            var recentEncounters = _memoryService.GetRecentEncounters(commanderId);
            var successfulTactics = memory.TacticsSuccessRate
                .Where(kvp => kvp.Value > 0)
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            return new CommanderContext
            {
                CommanderId = commanderId,
                PreviousEncounters = recentEncounters.Select(e => new BattleHistory
                {
                    Date = e.Date,
                    Outcome = e.Outcome,
                    TacticsUsed = e.TacticsUsed.ToArray()
                }).ToList(),
                SuccessfulTactics = successfulTactics
            };
        }

        private string GetCommanderId(BattleSnapshot snapshot)
        {
            // TODO: Implement proper commander identification
            // For now, use a simple hash of the enemy units
            var enemyUnits = string.Join(",", snapshot.EnemyUnits.Select(u => GetUnitType(u)));
            return enemyUnits.GetHashCode().ToString();
        }

        private string GetUnitType(UnitData unit)
        {
            if (unit.IsRanged && unit.IsMounted)
                return "HorseArcher";
            if (unit.IsRanged)
                return "Ranged";
            if (unit.IsMounted)
                return "Cavalry";
            return "Infantry";
        }

        public void RecordBattleOutcome(string commanderId, BattleSnapshot snapshot, string outcome, List<string> tacticsUsed)
        {
            _memoryService.UpdateCommanderMemory(commanderId, snapshot, outcome, tacticsUsed);
        }

        private class AIResponse
        {
            [JsonProperty("command")]
            public string Command { get; set; }
        }
    }

    public class AIRequest
    {
        public BattleSnapshot BattleData { get; set; }
        public CommanderContext Context { get; set; }
    }

    public class AIResponse
    {
        public string Action { get; set; }
        public int[] UnitIds { get; set; }
        public float[] TargetPosition { get; set; }
        public string Reasoning { get; set; }
    }

    public class CommanderContext
    {
        public string CommanderId { get; set; }
        public List<BattleHistory> PreviousEncounters { get; set; }
        public List<string> SuccessfulTactics { get; set; }
    }

    public class BattleHistory
    {
        public DateTime Date { get; set; }
        public string Outcome { get; set; }
        public string[] TacticsUsed { get; set; }
    }
} 