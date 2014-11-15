using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rikrop.Core.Threading.Test
{
    [TestFixture]
    public class AsyncLockTest
    {
        [Test, Timeout(3000)]
        public async void AsyncLockShouldAllowOnlyOneThreadEnterToCodeBlock()
        {
            const int timeout = 100;

            var asyncLock = new AsyncLock();
            
            var taskCompletionSource1 = new TaskCompletionSource<DateTime>();
            var taskCompletionSource2 = new TaskCompletionSource<DateTime>();

            new Thread(o => TestMethod(timeout, asyncLock, (TaskCompletionSource<DateTime>) o)).Start(taskCompletionSource1);
            new Thread(o => TestMethod(timeout, asyncLock, (TaskCompletionSource<DateTime>) o)).Start(taskCompletionSource2);

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

            Assert.Greater(offset, TimeSpan.FromMilliseconds(timeout));
        }

        private async void TestMethod(int timeout, AsyncLock asyncLock, TaskCompletionSource<DateTime> taskCompletionSource)
        {
            using (await asyncLock)
            {
                Thread.Sleep(timeout);
                taskCompletionSource.SetResult(DateTime.Now);
            }
        }
    }
}