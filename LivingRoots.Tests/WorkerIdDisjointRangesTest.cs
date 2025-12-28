using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Xunit;

namespace LivingRoots.Tests
{
    public class WorkerIdDisjointRangesTest
    {
        [Fact]
        public async Task ThreadSafety_WithDisjointTileRanges_VerifiesUniqueWorkerIds()
        {
            // Arrange
            var mockDataService = new Mock<IModDataService>();
            var mockMonitor = new Mock<IMonitor>();
            var mockFileNameSanitizationService = new Mock<IFileNameSanitizationService>();
            var service = new SoilHealthService(mockDataService.Object, mockMonitor.Object, mockFileNameSanitizationService.Object);
            var exceptions = new List<Exception>();
            var lockObj = new object();
            var accessedWorkerIds = new List<int>();
            var workerIdLock = new object();

            // Act - Multiple threads accessing the service simultaneously with disjoint tile ranges
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var workerId = i; // Capture per-iteration value to avoid closure issues
                var task = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            int x = (workerId * 100) + j;
                            int y = workerId;
                            var tile = new Vector2(x, y);
                            service.SetSoilHealth("Farm", tile, j % 10 * 5.0f); // Keep values within [0,100] range
                            service.GetSoilHealth("Farm", tile);
                            service.UpdateHealth("Farm", tile, 1.0f);
                        }
                        
                        // Record which worker ID was used
                        lock (workerIdLock)
                        {
                            accessedWorkerIds.Add(workerId);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
                tasks.Add(task);
            }

            // Add timeout to prevent hanging indefinitely
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var whenAllTask = Task.WhenAll(tasks);
            
            var completedTask = await Task.WhenAny(whenAllTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Assert.Fail("ThreadSafety_WithDisjointTileRanges_VerifiesUniqueWorkerIds test timed out after 30 seconds");
            }
            
            // Wait for the actual tasks to complete if they haven't already
            await whenAllTask;

            // Assert - No exceptions should have occurred due to race conditions
            Assert.Empty(exceptions);
            
            // Verify that each worker used a unique workerId
            var uniqueWorkerIds = new HashSet<int>(accessedWorkerIds);
            Assert.Equal(10, uniqueWorkerIds.Count); // Should have 10 unique worker IDs
            
            // Verify that all expected worker IDs were used (0 through 9)
            for (int i = 0; i < 10; i++)
            {
                Assert.Contains(i, uniqueWorkerIds);
            }
        }
    }
}
