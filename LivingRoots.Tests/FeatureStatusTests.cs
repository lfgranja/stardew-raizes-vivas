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
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            FileInfo? readmeFile = null;
            
            for (int i = 0; i < 10 && dir != null; i++)
            {
                var files = dir.GetFiles("README.md");
                if (files.Length > 0)
                {
                    readmeFile = files[0];
                    break;
                }
                dir = dir.Parent;
            }
            
            Assert.True(readmeFile != null && readmeFile.Exists, $"README.md could not be found. Started search from {AppContext.BaseDirectory}");
            var readmePath = readmeFile.FullName;



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
