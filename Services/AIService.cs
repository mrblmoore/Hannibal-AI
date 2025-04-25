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
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly ModConfig _config;

        public AIService(string endpoint, string apiKey)
        {
            _httpClient = new HttpClient();
            _endpoint = endpoint;
            _apiKey = apiKey;
            _config = ModConfig.Instance;
        }

        public async Task<AIDecision> GetDecisionAsync(BattleSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    return await FallbackService.Instance.GetDecisionAsync(snapshot);
                }

                var request = new AIRequest
                {
                    BattleSnapshot = snapshot
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

                return ParseAIResponse(aiResponse);
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[HannibalAI] Error in AI processing: {ex.Message}");
                return await FallbackService.Instance.GetDecisionAsync(snapshot);
            }
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
                TaleWorlds.Library.Debug.Print($"[HannibalAI] Error parsing AI response: {ex.Message}");
                throw;
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
    }
} 