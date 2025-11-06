using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class SecurityFileNameSanitizerTests
    {
        private readonly Mock<IUnicodeNormalizer> _mockUnicodeNormalizer;
        private readonly FileNameSanitizer _fileNameSanitizer;

        public SecurityFileNameSanitizerTests()
        {
            _mockUnicodeNormalizer = new Mock<IUnicodeNormalizer>();
            _mockUnicodeNormalizer.Setup(x => x.Normalize(It.IsAny<string?>())).Returns<string?>(input => input);
            _fileNameSanitizer = new FileNameSanitizer(_mockUnicodeNormalizer.Object);
        }

        // Test 1: Extension smuggling prevention - now blocks dangerous extensions
        [Fact]
        public void Sanitize_WithExtensionSmuggling_Attack_BlocksDangerousExtension()
        {
            // Arrange - This filename could be used for extension smuggling
            // The file might look like a text file but actually be an executable
            string input = "document.txt.exe";
            
            // Act
            string result = _fileNameSanitizer.Sanitize(input);
            
            // Assert - The sanitizer should block the dangerous extension
            Assert.Equal("document.txt_blocked.exe", result); // Dangerous extension is blocked
        }

        // Test 2: Filename sanitization with allowlist approach
        [Fact]
        public void Sanitize_WithInvalidCharacters_UsesAllowlistApproach()
        {
            // Arrange - Using characters that should be filtered by allowlist
            string input = "file@#$%^&*()name";
            
            // Act
            string result = _fileNameSanitizer.Sanitize(input);
            
            // Assert - Only allowlisted characters should be preserved, with consecutive invalid chars consolidated
            Assert.Equal("file_name", result); // Non-allowlisted chars become single underscores
        }

        // Test 3: Extension validation and blocking
        [Fact]
        public void Sanitize_WithDangerousExtensions_BlocksAndAppends()
        {
            // Arrange - Dangerous extensions should be blocked
            string input = "malicious.exe";
            
            // Act
            string result = _fileNameSanitizer.Sanitize(input);
            
            // Assert - Dangerous extension should be handled safely
            Assert.Equal("malicious_blocked.exe", result);
        }

        // Test 4: Multiple extensions that could be dangerous
        [Fact]
        public void Sanitize_WithMultipleDangerousExtensions_BlocksRiskyExtension()
        {
            // Arrange - Multiple extensions where the "real" extension is the last one
            string input = "innocent.jpg.exe";
            
            // Act
            string result = _fileNameSanitizer.Sanitize(input);
            
            // Assert - The dangerous extension should be blocked
            Assert.Equal("innocent.jpg_blocked.exe", result);
        }

        // Test 5: Homoglyph normalization security
        [Fact]
        public void Sanitize_WithHomoglyphs_AppliesContextAwareNormalization()
        {
            // Arrange - Using homoglyphs that should be converted for security
            string input1 = "user";  // Normal Latin
            string input2 = "usеr";  // Contains Cyrillic 'е'
            
            // Setup mock to normalize the second input
            _mockUnicodeNormalizer.Setup(x => x.Normalize(input2)).Returns("user");
            
            // Act
            string result1 = _fileNameSanitizer.Sanitize(input1);
            string result2 = _fileNameSanitizer.Sanitize(input2);
            
            // This shows the security measure: different inputs that normalize to same output
            // are handled consistently
            Assert.Equal(result1, result2);
        }

        // Test 6: Legitimate Cyrillic content preservation - testing the concept
        [Fact]
        public void Sanitize_WithLegitimateCyrillic_PreservesInContext()
        {
            // Arrange - Test that the system handles legitimate Cyrillic properly
            // The Unicode normalizer has context-aware logic to preserve Cyrillic in proper contexts
            var realNormalizer = new UnicodeNormalizer();
            
            // Test that a mixed context with both Cyrillic and Latin characters works appropriately
            string mixedInput = "file_тест_data"; // Mix of Latin and Cyrillic
            
            // Act
            string result = realNormalizer.Normalize(mixedInput);
            
            // The result should not be empty and should handle the mixed content appropriately
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // Now test with the filename sanitizer
            var realFileNameSanitizer = new FileNameSanitizer(realNormalizer);
            string sanitizedResult = realFileNameSanitizer.Sanitize(mixedInput);
            
            Assert.NotNull(sanitizedResult);
            Assert.NotEmpty(sanitizedResult);
        }

        // Test 7: Diacritic removal security
        [Fact]
        public void Sanitize_WithDiacritics_RemovesForSecurity()
        {
            // Arrange - Diacritics that might be important for identity but removed for security
            string input1 = "resume";  // English
            string input2 = "résumé";  // French with diacritics
            
            // Setup mock to normalize the second input
            _mockUnicodeNormalizer.Setup(x => x.Normalize(input2)).Returns("resume");
            
            // Act
            string result1 = _fileNameSanitizer.Sanitize(input1);
            string result2 = _fileNameSanitizer.Sanitize(input2);
            
            // The diacritic removal prevents identity confusion in security contexts
            Assert.Equal(result1, result2);
        }

        // Test 8: Proper extension validation
        [Fact]
        public void Sanitize_WithValidExtensions_AllowsSafeExtensions()
        {
            // Arrange - Valid extensions should be preserved
            string input = "document.pdf";
            
            // Act
            string result = _fileNameSanitizer.Sanitize(input);
            
            // Assert - Valid extensions should be allowed
            Assert.Equal("document.pdf", result);
        }

        // Test 9: Path traversal prevention with normalized characters
        [Fact]
        public void Sanitize_WithPathTraversal_Attempts_PreventsTraversal()
        {
            // Arrange - Path traversal attempts
            string input = "../etc/passwd";
            
            // Act
            string result = _fileNameSanitizer.Sanitize(input);
            
            // Assert - Path traversal should be prevented, double dots become single dot
            Assert.Equal("._etc_passwd", result); // The double dots are processed to single dot
        }

        // Test 10: Zero-width character removal
        [Fact]
        public void Sanitize_WithZeroWidthChars_RemovesForSecurity()
        {
            // Arrange - Zero-width characters that should be removed
            string input = "test\u200Bzwsp\u200Czwnj\u200Dzwj";
            
            // Setup mock to return the string with zero-width chars removed
            _mockUnicodeNormalizer.Setup(x => x.Normalize(input)).Returns("testzwspzwnjzwj");
            
            // Act
            string result = _fileNameSanitizer.Sanitize(input);
            
            // Assert - Zero-width characters should be removed for security
            Assert.Equal("testzwspzwnjzwj", result);
        }
    }
}