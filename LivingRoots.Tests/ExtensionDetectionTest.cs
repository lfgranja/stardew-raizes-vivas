using System;
using System.Reflection;
using Xunit;
using LivingRoots.Domain;

namespace LivingRoots.Tests
{
    public class ExtensionDetectionTest
    {
        [Fact]
        public void TestExtensionDetectionWithUnicode()
        {
            // Create an instance of the service
            var mockUnicodeService = new MockUnicodeNormalizationService();
            var mockReservedService = new MockReservedNameHandler();
            var service = new FileNameSanitizationService(mockUnicodeService, mockReservedService);

            // Test filename with potential Unicode normalization issues
            string filename = "test\u0301.txt"; // é with combining acute accent
            
            // Get the extension using reflection to access the private method
            var method = typeof(FileNameSanitizationService)
                .GetMethod("GetFileExtension", BindingFlags.NonPublic | BindingFlags.Static);
            
            Assert.NotNull(method);
            var extension = method.Invoke(null, new object[] { filename }) as string;
            
            // Should correctly detect .txt extension
            Assert.Equal(".txt", extension);
        }
        
        [Fact]
        public void TestFindExtensionStartIndexDirectly()
        {
            // Get the method using reflection to test it directly
            var method = typeof(FileNameSanitizationService)
                .GetMethod("FindExtensionStartIndex", BindingFlags.NonPublic | BindingFlags.Static);
            
            Assert.NotNull(method);
            
            // Test various cases
            var testCases = new[]
            {
                ("test.txt", 4),      // Normal case - last dot at index 4
                ("file.exe", 4),      // Blocked extension - dot at index 4
                ("noextension", -1),  // No extension
                ("multiple.dots.txt", 13), // Multiple dots - last dot at index 13
                (".hidden", -1),      // Hidden file without real extension
                (".js", 0),           // Hidden file with blocked extension
                ("test.", -1),        // Ends with dot
                ("test..", -1),       // Ends with multiple dots
            };
            
            foreach (var (input, expectedIndex) in testCases)
            {
                var result = (int)method.Invoke(null, new object[] { input });
                Assert.Equal(expectedIndex, result);
            }
        }
    }
    
    // Mock implementations for testing
    internal class MockUnicodeNormalizationService : IUnicodeNormalizationService
    {
        public string? Normalize(string input)
        {
            return input; // Return input unchanged for testing
        }
    }
    
    internal class MockReservedNameHandler : IReservedNameHandler
    {
        public string? Handle(string input)
        {
            return input; // Return input unchanged for testing
        }
    }
}