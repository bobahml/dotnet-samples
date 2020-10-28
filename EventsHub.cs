using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Demo
{
    /// <summary>
    /// Waiting for events from RabbitMQ (or some other external source)
    /// </summary>
    /// <remarks>Class can be used for long polling</remarks>
    public sealed class EventsHub
    {
        /// <summary>
        /// Waiting tasks 
        /// </summary>
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> eventHandlers
            = new ConcurrentDictionary<Guid, TaskCompletionSource<bool>>();

        /// <summary>
        /// Asynchronous waiting for events by key
        /// </summary>
        /// <param name="key">object identifier</param>
        /// <param name="timeoutMs">max waiting time</param>
        /// <returns>True if event occurred, otherwise false</returns>
        public async Task<bool> WaitEventAsync(Guid key, int timeoutMs)
        {
            var source = eventHandlers.GetOrAdd(key, new TaskCompletionSource<bool>());
            var task = source.Task;

            using (var cts = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs, cts.Token)).ConfigureAwait(false);
                if (completedTask == task)
                {
                    cts.Cancel();
                    return await task;
                }

                return false;
            }
        }

        /// <summary>
        /// Notify about new event
        /// </summary>
        /// <param name="key">object identifier</param>
        public void FireEvent(Guid key)
        {
            if (eventHandlers.TryRemove(key, out var completionSource))
            {
                completionSource.TrySetResult(true);
            }
        }
    }
}
