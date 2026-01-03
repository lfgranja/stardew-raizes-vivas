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
                // Simulate some work that causes an exception
                throw new InvalidOperationException("Worker task exception occurred!");
            });

            // The problematic pattern: only adding the continuation task to be awaited
            // The original worker task is not awaited, so unobserved exceptions can occur
            tasks.Add(task.ContinueWith(continuationTask =>
            {
                // Handle the exception from the original task to prevent unobserved exception
                if (continuationTask.IsFaulted && continuationTask.Exception != null)
                {
                    lock (lockObj)
                    {
                        exceptions.AddRange(continuationTask.Exception.InnerExceptions);
                    }
                }
            }));

            // This should await all tasks, but the original task is not properly awaited
            await Task.WhenAll(tasks);
            // Ensure the worker exception is observed to prevent flaky UnobservedTaskException later.
            _ = task.Exception;

            // The test should detect the exception in the continuation handling
            Assert.NotEmpty(exceptions);
            Assert.Contains(exceptions, ex => ex.Message.Contains("Worker task exception occurred!"));
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
                // Simulate some work that causes an exception
                throw new InvalidOperationException("Worker task exception occurred!");
            });

            // The correct pattern: both the original task and its continuation are awaited
            var recordTask = task.ContinueWith(continuationTask =>
            {
                // Handle the exception from the original task to capture it properly
                if (continuationTask.IsFaulted && continuationTask.Exception != null)
                {
                    lock (lockObj)
                    {
                        exceptions.AddRange(continuationTask.Exception.InnerExceptions);
                    }
                }
            });

            tasks.Add(task); // Add the original task to be awaited (this will throw)
            tasks.Add(recordTask);

            // This will properly await all tasks, ensuring exceptions are observed
            // We need to catch the exception that will be thrown when awaiting the faulted task
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Task.WhenAll(tasks));

            // The test should detect the exception in the worker task through proper exception handling
            // We also check that the continuation captured the exception
            Assert.Contains(exceptions, ex => ex.Message.Contains("Worker task exception occurred!"));
        }
    }
}
