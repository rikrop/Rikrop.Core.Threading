using System.Threading;
using System.Threading.Tasks;
using Microsoft.Runtime.CompilerServices;

namespace Rikrop.Core.Threading
{
    public class CountdownEventAsync
    {
        private readonly ManualResetEventAsync _manualResetEventAsync;

        private int _initialCount;
        private int _currentCount;
        private readonly Task<bool> _notAwaitableTask = TaskEx.FromResult(true);

        public int CurrentCount
        {
            get { return _currentCount; }
        }

        public int MinimalThresholdValue { get; set; }

        public CountdownEventAsync()
        {
            _manualResetEventAsync = new ManualResetEventAsync(false);
        }

        public CountdownEventAsync(int minimalThresholdValue)
            : this()
        {
            MinimalThresholdValue = minimalThresholdValue;
        }

        public void Reset()
        {
            Reset(_initialCount);
        }

        public void Reset(int count)
        {
            Interlocked.Exchange(ref _initialCount, count);
            Interlocked.Exchange(ref _currentCount, count);

            if (count == 0)
            {
                StopAwait();
            }
        }

        public void AddOnce()
        {
            Add(1);
        }

        public void Add(int count)
        {
            Interlocked.Add(ref _currentCount, count);
        }

        public void Signal()
        {
            if (_currentCount <= 0)
            {
                return;
            }

            var decrementedCount = Interlocked.Decrement(ref _currentCount);
            if (decrementedCount <= MinimalThresholdValue)
            {
                StopAwait();
            }
            else if (decrementedCount < 0)
            {
                Interlocked.Increment(ref _currentCount);
            }
        }

        public Task WaitAsync()
        {
            if (_currentCount <= MinimalThresholdValue)
            {
                return _notAwaitableTask;
            }

            lock (_manualResetEventAsync)
            {
                if (_currentCount <= MinimalThresholdValue)
                {
                    return _notAwaitableTask;
                }

                return _manualResetEventAsync.WaitAsync();
            }
        }

        public TaskAwaiter GetAwaiter()
        {
            return WaitAsync().GetAwaiter();
        }

        private void StopAwait()
        {
            lock (_manualResetEventAsync)
            {
                _manualResetEventAsync.StopAwait();
            }
        }
    }
}