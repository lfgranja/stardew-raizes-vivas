// This is a simple test to understand the current behavior with extensions containing hyphens and underscores
// The file will be created but not executed as part of the architectural planning process
using System;
using System.IO;
using LivingRoots.Domain;
using Moq;

namespace LivingRoots.Tests
{
    public class ExtensionValidationTest
    {
        public static void Main()
        {
            // Create an instance of the service with mocked dependencies
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            var mockReservedService = new Mock<IReservedNameHandler>();
            
            // Setup the mock to return the input unchanged for these tests
            mockUnicodeService
                .Setup(x => x.Normalize(It.IsAny<string>()))
                .Returns<string>(s => s);
            
            // Setup reserved name handler to return input unchanged
            mockReservedService
                .Setup(x => x.Handle(It.IsAny<string>()))
                .Returns<string>(s => s);
            
            var service = new FileNameSanitizationService(mockUnicodeService.Object, mockReservedService.Object);
            
            // Test cases with extensions containing hyphens and underscores
            string[] testCases = {
                "file.name_v1.txt",      // Extension with underscore in the name part
                "test-file.js",          // Extension with hyphen in the name part
                "data.config-file.txt",  // Extension with hyphen in the name part
                "image.photo_v2.txt",    // Extension with underscore in the name part
                "script.test-script.js", // Extension with hyphen in the name part but blocked extension
                "config.my_app.config",  // Extension with underscore in the name part
                "file-name.exe",         // Blocked extension with hyphen
                "file_name.php",         // Blocked extension with underscore
                "test.my-ext.txt",       // Extension with hyphen followed by valid extension
                "file.my_ext.txt",       // Extension with underscore followed by valid extension
            };
            
            Console.WriteLine("Testing current extension validation behavior:");
            foreach (string testCase in testCases)
            {
                try
                {
                    var result = service.Sanitize(testCase);
                    Console.WriteLine($"Input: {testCase} -> Output: {result}");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Input: {testCase} -> Exception: {ex.Message}");
                }
            }
        }
    }
}