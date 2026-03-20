namespace ConfluenceAIBot.Models
{
    /// <summary>
    /// Smart Money Concepts market structure result from 5m timeframe analysis.
    /// Identifies swing highs/lows and classifies BOS / CHoCH events.
    /// </summary>
    public class SmcStructure
    {
        public decimal MostRecentSwingHigh  { get; set; }
        public decimal MostRecentSwingLow   { get; set; }
        public decimal PreviousSwingHigh    { get; set; }
        public decimal PreviousSwingLow     { get; set; }

        /// <summary>Human-readable classification: "Bullish BOS — HH/HL", "Bearish CHoCH", etc.</summary>
        public string MarketStructure  { get; set; } = "Awaiting Data…";
        public string StructureDetail  { get; set; } = "";

        public bool IsBullish      { get; set; }
        public bool IsBearish      { get; set; }
        public bool IsConsolidating { get; set; }
    }
}
