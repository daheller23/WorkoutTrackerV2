namespace WorkoutTrackerV2.Helpers
{
    /// <summary>
    /// Estimates one-rep max from a submaximal set.
    /// Both formulas are unreliable above ~10 reps — results are clamped
    /// to a minimum of 1 rep and flagged as estimates, not facts.
    /// </summary>
    public static class OneRepMaxCalculator
    {
        public const string FormulaEpley = "Epley";
        public const string FormulaBrzycki = "Brzycki";

        /// <summary>
        /// Epley: weight × (1 + reps / 30)
        /// Most widely used. Tends to overestimate at high rep counts.
        /// </summary>
        public static double Epley(double weight, int reps)
        {
            if (reps <= 0 || weight <= 0) return 0;
            if (reps == 1) return weight;
            return weight * (1 + reps / 30.0);
        }

        /// <summary>
        /// Brzycki: weight × 36 / (37 − reps)
        /// Popular in powerlifting. Returns 0 for reps >= 37 (formula breaks down).
        /// </summary>
        public static double Brzycki(double weight, int reps)
        {
            if (reps <= 0 || weight <= 0) return 0;
            if (reps == 1) return weight;
            if (reps >= 37) return 0;
            return weight * 36.0 / (37.0 - reps);
        }

        /// <summary>
        /// Calculates using whichever formula name is stored in settings.
        /// Falls back to Epley for unknown formula names.
        /// </summary>
        public static double Calculate(double weight, int reps, string formula)
        {
            return formula == FormulaBrzycki
                ? Brzycki(weight, reps)
                : Epley(weight, reps);
        }

        /// <summary>
        /// Returns a formatted display string e.g. "Est. 1RM: 225 lbs"
        /// Returns empty string when the result is zero or invalid.
        /// </summary>
        public static string FormatResult(double weight, int reps, string formula, string unit)
        {
            double result = Calculate(weight, reps, formula);
            return result > 0
                ? $"Est. 1RM: {result:F0} {unit}"
                : string.Empty;
        }
    }
}
