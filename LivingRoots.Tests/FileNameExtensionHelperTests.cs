using System;
using System.IO;
using LivingRoots.Domain;
using Moq;
using Xunit;

namespace LivingRoots.Tests
{
    public class FileNameExtensionHelperTests
    {
        [Fact]
        public void GetFileExtension_ValidExtension_ReturnsExtension()
        {
            // Arrange
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            var mockReservedService = new Mock<IReservedNameHandler>();
            var sanitizationService = new FileNameSanitizationService(mockUnicodeService.Object, mockReservedService.Object);

            // Act & Assert - Test using reflection to access private method
            Assert.Equal(".txt", GetFileExtensionTest(sanitizationService, "file.txt"));
            Assert.Equal(".jpg", GetFileExtensionTest(sanitizationService, "image.jpg"));
            Assert.Equal(".gz", GetFileExtensionTest(sanitizationService, "archive.tar.gz"));
        }

        [Fact]
        public void GetFileExtension_NoExtension_ReturnsEmpty()
        {
            // Arrange
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            var mockReservedService = new Mock<IReservedNameHandler>();
            var sanitizationService = new FileNameSanitizationService(mockUnicodeService.Object, mockReservedService.Object);

            // Act & Assert
            Assert.Equal("", GetFileExtensionTest(sanitizationService, "file"));
            Assert.Equal("", GetFileExtensionTest(sanitizationService, "file."));
            Assert.Equal("", GetFileExtensionTest(sanitizationService, ".hidden"));
        }

        [Fact]
        public void GetFileExtension_PathWithExtension_ReturnsExtension()
        {
            // Arrange
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            var mockReservedService = new Mock<IReservedNameHandler>();
            var sanitizationService = new FileNameSanitizationService(mockUnicodeService.Object, mockReservedService.Object);

            // Act & Assert
            Assert.Equal(".txt", GetFileExtensionTest(sanitizationService, "path/file.txt"));
            Assert.Equal(".txt", GetFileExtensionTest(sanitizationService, "path/to/file.txt"));
        }

        [Fact]
        public void GetFileExtension_InvalidExtension_ReturnsEmpty()
        {
            // Arrange
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            var mockReservedService = new Mock<IReservedNameHandler>();
            var sanitizationService = new FileNameSanitizationService(mockUnicodeService.Object, mockReservedService.Object);

            // Act & Assert
            Assert.Equal("", GetFileExtensionTest(sanitizationService, "file.txt/extra"));
            Assert.Equal("", GetFileExtensionTest(sanitizationService, "file.txt\\extra"));
        }

        [Fact]
        public void RemoveFileExtension_ValidExtension_ReturnsNameWithoutExtension()
        {
            // Arrange
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            var mockReservedService = new Mock<IReservedNameHandler>();
            var sanitizationService = new FileNameSanitizationService(mockUnicodeService.Object, mockReservedService.Object);

            // Act & Assert
            Assert.Equal("file", RemoveFileExtensionTest(sanitizationService, "file.txt"));
            Assert.Equal("image", RemoveFileExtensionTest(sanitizationService, "image.jpg"));
            Assert.Equal("archive.tar", RemoveFileExtensionTest(sanitizationService, "archive.tar.gz"));
        }

        [Fact]
        public void RemoveFileExtension_NoExtension_ReturnsOriginal()
        {
            // Arrange
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            var mockReservedService = new Mock<IReservedNameHandler>();
            var sanitizationService = new FileNameSanitizationService(mockUnicodeService.Object, mockReservedService.Object);

            // Act & Assert
            Assert.Equal("file", RemoveFileExtensionTest(sanitizationService, "file"));
            Assert.Equal("file.", RemoveFileExtensionTest(sanitizationService, "file."));
            Assert.Equal(".hidden", RemoveFileExtensionTest(sanitizationService, ".hidden"));
        }

        [Fact]
        public void RemoveFileExtension_PathWithExtension_ReturnsPathWithoutExtension()
        {
            // Arrange
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            var mockReservedService = new Mock<IReservedNameHandler>();
            var sanitizationService = new FileNameSanitizationService(mockUnicodeService.Object, mockReservedService.Object);

            // Act & Assert
            Assert.Equal("path/file", RemoveFileExtensionTest(sanitizationService, "path/file.txt"));
            Assert.Equal("path/to/file", RemoveFileExtensionTest(sanitizationService, "path/to/file.txt"));
        }

        [Fact]
        public void RemoveFileExtension_InvalidExtension_ReturnsOriginal()
        {
            // Arrange
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            var mockReservedService = new Mock<IReservedNameHandler>();
            var sanitizationService = new FileNameSanitizationService(mockUnicodeService.Object, mockReservedService.Object);

            // Act & Assert
            Assert.Equal("file.txt/extra", RemoveFileExtensionTest(sanitizationService, "file.txt/extra"));
            Assert.Equal("file.txt\\extra", RemoveFileExtensionTest(sanitizationService, "file.txt\\extra"));
        }

        [Fact]
        public void FindExtensionStartIndex_LogicIsConsistent()
        {
            // This test verifies that the logic to find extension start index is consistent
            // between GetFileExtension and RemoveFileExtension methods after refactoring
            var mockUnicodeService = new Mock<IUnicodeNormalizationService>();
            var mockReservedService = new Mock<IReservedNameHandler>();
            var sanitizationService = new FileNameSanitizationService(mockUnicodeService.Object, mockReservedService.Object);

            // Test cases where extension should be found
            string filename = "test.txt";
            string extension = GetFileExtensionTest(sanitizationService, filename);
            string nameWithoutExtension = RemoveFileExtensionTest(sanitizationService, filename);
            
            // If extension exists, combining them back should recreate the original filename
            if (!string.IsNullOrEmpty(extension))
            {
                Assert.Equal(filename, nameWithoutExtension + extension);
            }
        }

        // Reflection helper methods to access private methods for testing
        private string GetFileExtensionTest(FileNameSanitizationService service, string filename)
        {
            var method = typeof(FileNameSanitizationService).GetMethod("GetFileExtension", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            object? invokeResult = method!.Invoke(null, new object[] { filename });
            return (string)invokeResult!;
        }

        private string RemoveFileExtensionTest(FileNameSanitizationService service, string filename)
        {
            var method = typeof(FileNameSanitizationService).GetMethod("RemoveFileExtension", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            object? invokeResult = method!.Invoke(null, new object[] { filename });
            return (string)invokeResult!;
        }
    }
}