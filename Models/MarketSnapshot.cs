using System;
using System.Collections.Generic;

namespace ConfluenceAIBot.Models
{
    public class TimeframeMetrics
    {
        public decimal Ema200        { get; set; }
        public double  Rsi14         { get; set; }
        public double  MacdHistogram { get; set; }
    }

    public class IchimokuCloudData
    {
        public decimal TenkanSen { get; set; }
        public decimal KijunSen { get; set; }
        public decimal SenkouSpanA { get; set; }
        public decimal SenkouSpanB { get; set; }
    }

    public class MarketSnapshot
    {
        public string   Symbol          { get; set; } = "BTCUSDT";
        public decimal  CurrentPrice    { get; set; }
        public string   TradingStyle    { get; set; } = "SWING"; 
        
        // NEW: News Data
        public bool         UseNews     { get; set; } = false;
        public List<string> RecentNews  { get; set; } = new();
        
        // ── 15m Base indicators ─────────────────────────────────────────────
        public decimal  Ema200_15m      { get; set; }
        public double   Rsi14_15m       { get; set; }
        public double   MacdLine_15m    { get; set; }
        public double   MacdSignal_15m  { get; set; }
        public double   MacdHistogram_15m { get; set; }
        
        public IchimokuCloudData Ichimoku_15m { get; set; } = new();
        
        // ── Higher Timeframes (MTFA) ────────────────────────────────────────
        public TimeframeMetrics Metrics1H { get; set; } = new();
        public TimeframeMetrics Metrics4H { get; set; } = new();
        public TimeframeMetrics Metrics1D { get; set; } = new();

        // ── Derived data ─────────────────────────────────────────────────────
        public GannLevels   GannLevels   { get; set; } = new();
        public SmcStructure SmcStructure { get; set; } = new();
        public DateTime     Timestamp    { get; set; } = DateTime.UtcNow;

        public bool IsValid => CurrentPrice > 0 && Ema200_15m > 0;
    }
}