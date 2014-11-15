using System;
using System.Diagnostics.Contracts;

namespace Rikrop.Core.Threading
{
    public class ConcurrentExecutor
    {
        private const int DefaultConcurrencyLevel = 10000;

        private readonly object[] _locks;

        public ConcurrentExecutor()
            : this(DefaultConcurrencyLevel)
        {
            
        }

        public ConcurrentExecutor(int concurrencyLevel)
        {
            _locks = new object[concurrencyLevel];
            for (int i = 0; i < concurrencyLevel; i++)
            {
                _locks[i] = new object();
            }
        }

        public void Execute<TKey>(TKey syncKey, Action action)
        {
            Contract.Requires<ArgumentNullException>(!ReferenceEquals(syncKey, null));
            Contract.Requires<ArgumentNullException>(action != null);

            var lockNum = GetLockNumber(syncKey.GetHashCode());
            lock(_locks[lockNum])
            {
                action();
            }
        }

        public TResult Execute<TKey, TResult>(TKey syncKey, Func<TResult> func)
        {
            Contract.Requires<ArgumentNullException>(!ReferenceEquals(syncKey, null));
            Contract.Requires<ArgumentNullException>(func != null);

            var lockNum = GetLockNumber(syncKey.GetHashCode());
            lock(_locks[lockNum])
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