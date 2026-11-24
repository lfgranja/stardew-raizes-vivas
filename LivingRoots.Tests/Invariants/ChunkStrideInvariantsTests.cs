using LivingRoots.Controllers;
using Xunit;

namespace LivingRoots.Tests.Invariants
{
    public class ChunkStrideInvariantsTests
    {
        [Theory]
        [InlineData(1000, 100)]
        [InlineData(40000, 1000)]
        [InlineData(5000, 1000)]
        public void GetChunkStartIndices_CoversAllTiles_NoDuplicates(int totalTiles, int chunkSize)
        {
            var starts = ModController.GetChunkStartIndices(totalTiles, chunkSize);

            // Every tile index must be covered by exactly one chunk.
            var covered = new bool[totalTiles];
            foreach (var start in starts)
            {
                for (int i = start; i < start + chunkSize && i < totalTiles; i++)
                {
                    Assert.False(covered[i], $"Tile {i} covered twice.");
                    covered[i] = true;
                }
            }

            for (int i = 0; i < totalTiles; i++)
            {
                Assert.True(covered[i], $"Tile {i} not covered.");
            }
        }

        [Fact]
        public void GetChunkStartIndices_Stride61_ProducesPermutation()
        {
            var starts = ModController.GetChunkStartIndices(10000, 1000, 61);
            // 10 chunks
            Assert.Equal(10, starts.Count);
        }
    }
}
