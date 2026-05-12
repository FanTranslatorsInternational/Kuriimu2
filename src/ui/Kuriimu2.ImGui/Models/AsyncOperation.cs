using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kuriimu2.ImGui.Models
{
    internal class AsyncOperation
    {
        private readonly object _runningLock = new();

        private CancellationTokenSource? _cts;

        public event EventHandler? Started;
        public event EventHandler? Finished;

        public bool IsRunning { get; private set; }

        public bool WasCancelled { get; private set; }
        public bool WasSuccessful { get; private set; }

        public Exception? Exception { get; private set; }

        public async Task StartAsync(Func<CancellationTokenSource, Task> action)
        {
            // Check running condition
            if (!StartOperation())
                return;

            // Execute async action
            _cts = new CancellationTokenSource();
            try
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        await action(_cts);

                        WasSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        Exception = ex;
                    }
                }, _cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation
            }

            // Reset running condition
            FinalizeOperation();
        }

        public async Task StartAsync(Action<CancellationTokenSource> action)
        {
            // Check running condition
            if (!StartOperation())
                return;

            // Execute async action
            _cts = new CancellationTokenSource();
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        action(_cts);

                        WasSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        Exception = ex;
                    }
                }, _cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation
            }

            // Reset running condition
            FinalizeOperation();
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        private bool StartOperation()
        {
            lock (_runningLock)
            {
                if (IsRunning)
                    return false;

                IsRunning = true;
                WasCancelled = false;
                WasSuccessful = false;

                // Invoke StateChanged event
                OnStarted();
            }

            return true;
        }

        private void FinalizeOperation()
        {
            lock (_runningLock)
            {
                IsRunning = false;
                WasCancelled = _cts?.IsCancellationRequested ?? false;

                // Invoke StateChanged event
                OnFinished();
            }
        }

        private void OnStarted()
        {
            Started?.Invoke(this, EventArgs.Empty);
        }

        private void OnFinished()
        {
            Finished?.Invoke(this, EventArgs.Empty);
        }
    }
}
