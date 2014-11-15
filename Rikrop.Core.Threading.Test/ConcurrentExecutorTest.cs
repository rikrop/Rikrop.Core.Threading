using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rikrop.Core.Threading.Test
{
    [TestFixture]
    public class ConcurrentExecutorTest
    {
        private const int Timeout1 = 100;
        private int _timeout2;

        private async Task<TimeSpan> MainTest(Action<int, ConcurrentExecutor, TaskCompletionSource<DateTime>> action)
        {
            var concurrentExecutor = new ConcurrentExecutor();

            var taskCompletionSource1 = new TaskCompletionSource<DateTime>();
            var taskCompletionSource2 = new TaskCompletionSource<DateTime>();

            new Thread(o => action(Timeout1, concurrentExecutor, (TaskCompletionSource<DateTime>) o)).Start(taskCompletionSource1);
            new Thread(o => action(_timeout2, concurrentExecutor, (TaskCompletionSource<DateTime>)o)).Start(taskCompletionSource2);

            var dt1 = await taskCompletionSource1.Task;
            var dt2 = await taskCompletionSource2.Task;

            TimeSpan offset;
            if (dt1 > dt2)
            {
                offset = dt1 - dt2;
            }
            else
            {
                offset = dt2 - dt1;
            }

            return offset;
        }

        private void TestActionExecute(int timeout, ConcurrentExecutor concurrentExecutor, TaskCompletionSource<DateTime> taskCompletionSource)
        {
            concurrentExecutor.Execute(timeout, () =>
                                                    {
                                                        Thread.Sleep(timeout);
                                                        taskCompletionSource.SetResult(DateTime.Now);
                                                    });
        }

        private void TestFuncExecute(int timeout, ConcurrentExecutor concurrentExecutor, TaskCompletionSource<DateTime> taskCompletionSource)
        {
            var dt = concurrentExecutor.Execute(timeout, () =>
                                                                 {
                                                                     Thread.Sleep(timeout);
                                                                     return DateTime.Now;
                                                                 });

            taskCompletionSource.SetResult(dt);
        }

        [Test, Timeout(3000)]
        public async void ConcurrentExecutorShouldAllowManyThreadsEnterToActionCodeBlock()
        {
            _timeout2 = 101;

            var offset = await MainTest(TestActionExecute);

            Assert.Less(offset, TimeSpan.FromMilliseconds(Timeout1));
        }

        [Test, Timeout(3000)]
        public async void ConcurrentExecutorShouldAllowManyThreadsEnterToFuncCodeBlock()
        {
            _timeout2 = 101;

            var offset = await MainTest(TestFuncExecute);

            Assert.Less(offset, TimeSpan.FromMilliseconds(Timeout1));
        }

        [Test, Timeout(3000)]
        public async void ConcurrentExecutorShouldAllowOnlyOneThreadEnterToActionCodeBlock()
        {
            _timeout2 = Timeout1;

            var offset = await MainTest(TestActionExecute);

            Assert.Greater(offset, TimeSpan.FromMilliseconds(Timeout1));
        }

        [Test, Timeout(3000)]
        public async void ConcurrentExecutorShouldAllowOnlyOneThreadEnterToFuncCodeBlock()
        {
            _timeout2 = Timeout1;

            var offset = await MainTest(TestFuncExecute);

            Assert.Greater(offset, TimeSpan.FromMilliseconds(Timeout1));
        }
    }
}