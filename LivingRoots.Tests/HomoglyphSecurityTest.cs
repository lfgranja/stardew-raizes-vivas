
using System;
using LivingRoots.Domain;
using Xunit;

namespace LivingRoots.Tests
{
    public class HomoglyphSecurityTest
    {
        [Fact]
        public void HomoglyphSecurity_WithCyrillicX_ConvertsToCorrectLatinX()
        {
            // Arrange
            var normalizer = new UnicodeNormalizationService();
            string cyrillicX = "х"; // Cyrillic 'х' which looks like Latin 'x'
            string cyrillicXUpper = "Х"; // Cyrillic 'Х' which looks like Latin 'X'

            // Act
            string resultLower = normalizer.Normalize(cyrillicX)!;
            string resultUpper = normalizer.Normalize(cyrillicXUpper)!;

            // Assert - Verify that Cyrillic 'х' now correctly maps to Latin 'x', not 'h'
            Assert.Equal("x", resultLower);
            Assert.Equal("X", resultUpper);
            
            Console.WriteLine($"Cyrillic 'х' correctly normalizes to: '{resultLower}'");
            Console.WriteLine($"Cyrillic 'Х' correctly normalizes to: '{resultUpper}'");
            
            // This demonstrates the security improvement:
            // Before: Cyrillic 'х' incorrectly mapped to 'h' - wrong visual equivalent
            // After: Cyrillic 'х' correctly maps to 'x' - correct visual equivalent
        }
    }
}
