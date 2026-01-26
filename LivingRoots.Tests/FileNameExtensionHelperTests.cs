using LivingRoots.Domain;

namespace LivingRoots.Tests
{
    public class FileNameExtensionHelperTests
    {
        [Fact]
        public void GetFileExtension_ValidExtension_ReturnsExtension()
        {
            // Arrange

            // Act & Assert - Test using reflection to access private method
            Assert.Equal(".txt", GetFileExtensionTest("file.txt"));
            Assert.Equal(".jpg", GetFileExtensionTest("image.jpg"));
            Assert.Equal(".gz", GetFileExtensionTest("archive.tar.gz"));
        }

        [Fact]
        public void GetFileExtension_NoExtension_ReturnsEmpty()
        {
            // Arrange

            // Act & Assert
            Assert.Equal("", GetFileExtensionTest("file"));
            Assert.Equal("", GetFileExtensionTest("file."));
            Assert.Equal("", GetFileExtensionTest(".hidden"));
        }

        [Fact]
        public void GetFileExtension_PathWithExtension_ReturnsExtension()
        {
            // Arrange

            // Act & Assert
            Assert.Equal(".txt", GetFileExtensionTest("path/file.txt"));
            Assert.Equal(".txt", GetFileExtensionTest("path/to/file.txt"));
        }

        [Fact]
        public void GetFileExtension_InvalidExtension_ReturnsEmpty()
        {
            // Arrange

            // Act & Assert
            Assert.Equal("", GetFileExtensionTest("file.txt/extra"));
            Assert.Equal("", GetFileExtensionTest("file.txt\\extra"));
        }

        [Fact]
        public void RemoveFileExtension_ValidExtension_ReturnsNameWithoutExtension()
        {
            // Arrange

            // Act & Assert
            Assert.Equal("file", RemoveFileExtensionTest("file.txt"));
            Assert.Equal("image", RemoveFileExtensionTest("image.jpg"));
            Assert.Equal("archive.tar", RemoveFileExtensionTest("archive.tar.gz"));
        }

        [Fact]
        public void RemoveFileExtension_NoExtension_ReturnsOriginal()
        {
            // Arrange

            // Act & Assert
            Assert.Equal("file", RemoveFileExtensionTest("file"));
            Assert.Equal("file.", RemoveFileExtensionTest("file."));
            Assert.Equal(".hidden", RemoveFileExtensionTest(".hidden"));
        }

        [Fact]
        public void RemoveFileExtension_PathWithExtension_ReturnsPathWithoutExtension()
        {
            // Arrange

            // Act & Assert
            Assert.Equal("path/file", RemoveFileExtensionTest("path/file.txt"));
            Assert.Equal("path/to/file", RemoveFileExtensionTest("path/to/file.txt"));
        }

        [Fact]
        public void RemoveFileExtension_InvalidExtension_ReturnsOriginal()
        {
            // Arrange

            // Act & Assert
            Assert.Equal("file.txt/extra", RemoveFileExtensionTest("file.txt/extra"));
            Assert.Equal("file.txt\\extra", RemoveFileExtensionTest("file.txt\\extra"));
        }

        [Fact]
        public void FindExtensionStartIndex_LogicIsConsistent()
        {
            // This test verifies that the logic to find extension start index is consistent
            // between GetFileExtension and RemoveFileExtension methods after refactoring

            // Test cases where extension should be found
            var filename = "test.txt";
            var extension = GetFileExtensionTest(filename);
            var nameWithoutExtension = RemoveFileExtensionTest(filename);

            // If extension exists, combining them back should recreate the original filename
            if (!string.IsNullOrEmpty(extension))
            {
                Assert.Equal(filename, nameWithoutExtension + extension);
            }
        }

        // Reflection helper methods to access private methods for testing
        private static string GetFileExtensionTest(string filename)
        {
            var method = typeof(FileNameSanitizationService).GetMethod(
                "GetFileExtension",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );
            var invokeResult = method!.Invoke(null, new object[] { filename });
            return (string)invokeResult!;
        }

        private static string RemoveFileExtensionTest(string filename)
        {
            var method = typeof(FileNameSanitizationService).GetMethod(
                "RemoveFileExtension",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );
            var invokeResult = method!.Invoke(null, new object[] { filename });
            return (string)invokeResult!;
        }
    }
}
