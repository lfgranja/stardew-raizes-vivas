using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using Moq;
using StardewModdingAPI;
using Xunit;
using LivingRoots.Services;

namespace LivingRoots.Tests
{
    public class DebugFileNameSanitizerTest
    {
        [Fact]
        public void DebugConsecutiveDotsProcessing()
        {
            // Create a mock UnicodeNormalizer that returns the input unchanged
            var mockUnicodeNormalizer = new Mock<IUnicodeNormalizer>();
            mockUnicodeNormalizer.Setup(x => x.Normalize(It.IsAny<string>())).Returns<string>(s => s);
            
            var sanitizer = new FileNameSanitizer(mockUnicodeNormalizer.Object);
            
            // Test with a simple case
            string input = "file...name";
            Console.WriteLine($"Input: '{input}'");
            
            // Step by step, let's see what happens
            // First, let's manually call the internal methods to debug
            var sanitizerType = typeof(FileNameSanitizer);
            var sanitizeInvalidCharsMethod = sanitizerType.GetMethod("SanitizeInvalidCharacters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var processConsecutiveDotsMethod = sanitizerType.GetMethod("ProcessConsecutiveDots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            // Test character sanitization
            string sanitized = (string)sanitizeInvalidCharsMethod.Invoke(null, new object[] { input });
            Console.WriteLine($"After character sanitization: '{sanitized}'");
            
            // Test consecutive dots processing
            string processed = (string)processConsecutiveDotsMethod.Invoke(null, new object[] { sanitized });
            Console.WriteLine($"After consecutive dots processing: '{processed}'");
            
            // Now the full method
            string result = sanitizer.Sanitize(input);
            Console.WriteLine($"Full result: '{result}'");
            
            // Expected result should be "file.name"
            Assert.Equal("file.name", result);
        }
    }
}