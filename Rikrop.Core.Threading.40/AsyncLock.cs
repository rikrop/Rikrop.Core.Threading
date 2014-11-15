using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Runtime.CompilerServices;

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
            await Task.Factory.StartNew(() => _semaphoreSlim.Wait());

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