using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kuriimu2.ImGui.Models
{
    class AsyncOperation
    {
        private readonly object _runningLock = new object();

        private CancellationTokenSource _cts;

        public event EventHandler Started;
        public event EventHandler Finished;

        public bool IsRunning { get; private set; }

        public bool WasCancelled { get; private set; }

        public async Task StartAsync(Func<CancellationTokenSource, Task> action)
        {
            // Check running condition
            lock (_runningLock)
            {
                if (IsRunning)
                    throw new InvalidOperationException("An operation is already running.");

                IsRunning = true;
                WasCancelled = false;

                // Invoke StateChanged event
                OnStarted();
            }

            // Execute async action
            _cts = new CancellationTokenSource();
            try
            {
                await Task.Run(async () => await action(_cts));
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation
            }

            // Reset running condition
            lock (_runningLock)
            {
                IsRunning = false;
                WasCancelled = _cts.IsCancellationRequested;

                // Invoke StateChanged event
                OnFinished();
            }
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        private void OnStarted()
        {
            Started?.Invoke(this, new EventArgs());
        }

        private void OnFinished()
        {
            Finished?.Invoke(this, new EventArgs());
        }
    }
}
