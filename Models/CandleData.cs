namespace ConfluenceAIBot.Models
{
    /// <summary>Internal OHLCV candle — decoupled from Binance.Net types.</summary>
    public class CandleData
    {
        public DateTime OpenTime { get; set; }
        public decimal  Open     { get; set; }
        public decimal  High     { get; set; }
        public decimal  Low      { get; set; }
        public decimal  Close    { get; set; }
        public decimal  Volume   { get; set; }
    }
}
