using System;
using Xunit;
using LivingRoots.Domain;
using Moq;

namespace LivingRoots.Tests
{
    public class ExtensionDetectionTest
    {
        [Fact]
        public void TestExtensionDetectionWithUnicodeThroughPublicApi()
        {
            // Create an instance of the service with mocked dependencies
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            var mockReservedService = new Mock<IReservedNameHandler>();
            
            // Setup the mock to return the input with the combining character normalized
            mockUnicodeService
                .Setup(x => x.Normalize("test\u0301.txt"))
                .Returns("test\u0301.txt"); // Using the original form for this test
            
            // Setup reserved name handler to return input unchanged
            mockReservedService
                .Setup(x => x.Handle(It.IsAny<string>()))
                .Returns<string>(s => s);
            
            var service = new FileNameSanitizationService(mockUnicodeService.Object, mockReservedService.Object);

            // Test filename with potential Unicode normalization issues through public API
            string filename = "test\u0301.txt"; // é with combining acute accent
            
            // Call the public Sanitize method
            var result = service.Sanitize(filename);
            
            // Should preserve the original filename structure since it's safe
            Assert.NotNull(result);
            Assert.EndsWith(".txt", result); // The .txt extension should be preserved
        }
        
        [Fact]
        public void TestFindExtensionStartIndexScenariosThroughPublicApi()
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
            
            // Test various cases through the public API
            var testCases = new[]
            {
                ("test.txt", "test.txt"),      // Normal case - should preserve extension
                ("file.exe", "file.blocked"),  // Blocked extension - should be replaced
                ("noextension", "noextension"),  // No extension - should remain unchanged
                ("multiple.dots.txt", "multiple.dots.txt"), // Multiple dots - should preserve last extension
                (".hidden", ".hidden"),        // Hidden file without real extension - should remain unchanged
                (".js", ".file.blocked"),      // Hidden file with blocked extension - should be replaced
                ("test.", "test"),             // Ends with dot - should remove trailing dot
                ("test..", "test"),            // Ends with multiple dots - should remove trailing dots
            };
            
            foreach (var (input, expected) in testCases)
            {
                var result = service.Sanitize(input);
                Assert.Equal(expected, result);
            }
        }
        
        [Fact]
        public void TestExtensionDetectionWithHyphensAndUnderscoresThroughPublicApi()
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
            
            // Test various extensions with hyphens and underscores through public API
            var testCases = new[]
            {
                ("test.my-ext.txt", "test.my-ext.txt"),      // Extension with hyphen followed by valid extension
                ("file.my_ext.txt", "file.my_ext.txt"),      // Extension with underscore followed by valid extension
                ("data.config-file.txt", "data.config-file.txt"), // Extension with hyphen followed by valid extension
                ("image.photo_v2.txt", "image.photo_v2.txt"),   // Extension with underscore followed by valid extension
                ("doc.file-name.txt", "doc.file-name.txt"),     // Multiple dots, extension with hyphen
                ("archive.backup_v1.7z", "archive.backup_v1.7z"), // Multiple dots, extension with underscore
                ("script.test-script.js", "script.test-script.blocked"), // Extension with hyphen but blocked extension
                ("config.my_app.config", "config.my_app.config"),  // Extension with underscore
            };
            
            foreach (var (input, expected) in testCases)
            {
                var result = service.Sanitize(input);
                Assert.Equal(expected, result);
            }
        }
        
        [Fact]
        public void TestBlockedExtensionsAreHandledThroughPublicApi()
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
            
            // Test various blocked extensions through the public API
            var blockedExtensions = new[]
            {
                "test.exe", "file.dll", "script.js", "program.bat", 
                "app.sh", "config.php", "data.asp", "script.vbs"
            };
            
            foreach (var input in blockedExtensions)
            {
                var result = service.Sanitize(input);
                Assert.EndsWith(".blocked", result); // All blocked extensions should be replaced with .blocked
            }
        }
        
        [Fact]
        public void TestValidExtensionsArePreservedThroughPublicApi()
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
            
            // Test various valid extensions through the public API
            var validExtensions = new[]
            {
                "test.txt", "file.pdf", "image.jpg", "document.docx", 
                "archive.zip", "data.json", "config.xml", "script.cs"
            };
            
            foreach (var input in validExtensions)
            {
                var result = service.Sanitize(input);
                Assert.Equal(input, result); // Valid extensions should be preserved unchanged
            }
        }
        
        [Fact]
        public void TestExtensionDetectionEdgeCases()
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
            
            // Test edge cases that specifically target extension detection
            var testCases = new[]
            {
                ("file.", "file"),             // Ends with dot - no extension, trailing dot removed
                ("file..", "file"),            // Ends with multiple dots - no extension, trailing dots removed
                ("file...", "file"),           // Multiple trailing dots - all removed
                ("file..txt", "file.txt"),     // Multiple dots before extension
                (".file.txt", ".file.txt"),    // Hidden file with extension
                (".file", ".file"),            // Hidden file without extension
                ("file.txt.bak", "file.txt.bak"), // Extension with dot in name
                ("file.txt.backup", "file.txt.backup"), // Multiple extensions
                ("file.txt.", "file.txt"),     // Ends with dot after extension
                ("file.txt..", "file.txt"),    // Ends with multiple dots after extension
            };
            
            foreach (var (input, expected) in testCases)
            {
                var result = service.Sanitize(input);
                Assert.Equal(expected, result);
            }
        }
        
        [Fact]
        public void TestExtensionWithSpecialCharacters()
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
            
            // Test extensions with special characters that should be handled properly
            var testCases = new[]
            {
                ("file-name.txt", "file-name.txt"),    // Hyphen in filename
                ("file_name.txt", "file_name.txt"),    // Underscore in filename
                ("file name.txt", "file_name.txt"),    // Space in filename (should be replaced)
                ("file.name.txt", "file.name.txt"),    // Multiple dots in filename
                ("file-name.exe", "file-name.blocked"), // Blocked extension with hyphen
                ("file_name.php", "file_name.blocked"), // Blocked extension with underscore
            };
            
            foreach (var (input, expected) in testCases)
            {
                var result = service.Sanitize(input);
                Assert.Equal(expected, result);
            }
        }
        
        [Fact]
        public void TestHiddenFilesWithBlockedExtensions()
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
            
            // Test cases for hidden files with blocked extensions
            var testCases = new[]
            {
                (".js", ".file.blocked"),       // Hidden file with blocked extension
                (".php", ".file.blocked"),      // Another hidden file with blocked extension
                (".exe", ".file.blocked"),      // Hidden file with blocked extension
                (".vbs", ".file.blocked"),      // Another hidden file with blocked extension
            };
            
            foreach (var (input, expected) in testCases)
            {
                var result = service.Sanitize(input);
                Assert.Equal(expected, result);
            }
        }
    }
}
