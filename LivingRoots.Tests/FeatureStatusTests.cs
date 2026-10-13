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
        /// <summary>
        /// T198 (FR-016/SC-006): the README's `## Features` section must label
        /// only the capabilities the player CANNOT observe with `(Planned)` —
        /// and, symmetrically, must not label observable capabilities that way.
        /// The soil-health capabilities below became observable when Feature 004
        /// shipped the loop, so they are asserted present WITHOUT the label.
        /// </summary>
        [Fact]
        public void Readme_ObservableFeatures_AreNotLabelledPlanned()
        {
            // Arrange
            var expectedObservableFeatures = new[]
            {
                "Visual indicators show soil health status",
                "Health degrades over time when soil is left bare",
                "Health improves with compost application",
                "Composting bin: craft, place, load with organic waste, harvest compost after 2 in-game days"
            };

            // Arrange — capabilities with no in-game behaviour must carry the label.
            var expectedUnbuiltFeatures = new[]
            {
                "Learn about real-world soil management (Planned)",
                "Enhance your farming experience with educational elements (Planned)"
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

            // Assert — observable capabilities claim no label...
            foreach (var feature in expectedObservableFeatures)
            {
                Assert.Contains(feature, readmeContent);
                Assert.DoesNotContain(feature + " (Planned)", readmeContent);
            }

            // ...and unbuilt ones carry it (FR-016 zero false claims).
            foreach (var feature in expectedUnbuiltFeatures)
            {
                Assert.Contains(feature, readmeContent);
            }

            // The retired-multiplier claim must not survive (FR-005a supersedes it).
            Assert.DoesNotContain("Different materials provide different benefits", readmeContent);
        }
    }
}
