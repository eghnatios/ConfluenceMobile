using Newtonsoft.Json;

namespace ConfluenceAIBot.Models
{
    public class GeminiSignal
    {
        [JsonProperty("bias")]
        public string Bias { get; set; }

        [JsonProperty("setup_type")]
        public string SetupType { get; set; }

        [JsonProperty("entry_zone")]
        public string EntryZone { get; set; }
        
        [JsonProperty("take_profit_1")]
        public string TakeProfit1 { get; set; }

        [JsonProperty("take_profit_2")]
        public string TakeProfit2 { get; set; }

        [JsonProperty("take_profit_3")]
        public string TakeProfit3 { get; set; }
        
        [JsonProperty("stop_loss")]
        public string StopLoss { get; set; }

        [JsonProperty("confidence_score")]
        public int ConfidenceScore { get; set; }

        public bool IsBullish => Bias?.ToUpper() == "BULLISH";
        public bool IsBearish => Bias?.ToUpper() == "BEARISH";
        
        // NEW: Detects when the AI refuses to trade
        public bool IsWait => Bias?.ToUpper() == "WAIT";
    }
}