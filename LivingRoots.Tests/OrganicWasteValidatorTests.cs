using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using LivingRoots.Tests.Fixtures;
using Moq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests
{
    public class OrganicWasteValidatorTests
    {
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly OrganicWasteValidator _validator;

        public OrganicWasteValidatorTests()
        {
            _mockMonitor = new Mock<IMonitor>();
            _validator = new OrganicWasteValidator(_mockMonitor.Object);
        }

        [Fact]
        public void IsValidOrganicWaste_Seeds_ReturnsTrue_FR41()
        {
            // Arrange
            var item = ItemFactory.CreateItem("Seeds", -74);

            // Act
            var result = _validator.IsValidOrganicWaste(item);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidOrganicWaste_Vegetables_ReturnsTrue_FR41()
        {
            // Arrange
            var item = ItemFactory.CreateItem("Vegetable", -75);

            // Act
            var result = _validator.IsValidOrganicWaste(item);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidOrganicWaste_Fruits_ReturnsTrue_FR41()
        {
            // Arrange
            var item = ItemFactory.CreateItem("Fruit", -79);

            // Act
            var result = _validator.IsValidOrganicWaste(item);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidOrganicWaste_Flowers_ReturnsTrue_FR41()
        {
            // Arrange
            var item = ItemFactory.CreateItem("Flower", -80);

            // Act
            var result = _validator.IsValidOrganicWaste(item);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidOrganicWaste_Forage_ReturnsTrue_FR41()
        {
            // Arrange
            var item = ItemFactory.CreateItem("Forage", -81);

            // Act
            var result = _validator.IsValidOrganicWaste(item);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidOrganicWaste_Stone_ReturnsFalse_FR42()
        {
            // Arrange
            var item = ItemFactory.CreateItem("Stone", -12);

            // Act
            var result = _validator.IsValidOrganicWaste(item);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidOrganicWaste_Wood_ReturnsFalse_FR42()
        {
            // Arrange
            var item = ItemFactory.CreateItem("Wood", -14);

            // Act
            var result = _validator.IsValidOrganicWaste(item);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidOrganicWaste_NullItem_ReturnsFalse_FR43()
        {
            // Arrange
            Item? item = null;

            // Act
            var result = _validator.IsValidOrganicWaste(item!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidOrganicWaste_CompostableTag_ReturnsTrue()
        {
            // Arrange
            var item = ItemFactory.CreateItem("ModItem", -999);
            item.modData["ContextTag.compostable_item"] = "true";

            // Act
            var result = _validator.IsValidOrganicWaste(item);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidOrganicWaste_NotCompostableTag_ReturnsFalse()
        {
            // Arrange
            var item = ItemFactory.CreateItem("Seeds", -74);
            item.modData["ContextTag.not_compostable"] = "true";

            // Act
            var result = _validator.IsValidOrganicWaste(item);

            // Assert
            Assert.False(result);
        }
    }
}
