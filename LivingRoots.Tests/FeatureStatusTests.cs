using System.IO;
using System.Linq;
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
            var expectedPlannedFeatures = new[]
            {
                "Health degrades over time when soil is left bare (Planned)",
                "Health improves with compost application (Planned)"
            };

            var expectedImplementedFeatures = new[]
            {
                "Visual indicators show soil health status",
                "hover tooltips",
                "color-coded tile overlays"
            };

            // Read README.md from embedded resource for reliable test access across all environments
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            var readmeResourceName = resourceNames.FirstOrDefault(name => name.EndsWith("README.md"));

            Assert.True(readmeResourceName != null, $"README.md resource not found in assembly. Available resources: {string.Join(", ", resourceNames)}");

            // Act
            using var stream = assembly.GetManifestResourceStream(readmeResourceName);
            Assert.True(stream != null, $"Could not load README.md resource: {readmeResourceName}");

            using var reader = new StreamReader(stream);
            var readmeContent = reader.ReadToEnd();

            // Assert - Verify planned features
            foreach (var feature in expectedPlannedFeatures)
            {
                Assert.Contains(feature, readmeContent);
            }

            // Assert - Verify implemented features
            foreach (var feature in expectedImplementedFeatures)
            {
                Assert.Contains(feature, readmeContent);
            }
        }
    }
}
