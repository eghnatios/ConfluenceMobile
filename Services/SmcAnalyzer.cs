using ConfluenceAIBot.Models;

namespace ConfluenceAIBot.Services
{
    public static class SmcAnalyzer
    {
        private const int SwingLookback = 3;

        public static SmcStructure Analyze(List<CandleData> candles)
        {
            if (candles == null || candles.Count < SwingLookback * 2 + 5)
                return new SmcStructure { MarketStructure = "Not enough data" };

            var swingHighs = new List<CandleData>();
            var swingLows  = new List<CandleData>();

            // ── Detect pivots ────────────────────────────────────────────────
            int N = SwingLookback;
            for (int i = N; i < candles.Count - N; i++)
            {
                bool isHigh = true, isLow = true;
                for (int j = 1; j <= N; j++)
                {
                    // Allow equal highs/lows. Only invalidate if neighbor is strictly higher/lower.
                    if (candles[i].High < candles[i - j].High || candles[i].High < candles[i + j].High)
                        isHigh = false;

                    if (candles[i].Low > candles[i - j].Low || candles[i].Low > candles[i + j].Low)
                        isLow = false;
                }
                if (isHigh) swingHighs.Add(candles[i]);
                if (isLow)  swingLows.Add(candles[i]);
            }

            if (swingHighs.Count < 2 || swingLows.Count < 2)
                return new SmcStructure { MarketStructure = "Insufficient swing points" };

            // ── Use the two most recent pivots ───────────────────────────────
            var sh1 = swingHighs[^2]; // previous swing high
            var sh2 = swingHighs[^1]; // most recent swing high
            var sl1 = swingLows[^2];  // previous swing low
            var sl2 = swingLows[^1];  // most recent swing low

            bool higherHigh = sh2.High > sh1.High;
            bool higherLow  = sl2.Low  > sl1.Low;
            bool lowerHigh  = sh2.High < sh1.High;
            bool lowerLow   = sl2.Low  < sl1.Low;

            decimal currentPrice = candles[^1].Close;

            // ── Detect BOS / CHoCH ──────────────────────────────────────────
            bool bullishBos  = currentPrice > sh1.High;    // price broke previous SH
            bool bearishBos  = currentPrice < sl1.Low;     // price broke previous SL
            bool bullishChoch = lowerHigh && higherLow;    // bearish→bullish CHoCH
            bool bearishChoch = higherHigh && lowerLow;    // bullish→bearish CHoCH

            string structure;
            string detail;
            bool isBullish, isBearish, isConsolidating;

            if (higherHigh && higherLow)
            {
                structure       = bullishBos ? "⬆ Bullish BOS — HH/HL" : "⬆ Bullish Structure — HH/HL";
                detail          = $"SH: ${sh2.High:N0}  SL: ${sl2.Low:N0}  |  Trend: Bullish";
                isBullish       = true;
                isBearish       = false;
                isConsolidating = false;
            }
            else if (lowerHigh && lowerLow)
            {
                structure       = bearishBos ? "⬇ Bearish BOS — LH/LL" : "⬇ Bearish Structure — LH/LL";
                detail          = $"SH: ${sh2.High:N0}  SL: ${sl2.Low:N0}  |  Trend: Bearish";
                isBullish       = false;
                isBearish       = true;
                isConsolidating = false;
            }
            else if (bullishChoch)
            {
                structure       = "↕ Bullish CHoCH — LH/HL";
                detail          = "Bearish → Bullish shift. Watch for confirmation.";
                isBullish       = true;
                isBearish       = false;
                isConsolidating = false;
            }
            else if (bearishChoch)
            {
                structure       = "↕ Bearish CHoCH — HH/LL";
                detail          = "Bullish → Bearish shift. Watch for confirmation.";
                isBullish       = false;
                isBearish       = true;
                isConsolidating = false;
            }
            else
            {
                structure       = "⟷ Consolidation — LH/HL";
                detail          = "No clear directional structure. Range-bound.";
                isBullish       = false;
                isBearish       = false;
                isConsolidating = true;
            }

            return new SmcStructure
            {
                MostRecentSwingHigh  = sh2.High,
                MostRecentSwingLow   = sl2.Low,
                PreviousSwingHigh    = sh1.High,
                PreviousSwingLow     = sl1.Low,
                MarketStructure      = structure,
                StructureDetail      = detail,
                IsBullish            = isBullish,
                IsBearish            = isBearish,
                IsConsolidating      = isConsolidating
            };
        }
    }
}