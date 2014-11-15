using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace Rikrop.Core.Threading
{
    public class ConcurrentExecutorSlim
    {
        private const int DefaultConcurrencyLevel = 10000;

        private readonly AsyncLock[] _locks;

        public ConcurrentExecutorSlim()
            : this(DefaultConcurrencyLevel)
        {
            
        }

        public ConcurrentExecutorSlim(int concurrencyLevel)
        {
            _locks = new AsyncLock[concurrencyLevel];
            for (int i = 0; i < concurrencyLevel; i++)
            {
                _locks[i] = new AsyncLock();
            }
        }

        public async Task Execute<TKey>(TKey syncKey, Action action)
        {
            Contract.Requires<ArgumentNullException>(!ReferenceEquals(syncKey, null));
            Contract.Requires<ArgumentNullException>(action != null);

            var lockNum = GetLockNumber(syncKey.GetHashCode());
            using (await _locks[lockNum])
            {
                action();
            }
        }

        public async Task<TResult> Execute<TKey, TResult>(TKey syncKey, Func<TResult> func)
        {
            Contract.Requires<ArgumentNullException>(!ReferenceEquals(syncKey, null));
            Contract.Requires<ArgumentNullException>(func != null);

            var lockNum = GetLockNumber(syncKey.GetHashCode());
            using (await _locks[lockNum])
            {
                return func();
            }
        }

        private int GetLockNumber(int hashCode)
        {
            return (hashCode & 0x7fffffff) % _locks.Length;
        }
    }
}