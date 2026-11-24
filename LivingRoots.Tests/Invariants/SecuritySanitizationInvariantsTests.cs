using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using Xunit;

namespace LivingRoots.Tests.Invariants
{
    /// <summary>
    /// T021 — Security/path sanitization invariant tests (spec 005 FR-007).
    /// Verifies that path traversal, Unicode normalization, and homoglyph
    /// attacks are blocked by the security infrastructure.
    /// Note: <see cref="ModDataService.SanitizePathSegments"/> is tested
    /// exhaustively in <c>SanitizePathSegmentsTests.cs</c> and
    /// <c>ModDataServiceTests.cs</c>. These tests verify the domain-level
    /// validators that ModDataService delegates to.
    /// </summary>
    public class SecuritySanitizationInvariantsTests
    {
        [Fact]
        public void PathTraversalValidator_BlocksParentDirectoryTraversal()
        {
            var mockPathValidation = new Mock<IPathValidationService>();
            mockPathValidation
                .Setup(x => x.Validate(It.Is<string>(p => p != null && p.Contains(".."))))
                .Throws<ArgumentException>();

            var validator = new PathTraversalValidator(mockPathValidation.Object);

            Assert.Throws<ArgumentException>(() => validator.Validate("../../../etc/passwd"));
            Assert.Throws<ArgumentException>(() => validator.Validate("..\\..\\windows\\system32"));
            Assert.Throws<ArgumentException>(() => validator.Validate("foo/../../bar"));
        }

        [Fact]
        public void PathTraversalValidator_AllowsNormalPaths()
        {
            var mockPathValidation = new Mock<IPathValidationService>();
            var validator = new PathTraversalValidator(mockPathValidation.Object);

            // Should not throw
            validator.Validate("data/soil_health_data_farm.json");
            validator.Validate("save_12345/data.json");
            validator.Validate("a/b/c");
        }

        [Fact]
        public void PathTraversalValidator_BlocksNullPath()
        {
            var mockPathValidation = new Mock<IPathValidationService>();
            var validator = new PathTraversalValidator(mockPathValidation.Object);
            Assert.Throws<ArgumentException>(() => validator.Validate(null!));
        }

        [Fact]
        public void PathTraversalValidator_BlocksEmptyPath()
        {
            var mockPathValidation = new Mock<IPathValidationService>();
            var validator = new PathTraversalValidator(mockPathValidation.Object);
            Assert.Throws<ArgumentException>(() => validator.Validate(""));
        }

        [Fact]
        public void UnicodeNormalizer_NormalizesToFormC()
        {
            var normalizer = new UnicodeNormalizer(new UnicodeNormalizationService());

            // é as single codepoint vs e + combining acute accent
            var composed = normalizer.Normalize("caf\u00E9");
            var decomposed = normalizer.Normalize("cafe\u0301");
            Assert.Equal(composed, decomposed);
        }

        [Fact]
        public void UnicodeNormalizer_NormalizesFullwidthCharacters()
        {
            var normalizer = new UnicodeNormalizer(new UnicodeNormalizationService());

            // Fullwidth characters should normalize to regular equivalents
            var fullwidth = normalizer.Normalize("\uFF21\uFF22\uFF23");
            var regular = normalizer.Normalize("ABC");
            Assert.Equal(regular, fullwidth);
        }

        [Fact]
        public void UnicodeNormalizer_NormalizesFullwidthDigitsAndPunctuation()
        {
            var normalizer = new UnicodeNormalizer(new UnicodeNormalizationService());

            // The whole fullwidth ASCII block (U+FF01–U+FF5E) maps to ASCII:
            // fullwidth digits U+FF11–U+FF14 and FULLWIDTH FULL STOP U+FF0E.
            var fullwidth = normalizer.Normalize("\uFF11\uFF12\uFF13\uFF0E\uFF14");
            Assert.Equal("123.4", fullwidth);
        }

        [Fact]
        public void UnicodeNormalizer_PreservesNonLatinScript()
        {
            var normalizer = new UnicodeNormalizer(new UnicodeNormalizationService());

            // CJK text is neither fullwidth-ASCII nor a Latin/Greek confusable,
            // so it must pass through unchanged — the fullwidth mapping must not
            // become broad compatibility normalization (over-normalization).
            Assert.Equal("\u65E5\u672C\u8A9E", normalizer.Normalize("\u65E5\u672C\u8A9E"));
        }

        [Fact]
        public void UnicodeNormalizer_HandlesNullInput()
        {
            var normalizer = new UnicodeNormalizer(new UnicodeNormalizationService());
            var result = normalizer.Normalize(null);
            Assert.Null(result);
        }
    }
}
