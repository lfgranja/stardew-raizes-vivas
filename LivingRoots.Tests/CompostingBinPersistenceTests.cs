using System.Collections.Generic;
using LivingRoots.Services;
using Xunit;

namespace LivingRoots.Tests
{
    public class CompostingBinPersistenceTests
    {
        /// <summary>T123 [P] [US2] RED — FR-012 (partial): null payload isolates per location.</summary>
        [Fact]
        public void T123_NullLocationPayload_IsolatesThatLocation_Only()
        {
            // RED: tolerant reader aborts whole load when a single location payload is null.
            Assert.True(false, "RED T123: null payload aborts entire load instead of isolating per location (FR-012 partial).");
        }
    }
}
