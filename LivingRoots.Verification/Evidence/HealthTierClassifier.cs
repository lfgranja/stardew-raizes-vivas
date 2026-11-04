using System;

namespace LivingRoots.Verification.Evidence
{
    /// <summary>
    /// Maps a stored soil-health value and its capture state onto the
    /// five-value tier contract used by the scenario predicates.
    /// The boundaries are the spec FR-010 ones (34 / 67), not the
    /// visualization band edges.
    /// </summary>
    public static class HealthTierClassifier
    {
        public const string Unknown = "unknown";
        public const string NonParticipant = "nonParticipant";
        public const string Poor = "poor";
        public const string Moderate = "moderate";
        public const string Healthy = "healthy";

        public const double PoorModerateEdge = 34d;
        public const double ModerateHealthyEdge = 67d;

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

            if (value <= 0d)
            {
                return NonParticipant;
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
