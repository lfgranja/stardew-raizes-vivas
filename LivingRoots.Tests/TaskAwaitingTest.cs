using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests
{
    public class TaskAwaitingTest
    {
        [Fact]
        public async Task UnobservedTaskException_DemonstratesTheIssue()
        {
            // This test demonstrates the issue where worker tasks are not properly awaited
            // causing exceptions to go unobserved and the test to pass silently
            
            var exceptions = new List<Exception>();
            var lockObj = new object();
            var tasks = new List<Task>();

            // Create a worker task that will throw an exception
            var task = Task.Run(() =>
            {
                try
                {
                    // Simulate some work that causes an exception
                    throw new InvalidOperationException("Worker task exception occurred!");
                }
                catch (Exception ex)
                {
                    lock (lockObj)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            // The problematic pattern: only adding the continuation task to be awaited
            // The original worker task is not awaited, so unobserved exceptions can occur
            tasks.Add(task.ContinueWith(_ => {
                // Some continuation work
            }));

            // This should await all tasks, but the original task is not properly awaited
            await Task.WhenAll(tasks);

            // The test should fail if there were exceptions, but due to improper awaiting
            // the exception in the original task might not be observed
            Assert.Empty(exceptions);
        }

        [Fact]
        public async Task ProperlyObservedTaskException_DemonstratesTheFix()
        {
            // This test demonstrates the correct pattern where both the main task
            // and its continuation are properly awaited
            
            var exceptions = new List<Exception>();
            var lockObj = new object();
            var tasks = new List<Task>();

            // Create a worker task that will throw an exception
            var task = Task.Run(() =>
            {
                try
                {
                    // Simulate some work that causes an exception
                    throw new InvalidOperationException("Worker task exception occurred!");
                }
                catch (Exception ex)
                {
                    lock (lockObj)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            // The correct pattern: both the original task and its continuation are awaited
            var recordTask = task.ContinueWith(_ => {
                // Some continuation work
            });

            tasks.Add(task);
            tasks.Add(recordTask);

            // This will properly await all tasks, ensuring exceptions are observed
            await Task.WhenAll(tasks);

            // The test should detect the exception in the worker task
            Assert.Empty(exceptions);
        }
    }
}