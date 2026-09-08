using System.Reflection;
using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Tests for <see cref="SaveIdProvider"/> � verifies save ID resolution per FR-6.1 through FR-6.3.
    /// </summary>
    public class SaveIdProviderTests
    {
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly SaveIdProvider _sut;

        public SaveIdProviderTests()
        {
            _mockMonitor = new Mock<IMonitor>();
            _sut = new SaveIdProvider(_mockMonitor.Object);
        }

        [Fact]
        public void GetSaveId_WithValidSaveFolder_ReturnsId()
        {
            // Arrange
            var field = typeof(Constants).GetField("SaveFolderName", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            field.SetValue(null, "test_save_id");

            // Act
            var result = _sut.GetSaveId();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test_save_id", result);
        }

        [Fact]
        public void GetSaveId_WithNullSaveFolder_ReturnsNull()
        {
            // Arrange
            var field = typeof(Constants).GetField("SaveFolderName", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            field.SetValue(null, null);

            // Act
            var result = _sut.GetSaveId();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetSaveId_WithEmptySaveFolder_ReturnsNull()
        {
            // Arrange
            var field = typeof(Constants).GetField("SaveFolderName", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            field.SetValue(null, string.Empty);

            // Act
            var result = _sut.GetSaveId();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetSaveId_WithWhitespaceSaveFolder_ReturnsNull()
        {
            // Arrange
            var field = typeof(Constants).GetField("SaveFolderName", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            field.SetValue(null, "   ");

            // Act
            var result = _sut.GetSaveId();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetSaveId_WithTooLongSaveFolder_ReturnsNull()
        {
            // Arrange
            var longSaveId = new string('a', 201);
            var field = typeof(Constants).GetField("SaveFolderName", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            field.SetValue(null, longSaveId);

            // Act
            var result = _sut.GetSaveId();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetSaveId_MultipleCalls_ReturnsSameId()
        {
            // Arrange
            var field = typeof(Constants).GetField("SaveFolderName", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            field.SetValue(null, "consistent_save_id");

            // Act
            var result1 = _sut.GetSaveId();
            var result2 = _sut.GetSaveId();
            var result3 = _sut.GetSaveId();

            // Assert
            Assert.NotNull(result1);
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }
    }
}
