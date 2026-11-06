using System;

namespace LivingRoots.Verification.Evidence
{
    /// <summary>
    /// Maps a stored soil-health value and its capture state onto the
    /// five-value tier contract used by the scenario predicates.
    /// Boundaries are the spec FR-013 / A7 ones (31 / 71); measured 0 is
    /// Poor, only NaN/missing is Unknown.
    /// </summary>
    public static class HealthTierClassifier
    {
        public const string Unknown = "unknown";
        public const string NonParticipant = "nonParticipant";
        public const string Poor = "poor";
        public const string Moderate = "moderate";
        public const string Healthy = "healthy";

        public const double PoorModerateEdge = 31d;
        public const double ModerateHealthyEdge = 71d;

        public static string Classify(double? health, string state)
        {
            if (state == "missing" || state == "nan")
            {
                return Unknown;
            }

            if (!health.HasValue)
            {
                return Unknown;
            }

            double value = health.Value;
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return Unknown;
            }

            if (value < PoorModerateEdge)
            {
                return Poor;
            }

            if (value < ModerateHealthyEdge)
            {
                return Moderate;
            }

            return Healthy;
        }
    }
}
