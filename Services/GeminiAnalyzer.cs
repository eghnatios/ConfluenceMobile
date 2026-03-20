using System.Net.Http;
using System.Text;
using ConfluenceAIBot.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConfluenceAIBot.Services
{
    public sealed class GeminiAnalyzer : IDisposable
    {
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=";
        private readonly HttpClient _http;

        public GeminiAnalyzer() { _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) }; }

        public async Task<GeminiSignal> AnalyzeAsync(string apiKey, MarketSnapshot snap, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("Gemini API key is required.", nameof(apiKey));

            string prompt  = BuildPrompt(snap);
            string payload = BuildRequestPayload(prompt, snap.TradingStyle);

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + apiKey);
            request.Content   = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            string raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Gemini API error {(int)response.StatusCode}: {ExtractGeminiError(raw)}");

            string jsonText = ExtractTextFromGeminiResponse(raw);
            string cleanJson = ExtractJsonObject(jsonText);

            return JsonConvert.DeserializeObject<GeminiSignal>(cleanJson) 
                   ?? throw new InvalidOperationException("Gemini returned null signal.");
        }

        private static string GetHtfSummary(string label, TimeframeMetrics m, decimal price)
        {
            string ema = price >= m.Ema200 ? "ABOVE" : "BELOW";
            string macd = m.MacdHistogram > 0 ? "BULLISH" : "BEARISH";
            return $"{label} : Price {ema} 200 EMA | RSI: {m.Rsi14:F1} | MACD: {macd}";
        }

        private static string BuildPrompt(MarketSnapshot s)
        {
            double emaDelta = s.Ema200_15m > 0 ? (double)((s.CurrentPrice - s.Ema200_15m) / s.Ema200_15m * 100m) : 0;
            string emaRelation = s.CurrentPrice >= s.Ema200_15m ? "ABOVE" : "BELOW";
            string macdBias = s.MacdHistogram_15m > 0 ? "Bullish" : "Bearish";

            string ichiCloudStatus = s.CurrentPrice > s.Ichimoku_15m.SenkouSpanA && s.CurrentPrice > s.Ichimoku_15m.SenkouSpanB ? "ABOVE CLOUD (Bullish)" :
                                     s.CurrentPrice < s.Ichimoku_15m.SenkouSpanA && s.CurrentPrice < s.Ichimoku_15m.SenkouSpanB ? "BELOW CLOUD (Bearish)" : "INSIDE CLOUD (Consolidating)";

            // NEW: The News Block
            string newsSection = s.UseNews && s.RecentNews.Any()
                ? "\nLIVE FUNDAMENTAL NEWS HEADLINES:\n" + string.Join("\n", s.RecentNews.Select(n => $"  - {n}")) + "\n"
                : "";

            string styleInstruction = s.TradingStyle == "SCALP"
                ? "You are an expert cryptocurrency SCALPER. Goal: find quick, short-term scalps. Base your PRIMARY bias on the 15m momentum, Ichimoku Cloud, and 5m SMC structure."
                : "You are an expert cryptocurrency SWING TRADER. Goal: find high-probability, higher-timeframe swing trades. Base your PRIMARY bias heavily on the 1H, 4H, and 1D macro trends.";

            return $$"""
            You combine Smart Money Concepts (SMC), W.D. Gann harmonics, Ichimoku Kinko Hyo, and Multi-Timeframe Analysis (MTFA).
            {{styleInstruction}}

            LIVE MARKET DATA — {{s.Symbol}} — {{s.Timestamp:yyyy-MM-dd HH:mm}} UTC
            ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            MACRO TREND (1H, 4H, 1D):
              {{GetHtfSummary("1 Hour", s.Metrics1H, s.CurrentPrice)}}
              {{GetHtfSummary("4 Hour", s.Metrics4H, s.CurrentPrice)}}
              {{GetHtfSummary("1 Day ", s.Metrics1D, s.CurrentPrice)}}

            MICRO TREND & MOMENTUM (15m):
              Current Price  : ${{s.CurrentPrice:N2}}
              200 EMA        : ${{s.Ema200_15m:N2}} (Price is {{emaRelation}} by {{Math.Abs(emaDelta):F2}}%)
              RSI (14)       : {{s.Rsi14_15m:F2}}
              MACD Histogram : {{s.MacdHistogram_15m:F4}} [{{macdBias}}]

            ICHIMOKU CLOUD (15m):
              Cloud Position : {{ichiCloudStatus}}

            SMC STRUCTURE (5m timeframe):
              Classification : {{s.SmcStructure.MarketStructure}}
              Swing High     : ${{s.SmcStructure.MostRecentSwingHigh:N2}}
              Swing Low      : ${{s.SmcStructure.MostRecentSwingLow:N2}}

            GANN HARMONIC LEVELS (Resistance and Support):
              R3 (+4/8) : ${{s.GannLevels.HalfAbove:N2}}
              R2 (+2/8) : ${{s.GannLevels.QuarterAbove:N2}}
              R1 (+1/8) : ${{s.GannLevels.EighthAbove:N2}}
              PIVOT     : ${{s.GannLevels.GannBase:N2}}
              S1 (-1/8) : ${{s.GannLevels.EighthBelow:N2}}
              S2 (-2/8) : ${{s.GannLevels.QuarterBelow:N2}}
              S3 (-4/8) : ${{s.GannLevels.HalfBelow:N2}}
            {{newsSection}}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

            TASK RULES:
            1. Analyze the confluence of technicals and (if provided) the news sentiment.
            2. IF technicals strongly conflict, OR if price is stuck inside the Ichimoku Cloud, OR if negative fundamental news invalidates the technical setup, your bias MUST be 'WAIT'.
            3. If your bias is 'WAIT', you MUST set entry_zone, take_profit_1, 2, 3, and stop_loss all exactly to 'N/A'.
            """;
        }

        private static string BuildRequestPayload(string prompt, string tradingStyle)
        {
            double temp = tradingStyle == "SCALP" ? 0.1 : 0.25;

            var body = new
            {
                system_instruction = new { parts = new[] { new { text = $"You are a quantitative crypto risk manager. Provide pure JSON only." } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
                generationConfig = new
                {
                    temperature = temp,
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            // NEW: Added WAIT to the schema
                            bias = new { type = "STRING", @enum = new[] { "BULLISH", "BEARISH", "NEUTRAL", "WAIT" } },
                            setup_type = new { type = "STRING", description = $"Max 80 chars describing the setup or the reason for WAIT." },
                            entry_zone = new { type = "STRING" },
                            take_profit_1 = new { type = "STRING" },
                            take_profit_2 = new { type = "STRING" },
                            take_profit_3 = new { type = "STRING" },
                            stop_loss = new { type = "STRING" },
                            confidence_score = new { type = "INTEGER" }
                        },
                        required = new[] { "bias", "setup_type", "entry_zone", "take_profit_1", "take_profit_2", "take_profit_3", "stop_loss", "confidence_score" }
                    }
                }
            };
            return JsonConvert.SerializeObject(body);
        }

        private static string ExtractTextFromGeminiResponse(string raw)
        {
            var root = JObject.Parse(raw);
            return root["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.Value<string>() ?? "";
        }

        private static string ExtractJsonObject(string text) => text.Replace("```json", "").Replace("```", "").Trim();
        private static string ExtractGeminiError(string raw) { try { return JObject.Parse(raw)["error"]?["message"]?.Value<string>() ?? raw; } catch { return raw; } }
        public void Dispose() => _http.Dispose();
    }
}