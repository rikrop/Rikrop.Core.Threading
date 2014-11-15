using System.Threading.Tasks;
using Microsoft.Runtime.CompilerServices;

namespace Rikrop.Core.Threading
{
    public class ManualResetEventAsync
    {
        private readonly bool _allowToStopWaitBeforeAwaited;
        private readonly object _lock = new object();
        private TaskCompletionSource<bool> _taskCompletionSource;

        public ManualResetEventAsync()
            :this(true)
        {
            
        }

        public ManualResetEventAsync(bool allowToStopWaitBeforeAwaited)
        {
            _allowToStopWaitBeforeAwaited = allowToStopWaitBeforeAwaited;
        }

        public void StopAwait()
        {
            if (_taskCompletionSource == null && !_allowToStopWaitBeforeAwaited)
            {
                return;
            }
            var taskCompletionSource = EnsureInitialized();

            taskCompletionSource.TrySetResult(true);
        }

        public async Task WaitAsync()
        {
            var taskCompletionSource = EnsureInitialized();

            await taskCompletionSource.Task;

            _taskCompletionSource = null;
        }

        public TaskAwaiter GetAwaiter()
        {
            return WaitAsync().GetAwaiter();
        }

        private TaskCompletionSource<bool> EnsureInitialized()
        {
            var taskCompletionSource = _taskCompletionSource;

            if (taskCompletionSource == null)
            {
                lock (_lock)
                {
                    taskCompletionSource = _taskCompletionSource;

                    if (taskCompletionSource == null)
                    {
                        taskCompletionSource = new TaskCompletionSource<bool>();

                        _taskCompletionSource = taskCompletionSource;
                    }
                }
            }

            return taskCompletionSource;
        }
    }
}