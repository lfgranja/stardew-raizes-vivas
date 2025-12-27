using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Tests for verifying feature status in README.md
    /// </summary>
    public class FeatureStatusTests
    {
        [Fact]
        public void Readme_FeaturesMarkedAsPlanned_ContainsPlannedPrefix()
        {
            // Arrange
            var readmePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "README.md");
            var expectedPlannedFeatures = new[]
            {
                "Visual indicators show soil health status (Planned)",
                "Health degrades over time when soil is left bare (Planned)",
                "Health improves with compost application (Planned)"
            };
            
            // Act
            var readmeContent = File.ReadAllText(readmePath);
            
            // Assert - This should fail initially since the features are not marked as planned yet
            foreach (var feature in expectedPlannedFeatures)
            {
                Assert.Contains(feature, readmeContent);
            }
        }
    }
}
