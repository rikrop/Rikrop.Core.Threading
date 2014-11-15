using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Runtime.CompilerServices;
using Timer = System.Timers.Timer;

namespace Rikrop.Core.Threading
{
    public class BatchPool<T> : IDisposable
    {
        private const int DefaultFlushThreashold = 1000;
        private const int DefaultFlushTimeout = 1000;

        private readonly Action<IEnumerable<T>> _flushAction;
        private readonly int _flushThreashold;
        private readonly TimeSpan _flushTimeout;
        private readonly ConcurrentQueue<T> _buffer;
        private readonly ManualResetEventAsync _manualResetEventAsync = new ManualResetEventAsync(false);
        private readonly Timer _timer;

        private int _flushCount;
        private int _itemsCount;
        private DateTime _lastAddedDate;

        public BatchPool(Action<IEnumerable<T>> flushAction)
            : this(flushAction, DefaultFlushThreashold, TimeSpan.FromMilliseconds(DefaultFlushTimeout))
        {
        }

        public BatchPool(Action<IEnumerable<T>> flushAction, int flushThreashold)
            : this(flushAction, flushThreashold, TimeSpan.FromMilliseconds(DefaultFlushTimeout))
        {
        }

        public BatchPool(Action<IEnumerable<T>> flushAction, int flushThreashold, TimeSpan flushTimeout)
        {
            Contract.Requires<ArgumentNullException>(flushAction != null);
            Contract.Requires<ArgumentException>(flushThreashold > 0);
            Contract.Requires<ArgumentException>(flushTimeout != TimeSpan.Zero);

            _flushAction = flushAction;
            _flushThreashold = flushThreashold;
            _flushTimeout = flushTimeout;
            _buffer = new ConcurrentQueue<T>();

            _timer = new Timer(flushTimeout.TotalMilliseconds);
            _timer.Elapsed += OnFlushTimeoutTimerElapsed;
            _timer.Start();
        }

        public void Add(T item)
        {
            _buffer.Enqueue(item);
            _lastAddedDate = DateTime.Now;

            var currentCount = Interlocked.Increment(ref _itemsCount);
            if (currentCount % _flushThreashold == 0)
            {
                FlushAsync();
            }
        }

        public Task WaitAsync()
        {
            lock (_manualResetEventAsync)
            {
                if (_itemsCount == 0 && _flushCount == 0)
                {
                    return TaskEx.FromResult(true);
                }
                return _manualResetEventAsync.WaitAsync();
            }
        }

        public TaskAwaiter GetAwaiter()
        {
            return WaitAsync().GetAwaiter();
        }

        public void ForceFlush()
        {
            FlushAsync();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private void OnFlushTimeoutTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now - _lastAddedDate >= _flushTimeout && _flushCount == 0)
            {
                FlushAsync();
            }
        }

        private async void FlushAsync()
        {
            if (_itemsCount == 0)
            {
                return;
            }

            Interlocked.Increment(ref _flushCount);
            try
            {
                await TaskEx.Run(() => Flush());
            }
            finally
            {
                //Нужно синхронизировать поток флаша с потоком, который может вызвать метод WaitAsync
                lock (_manualResetEventAsync)
                {
                    var currentFlushCount = Interlocked.Decrement(ref _flushCount);
                    if (currentFlushCount == 0 && _itemsCount == 0)
                    {
                        _manualResetEventAsync.StopAwait();
                    }
                }
            }
        }

        private void Flush()
        {
            var itemsToFlush = new List<T>(_flushThreashold);

            int dequeuedItemsCount;
            for (dequeuedItemsCount = 0; dequeuedItemsCount < _flushThreashold; dequeuedItemsCount++)
            {
                T item;
                if (_buffer.TryDequeue(out item))
                {
                    itemsToFlush.Add(item);
                }
                else
                {
                    break;
                }
            }

            Interlocked.Add(ref _itemsCount, -dequeuedItemsCount);

            _flushAction(itemsToFlush);
        }
    }
}