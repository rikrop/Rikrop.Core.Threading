using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Rikrop.Core.Threading
{
    public class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim _semaphoreSlim;

        public AsyncLock() :
            this(concurrencyLevel: 1)
        {
        }

        public AsyncLock(int concurrencyLevel)
        {
            _semaphoreSlim = new SemaphoreSlim(concurrencyLevel);
        }

        public async Task<AsyncLock> LockAsync()
        {
            await _semaphoreSlim.WaitAsync();

            return this;
        }

        public TaskAwaiter<AsyncLock> GetAwaiter()
        {
            return LockAsync().GetAwaiter();
        }

        void IDisposable.Dispose()
        {
            _semaphoreSlim.Release();
        }
    }
}