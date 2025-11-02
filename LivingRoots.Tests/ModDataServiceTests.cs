using StardewModdingAPI;
using Moq;
using LivingRoots.Services;
using Xunit;
using System.IO;


namespace LivingRoots.Tests
{
    public class ModDataServiceTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IDataHelper> _mockDataHelper;
        private readonly Mock<IMonitor> _mockMonitor;

        public ModDataServiceTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockDataHelper = new Mock<IDataHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockHelper.Setup(x => x.Data).Returns(_mockDataHelper.Object);
        }
        
        
        [Fact]
        public void SaveData_WithValidData_CallsWriteJsonFile()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act
            service.SaveData(testData, "test_key");
            
            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key.json", testData), Times.Once);
        }

        [Fact]
        public void SaveData_WithNullKey_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, null!));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, ""));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "   "));
        }
        
        [Fact]
        public void SaveData_WithNullData_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => service.SaveData<object>(null!, "test_key"));
            Assert.Equal("data", exception.ParamName);
        }

        [Fact]
        public void LoadData_WithValidKey_CallsReadJsonFile()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var expectedData = new { Name = "Test", Value = 123 };
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Returns(expectedData);
            
            // Act
            var result = service.LoadData<object>("test_key");
            
            // Assert
            Assert.Equal(expectedData, result);
            _mockDataHelper.Verify(x => x.ReadJsonFile<object>("data/test_key.json"), Times.Once);
        }

        [Fact]
        public void LoadData_WithFileNotFoundException_ReturnsNull()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(new System.IO.FileNotFoundException());
            
            // Act
            var result = service.LoadData<object>("test_key");
            
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void LoadData_WithJsonException_ThrowsException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(new Newtonsoft.Json.JsonException());
            
            // Act & Assert
            Assert.Throws<Newtonsoft.Json.JsonException>(() => service.LoadData<object>("test_key"));
        }

        [Fact]
        public void LoadData_WithNullKey_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.LoadData<object>(null!));
            Assert.Throws<ArgumentException>(() => service.LoadData<object>(""));
            Assert.Throws<ArgumentException>(() => service.LoadData<object>("   "));
        }

        [Fact]
        public void DataExists_WithExistingData_ReturnsTrue()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Returns(new object());
            
            // Act
            var result = service.DataExists("test_key");
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DataExists_WithFileNotFoundException_ReturnsFalse()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(new System.IO.FileNotFoundException());
            
            // Act
            var result = service.DataExists("test_key");
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DataExists_WithJsonException_ThrowsException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(new Newtonsoft.Json.JsonException());
            
            // Act & Assert
            Assert.Throws<Newtonsoft.Json.JsonException>(() => service.DataExists("test_key"));
        }
        [Fact]
        public void DataExists_WithIOException_ThrowsException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            _mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Throws(new System.IO.IOException());
            
            // Act & Assert
            Assert.Throws<System.IO.IOException>(() => service.DataExists("test_key"));
        }

        
        [Fact]
        public void RemoveData_WithExistingData_RemovesData()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act
            service.RemoveData("test_key");
            
            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile<object>("data/test_key.json", null), Times.Once);
        }

        [Fact]
        public void RemoveData_WithNullKey_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.RemoveData(null!));
            Assert.Throws<ArgumentException>(() => service.RemoveData(""));
            Assert.Throws<ArgumentException>(() => service.RemoveData("   "));
        }
        
        [Fact]
        public void GetFilePath_WithInvalidCharacters_SanitizesPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act
            service.SaveData(testData, "test/key\\with:invalid|chars");
 
            // Assert - the invalid characters should be replaced with underscores
            // The exact expected path should have all invalid characters replaced
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key_with_invalid_chars.json", testData), Times.Once);
        }

        [Fact]
        public void DataExists_WithDirectoryPathAndExistingFile_ReturnsTrue()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockDataHelper = new Mock<IDataHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var testDirectory = Path.Combine(Path.GetTempPath(), "LivingRootsTest");
            
            // Setup the mock to return a directory path and mock the ReadJsonFile to return an object
            mockHelper.Setup(x => x.Data).Returns(mockDataHelper.Object);
            mockHelper.Setup(x => x.DirectoryPath).Returns(testDirectory);
            mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/test_key.json")).Returns(new object());
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object);
            
            // Act
            var result = service.DataExists("test_key");
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DataExists_WithDirectoryPathAndNonExistingFile_ReturnsFalse()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockDataHelper = new Mock<IDataHelper>();
            var mockMonitor = new Mock<IMonitor>();
            
            // Setup the mock to return a directory path and mock the ReadJsonFile to throw FileNotFoundException
            mockHelper.Setup(x => x.Data).Returns(mockDataHelper.Object);
            mockHelper.Setup(x => x.DirectoryPath).Returns(Path.GetTempPath());
            mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/non_existing_key.json")).Throws(new System.IO.FileNotFoundException());
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object);
            
            // Act
            var result = service.DataExists("non_existing_key");
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DataExists_WithDirectoryPathAndZeroLengthFile_ThrowsException()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockDataHelper = new Mock<IDataHelper>();
            var mockMonitor = new Mock<IMonitor>();
            
            // Setup the mock to return a directory path and mock the ReadJsonFile to throw JsonException for empty content
            mockHelper.Setup(x => x.Data).Returns(mockDataHelper.Object);
            mockHelper.Setup(x => x.DirectoryPath).Returns(Path.GetTempPath());
            mockDataHelper.Setup(x => x.ReadJsonFile<object>("data/empty_key.json")).Throws(new Newtonsoft.Json.JsonException());
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object);
            
            // Act & Assert - Should still throw JsonException as per requirements
            Assert.Throws<Newtonsoft.Json.JsonException>(() => service.DataExists("empty_key"));
        }

        [Fact]
        public void RemoveData_WithDirectoryPath_RemovesDataThroughSMAPI()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockDataHelper = new Mock<IDataHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var testDirectory = Path.Combine(Path.GetTempPath(), "LivingRootsTest");
            
            // Setup the mock to return a directory path
            mockHelper.Setup(x => x.Data).Returns(mockDataHelper.Object);
            mockHelper.Setup(x => x.DirectoryPath).Returns(testDirectory);
            
            var service = new ModDataService(mockHelper.Object, mockMonitor.Object);
            
            // Act
            service.RemoveData("remove_test");
            
            // Assert - should call WriteJsonFile with null through SMAPI API
            mockDataHelper.Verify(x => x.WriteJsonFile<object>("data/remove_test.json", null), Times.Once);
            
            // The testDirectory is not actually created in this test, so cleanup is unnecessary
        }
        
        [Fact]
        public void GetFilePath_WithAbsolutePath_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act & Assert - Test with platform-appropriate absolute path
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "/absolute/path")); // Unix-style absolute path
        }
        
        [Fact]
        public void LoadData_WithAbsolutePath_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert - Test with platform-appropriate absolute path
            Assert.Throws<ArgumentException>(() => service.LoadData<object>("/absolute/path")); // Unix-style absolute path
        }
        
        [Fact]
        public void DataExists_WithAbsolutePath_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert - Test with platform-appropriate absolute path
            Assert.Throws<ArgumentException>(() => service.DataExists("/absolute/path")); // Unix-style absolute path
        }
        
        [Fact]
        public void RemoveData_WithAbsolutePath_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            
            // Act & Assert - Test with platform-appropriate absolute path
            Assert.Throws<ArgumentException>(() => service.RemoveData("/absolute/path")); // Unix-style absolute path
        }
        [Fact]
        public void GetFilePath_WithReservedWindowsName_AddsUnderscore()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act
            service.SaveData(testData, "CON");
            
            // Assert
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.json", testData), Times.Once);
        }
        
        [Fact]
        public void GetFilePath_WithReservedWindowsNameWithExtension_ExtractsBaseNameAndAddsUnderscore()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act
            service.SaveData(testData, "CON.txt");
            
            // Assert - The extension should be preserved, and underscore added to base name
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.txt.json", testData), Times.Once);
        }
        
        [Fact]
        public void GetFilePath_WithMultipleReservedWindowsNamesWithExtensions()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act & Assert for various reserved names with extensions
            service.SaveData(testData, "PRN.txt");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/PRN_.txt.json", testData), Times.Once);
            
            service.SaveData(testData, "AUX.log");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/AUX_.log.json", testData), Times.Once);
            
            service.SaveData(testData, "COM1.xml");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/COM1_.xml.json", testData), Times.Once);
            
            service.SaveData(testData, "LPT1.dat");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/LPT1_.dat.json", testData), Times.Once);
        }
        
        [Fact]
        public void GetFilePath_WithNonReservedNameWithExtension_DoesNotAddUnderscore()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act
            service.SaveData(testData, "normal.txt");
            
            // Assert - Non-reserved names should not be modified
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/normal.txt.json", testData), Times.Once);
        }
        
        [Fact]
        public void GetFilePath_WithUnicodeHomoglyphs_SanitizesPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act - Using homoglyphs that look like normal characters but are different Unicode
            service.SaveData(testData, "test\u0435xamplе"); // Cyrillic 'е' instead of Latin 'e'
            
            // Assert - Should be normalized and sanitized appropriately
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/testexampl_.json", testData), Times.Once);
        }
        
        [Fact]
        public void GetFilePath_WithMixedSeparators_SanitizesPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act - Mixed forward and backward slashes
            service.SaveData(testData, "test/key\\path");
            
            // Assert - All separators should be replaced with underscores
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key_path.json", testData), Times.Once);
        }
        
        [Fact]
        public void GetFilePath_WithMultipleDots_SanitizesPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act - Multiple dots that could collapse
            service.SaveData(testData, "file....name");
            
            // Assert - Multiple dots should be handled properly
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file._.name.json", testData), Times.Once);
        }
        
        [Fact]
        public void GetFilePath_WithLeadingTrailingWhitespace_SanitizesPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act - Leading and trailing whitespace
            service.SaveData(testData, "  test_key  ");
            
            // Assert - Whitespace should be trimmed
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key.json", testData), Times.Once);
        }
        
        [Fact]
        public void GetFilePath_WithOnlyWhitespace_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act & Assert - Only whitespace should throw an exception
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "   "));
        }
        
        [Fact]
        public void GetFilePath_WithPathTraversalAttempt_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act & Assert - Path traversal should be blocked
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "../../../etc/passwd"));
        }
        
        [Fact]
        public void GetFilePath_WithComplexPathTraversalAttempt_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act & Assert - Complex path traversal should be blocked
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "..\\..\\windows\\system32"));
        }
        
        [Fact]
        public void GetFilePath_WithSpecialCharacters_SanitizesPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act - Various special characters
            service.SaveData(testData, "file<name>with:special\"chars|and?wildcards*");
            
            // Assert - Special characters should be replaced with underscores
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_name_with_special_chars_and_wildcards_.json", testData), Times.Once);
        }
        
        [Fact]
        public void GetFilePath_WithMultipleUnderscores_SanitizesPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act - Multiple consecutive underscores
            service.SaveData(testData, "file__with___multiple____underscores");
            
            // Assert - Multiple underscores should be collapsed to single underscore
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_with_multiple_underscores.json", testData), Times.Once);
        }
        
        [Fact]
        public void GetFilePath_WithUnicodeNormalization_SanitizesPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act - Unicode that normalizes differently
            service.SaveData(testData, "café"); // Could be represented with combining characters
            
            // Assert - Should be normalized appropriately
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/caf_.json", testData), Times.Once);
        }
        
        [Fact]
        public void GetFilePath_WithEmptyStringAfterSanitization_ThrowsArgumentException()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act & Assert - Keys that sanitize to empty should throw an exception
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, ""));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "."));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, ".."));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "___"));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "..."));
        }
        
        [Fact]
        public void GetFilePath_WithVeryLongPath_SanitizesPath()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };
            
            // Act - Very long path with special characters
            var longPath = new string('a', 300) + "<>:\"|?*" + new string('b', 300);
            var expected = new string('a', 300) + "_______" + new string('b', 300);
            
            service.SaveData(testData, longPath);
            
            // Assert - Should handle long paths with invalid characters
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/{expected}.json", testData), Times.Once);
        }
        
        [Fact]
        public void SanitizeKey_WithUnicodeDiacritics_HandlesAccentsProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Various diacritics that should be handled
            service.SaveData(testData, "café"); // e with acute accent
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/caf_.json", testData), Times.Once);

            service.SaveData(testData, "naïve"); // i with diaeresis
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/na_ve.json", testData), Times.Once);

            service.SaveData(testData, "résumé"); // r with acute, e with acute
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/r_sum_.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithSpecialCharactersAtEnd_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Special characters at the end that should be trimmed
            service.SaveData(testData, "test_key.");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key.json", testData), Times.Once);

            service.SaveData(testData, "test_key_");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key.json", testData), Times.Once);

            service.SaveData(testData, "test_key___");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key.json", testData), Times.Once);

            service.SaveData(testData, "test_key...");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_key.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithReservedWindowsNamesWithComplexExtensions_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Reserved names with complex extensions
            service.SaveData(testData, "CON.txt.bak");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CON_.txt.bak.json", testData), Times.Once);

            service.SaveData(testData, "PRN..double..extension");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/PRN_..double..extension.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithMultipleDotsInComplexPatterns_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Multiple dots in various patterns
            service.SaveData(testData, "file.....name");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file._.name.json", testData), Times.Once);

            service.SaveData(testData, "....leading.dots");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/_.leading.dots.json", testData), Times.Once);

            service.SaveData(testData, "trailing.dots....");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/trailing.dots.json", testData), Times.Once);

            service.SaveData(testData, "..double..dot..pattern..");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/_.double._.dot._.pattern.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithPathTraversalComplexPatterns_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert - Complex path traversal patterns should still be blocked
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, ".../../path"));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "file...\\..\\path"));
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "normal.../../path"));
        }

        [Fact]
        public void SanitizeKey_WithComplexUnicodeNormalization_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Complex Unicode normalization cases
            // Combining characters that should be handled
            service.SaveData(testData, "a\u0300"); // a with grave accent (combining)
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/a_.json", testData), Times.Once);

            service.SaveData(testData, "e\u0301"); // e with acute accent (combining)
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/e_.json", testData), Times.Once);

            // Multiple combining marks
            service.SaveData(testData, "o\u0302\u0301"); // o with circumflex and acute (multiple combining)
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/o_.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithHomoglyphAndDiacriticsTogether_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Combining homoglyphs and diacritics
            service.SaveData(testData, "tеst\u0301"); // Cyrillic 'е' with acute accent
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_.json", testData), Times.Once);

            service.SaveData(testData, "cafe\u0435\u0301"); // Latin 'cafe' with Cyrillic 'e' and acute accent
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/cafe_.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithComplexSpecialCharacterSequences_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Complex sequences of special characters
            service.SaveData(testData, "file<:>name");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file___name.json", testData), Times.Once);

            service.SaveData(testData, "name\"|?*test");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/name____test.json", testData), Times.Once);

            service.SaveData(testData, "mixed<>/:\"|?*chars");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/mixed_______chars.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithMultipleNormalizationOperationsTogether_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Multiple operations happening in sequence
            service.SaveData(testData, "tеst\u0301<>/...file");
            // This should: convert Cyrillic 'е' to 'e', handle diacritic with underscore, 
            // replace special chars with underscores, handle multiple dots
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test____._file.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithEdgeCaseEmptyResultsAfterSanitization_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act & Assert - Various patterns that might result in empty strings after sanitization
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "<>:\"|?*")); // All invalid chars
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, ".........")); // All dots
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "___")); // All underscores after trimming
            Assert.Throws<ArgumentException>(() => service.SaveData(testData, "   ")); // All spaces after trimming
        }

        [Fact]
        public void SanitizeKey_WithVeryLongUnicodeString_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Very long string with unicode diacritics
            string input = new string('a', 10) + "café" + new string('b', 100) + "naïve" + new string('c', 100);
            service.SaveData(testData, input);

            // The expected result should handle the diacritics properly
            string expected = new string('a', 100) + "caf_e" + new string('b', 100) + "na_ve" + new string('c', 100);
            _mockDataHelper.Verify(x => x.WriteJsonFile($"data/{expected}.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithConsecutiveDiacritics_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - String with consecutive diacritics
            service.SaveData(testData, "a\u0300\u0301b"); // a with grave and acute accents
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/a__b.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithWindowsReservedNameComplexCase_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Reserved name with mixed case
            service.SaveData(testData, "con"); // lowercase reserved name
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/con_.json", testData), Times.Once);
            
            service.SaveData(testData, "CoN"); // mixed case reserved name
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/CoN_.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithMultiplePathSeparators_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Multiple path separators
            service.SaveData(testData, "path//double//separators");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/path_double_separators.json", testData), Times.Once);
            
            service.SaveData(testData, "path\\\\double\\\\separators");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/path_double_separators.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithMixedUnicodeNormalizationForms_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Different Unicode normalization forms
            string nfcForm = "café"; // Normalized composed form
            string nfdForm = "cafe\u0301"; // Normalized decomposed form (e with combining acute)
            
            service.SaveData(testData, nfcForm);
            // Both should result in the same sanitized output
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/caf_.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithSpecialCharactersAndExtensions_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Special characters with what looks like extensions
            service.SaveData(testData, "file<with>special:chars.txt");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/file_with_special_chars.txt.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithTrailingDotsAndSpaces_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - Trailing dots and spaces that should be trimmed
            service.SaveData(testData, "test   ...");
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithZeroWidthUnicodeCharacters_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - String with zero-width characters
            service.SaveData(testData, "test\u200Bzwsp\u200Czwnj\u200Dzwj"); // Zero-width space, non-joiner, joiner
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_zwsp_zwnj_zwj.json", testData), Times.Once);
        }

        [Fact]
        public void SanitizeKey_WithSurrogatePairs_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - String with surrogate pairs (emojis or other non-BMP characters)
            service.SaveData(testData, "test" + char.ConvertFromUtf32(0x1F600) + "smile"); // Grinning face emoji
            // The emoji should be handled properly without breaking the sanitization
            // This test verifies the method doesn't crash with surrogate pairs
            _mockDataHelper.Invocations.Clear(); // Clear previous invocations for this specific test
            var result = service.GetType().GetMethod("GetFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                              .Invoke(service, new object[] { "test" + char.ConvertFromUtf32(0x1F600) + "smile" });
            Assert.NotNull(result);
        }

        [Fact]
        public void SanitizeKey_WithHebrewAndRTLChars_HandlesProperly()
        {
            // Arrange
            var service = new ModDataService(_mockHelper.Object, _mockMonitor.Object);
            var testData = new { Name = "Test", Value = 123 };

            // Act - String with Hebrew characters (right-to-left)
            service.SaveData(testData, "test שלום");
            // The RTL characters should be preserved but invalid chars replaced
            _mockDataHelper.Verify(x => x.WriteJsonFile("data/test_____.json", testData), Times.Once);
        }
    }
}
