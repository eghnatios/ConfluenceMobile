namespace ConfluenceAIBot.Models
{
    /// <summary>
    /// Eight harmonic Gann price levels surrounding the live price.
    /// Calculated using W.D. Gann's Square-of-9 octave divisions:
    /// Whole → Half → Quarter → Eighth on each side.
    /// </summary>
    public class GannLevels
    {
        public decimal GannBase     { get; set; }   // Nearest whole magnitude pivot
        // ── Resistance levels ────────────────────────────────────────────────
        public decimal EighthAbove  { get; set; }   // +1/8  (nearest resistance)
        public decimal QuarterAbove { get; set; }   // +2/8  (quarter level)
        public decimal HalfAbove    { get; set; }   // +4/8  (half level)
        public decimal WholeAbove   { get; set; }   // +8/8  (full magnitude above)
        // ── Support levels ───────────────────────────────────────────────────
        public decimal EighthBelow  { get; set; }   // -1/8  (nearest support)
        public decimal QuarterBelow { get; set; }   // -2/8  (quarter level)
        public decimal HalfBelow    { get; set; }   // -4/8  (half level)
        public decimal WholeBelow   { get; set; }   // -8/8  (full magnitude below)
    }
}
