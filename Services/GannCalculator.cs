using ConfluenceAIBot.Models;

namespace ConfluenceAIBot.Services
{
    /// <summary>
    /// Computes W.D. Gann harmonic price levels.
    ///
    /// Algorithm:
    ///   1. Detect order-of-magnitude (e.g. BTC @95,000 → magnitude = 1,000).
    ///   2. Unit step  = magnitude / 8  (e.g. 125).
    ///   3. Floor / ceiling of current price to the nearest step boundary.
    ///   4. Project the 4 named harmonic levels on each side.
    /// </summary>
    public static class GannCalculator
    {
        public static GannLevels Calculate(decimal price)
        {
            if (price <= 0)
                throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive.");

            decimal magnitude = GetMagnitude(price);
            decimal step      = magnitude / 8m;

            // Strict floor / ceiling (handle exact boundary by nudging)
            decimal eighthBelow = Math.Floor(price / step) * step;
            if (eighthBelow >= price) eighthBelow -= step;

            decimal eighthAbove = eighthBelow + step;
            if (eighthAbove <= price) eighthAbove += step;   // guard exact hit

            // Nearest whole-magnitude pivot (base)
            decimal gannBase = Math.Round(price / magnitude) * magnitude;

            return new GannLevels
            {
                GannBase     = gannBase,
                // ── Above ──────────────────────────────────────────────────
                EighthAbove  = eighthAbove,               // +1/8
                QuarterAbove = eighthAbove  + step,       // +2/8
                HalfAbove    = eighthAbove  + step * 3m,  // +4/8  (from floor)
                WholeAbove   = gannBase     + magnitude,  // next full level
                // ── Below ──────────────────────────────────────────────────
                EighthBelow  = eighthBelow,               // -1/8
                QuarterBelow = eighthBelow  - step,       // -2/8
                HalfBelow    = eighthBelow  - step * 3m,  // -4/8
                WholeBelow   = gannBase     - magnitude   // previous full level
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Determines the "round number" magnitude for any asset price.
        // Examples:  95000 → 1000 | 3200 → 100 | 0.045 → 0.01
        // ─────────────────────────────────────────────────────────────────────
        private static decimal GetMagnitude(decimal price)
        {
            decimal magnitude = 1m;
            decimal temp      = price;

            if (temp >= 1m)
            {
                while (temp >= 100m) { temp /= 10m; magnitude *= 10m; }
            }
            else
            {
                while (temp < 10m) { temp *= 10m; magnitude /= 10m; }
            }

            return magnitude;
        }
    }
}
