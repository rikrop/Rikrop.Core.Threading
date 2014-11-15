using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Rikrop.Core.Threading
{
    public class CountdownTaskScheduler : TaskScheduler
    {
        private readonly CountdownEventAsync _countdownEventAsync = new CountdownEventAsync(Environment.ProcessorCount*3);

        public int TrackedTasksCount
        {
            get { return _countdownEventAsync.CurrentCount; }
        }

        public int MinimalThresholdValue
        {
            get { return _countdownEventAsync.MinimalThresholdValue; }
        }


        public CountdownTaskScheduler()
        {
        }

        public CountdownTaskScheduler(int minimalThresholdValue)
        {
            _countdownEventAsync.MinimalThresholdValue = minimalThresholdValue;
        }

        public void TrackTaskExecution(Task task)
        {
            _countdownEventAsync.AddOnce();

            task.ContinueWith(CountdownSignal, TaskContinuationOptions.ExecuteSynchronously);
        }

        public Task WaitAsync()
        {
            return _countdownEventAsync.WaitAsync();
        }

        public TaskAwaiter GetAwaiter()
        {
            return WaitAsync().GetAwaiter();
        }

        protected override void QueueTask(Task task)
        {
            _countdownEventAsync.AddOnce();

            Task.Factory.StartNew(ExecuteTask, task, CancellationToken.None, task.CreationOptions, Default);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (taskWasPreviouslyQueued)
            {
                return false;
            }

            return TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return null;
        }

        private void CountdownSignal(Task obj)
        {
            _countdownEventAsync.Signal();
        }

        private async void ExecuteTask(object task)
        {
            TryExecuteTask((Task) task);

            var proxyTask = task as Task<Task>;
            if (proxyTask != null)
            {
                await proxyTask.Result;
            }

            _countdownEventAsync.Signal();
        }
    }
}