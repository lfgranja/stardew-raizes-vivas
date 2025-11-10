using System;
using LivingRoots.Services;
using LivingRoots.Domain;
using Moq;
using Xunit;
using StardewModdingAPI;

namespace LivingRoots.Tests
{
    public class ModDataValidationHelperTests
    {
        [Fact]
        public void GetValidatedAndSanitizedKey_WithValidKey_ReturnsSanitizedKey()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockModLogic = new Mock<IModLogic>();
            
            // Setup the mod logic to return the input for sanitization (for this test)
            mockModLogic.Setup(x => x.SanitizeFileName(It.IsAny<string>())).Returns<string>(s => s);
            mockModLogic.Setup(x => x.ValidatePath(It.IsAny<string>())).Verifiable(); // Verify it's called
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object, mockModLogic.Object);

            // Act
            string result = GetValidatedAndSanitizedKeyTest(service, "valid_key");

            // Assert
            Assert.Equal("valid_key", result);
            mockModLogic.Verify(x => x.ValidatePath("valid_key"), Times.Once);
        }

        [Fact]
        public void GetValidatedAndSanitizedKey_WithNullKey_ThrowsArgumentException()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockModLogic = new Mock<IModLogic>();
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object, mockModLogic.Object);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => GetValidatedAndSanitizedKeyTest(service, (string)null!));
            Assert.Contains("Key cannot be null or empty", exception.Message);
        }

        [Fact]
        public void GetValidatedAndSanitizedKey_WithEmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockModLogic = new Mock<IModLogic>();
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object, mockModLogic.Object);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => GetValidatedAndSanitizedKeyTest(service, ""));
            Assert.Contains("Key cannot be null or empty", exception.Message);
        }

        [Fact]
        public void GetValidatedAndSanitizedKey_WithWhitespaceKey_ThrowsArgumentException()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockModLogic = new Mock<IModLogic>();
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object, mockModLogic.Object);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => GetValidatedAndSanitizedKeyTest(service, "   "));
            Assert.Contains("Key cannot be null or empty", exception.Message);
        }

        [Fact]
        public void GetValidatedAndSanitizedKey_WithInvalidPath_ThrowsException()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockModLogic = new Mock<IModLogic>();
            
            // Setup the mod logic to throw an exception when validating path
            mockModLogic.Setup(x => x.ValidatePath(It.IsAny<string>())).Throws(new InvalidOperationException("Invalid path"));
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object, mockModLogic.Object);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => GetValidatedAndSanitizedKeyTest(service, "invalid_path"));
            Assert.Contains("Invalid path", exception.Message);
        }

        // Helper method to access the private GetValidatedAndSanitizedKey method via reflection
        private string GetValidatedAndSanitizedKeyTest(ModDataService service, string key)
        {
            var method = typeof(ModDataService).GetMethod("GetValidatedAndSanitizedKey", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            try
            {
                object? invokeResult = method!.Invoke(service, new object[] { key });
            return (string)invokeResult!;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                // Unwrap the actual exception that was thrown by the method
                var innerException = ex.InnerException;
            if (innerException != null)
            {
                throw innerException;
            }
            else
            {
                throw new InvalidOperationException("Inner exception is null", ex);
            }
            }
        }
    }
}