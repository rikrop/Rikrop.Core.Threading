using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rikrop.Core.Threading
{
    public sealed class AsyncOneManyLock
    {
        private readonly Task _noContentionAccessGranter = TaskEx.FromResult<Object>(null);
        private readonly Queue<TaskCompletionSource<Object>> _waitingWritersQueue = new Queue<TaskCompletionSource<Object>>();
        private TaskCompletionSource<Object> _waitingReadersSignal = new TaskCompletionSource<Object>();
        private SpinLock _locker = new SpinLock(true);
        private Int32 _waitingReadersCount;
        private Int32 _state;

        private Boolean IsFree
        {
            get { return _state == 0; }
        }

        private Boolean IsOwnedByWriter
        {
            get { return _state == -1; }
        }

        private Boolean IsOwnedByReaders
        {
            get { return _state > 0; }
        }

        public Task WaitExclusive()
        {
            Lock();

            var accessGranter = _noContentionAccessGranter;

            if (IsFree)
            {
                MakeWriter();
            }
            else
            {
                var tcs = new TaskCompletionSource<Object>();
                _waitingWritersQueue.Enqueue(tcs);
                accessGranter = tcs.Task;
            }

            Unlock();

            return accessGranter;
        }

        public Task WaitShared()
        {
            Lock();

            var accessGranter = _noContentionAccessGranter;

            if (IsFree || (IsOwnedByReaders && _waitingWritersQueue.Count == 0))
            {
                AddReaders(1);
            }
            else
            {
                _waitingReadersCount++;
                accessGranter = _waitingReadersSignal.Task.ContinueWith(t => t.Result);
            }

            Unlock();

            return accessGranter;
        }

        public void Release()
        {
            TaskCompletionSource<Object> accessGranter = null;

            Lock();

            if (IsOwnedByWriter)
            {
                MakeFree();
            }
            else
            {
                SubtractReader();
            }

            if (IsFree)
            {
                if (_waitingWritersQueue.Count > 0)
                {
                    MakeWriter();
                    accessGranter = _waitingWritersQueue.Dequeue();
                }
                else if (_waitingReadersCount > 0)
                {
                    AddReaders(_waitingReadersCount);
                    _waitingReadersCount = 0;
                    accessGranter = _waitingReadersSignal;
                    _waitingReadersSignal = new TaskCompletionSource<Object>();
                }
            }

            Unlock();

            if (accessGranter != null)
            {
                accessGranter.SetResult(null);
            }
        }

        private void Lock()
        {
            var taken = false;
            _locker.Enter(ref taken);
        }

        private void Unlock()
        {
            _locker.Exit();
        }

        private void AddReaders(int count)
        {
            _state += count;
        }

        private void SubtractReader()
        {
            --_state;
        }

        private void MakeWriter()
        {
            _state = -1;
        }

        private void MakeFree()
        {
            _state = 0;
        }
    }
}