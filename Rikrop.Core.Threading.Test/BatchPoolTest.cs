using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rikrop.Core.Threading.Test
{
    [TestFixture]
    public class BatchPoolTest
    {
        [Test]
        public void BatchPoolShouldFlushAddedItemsWhenThresholdReached()
        {
            const int itemCount = 5;
            var items = Enumerable.Range(0, itemCount);

            var taskCancellationSource = new TaskCompletionSource<IReadOnlyCollection<int>>();
            var batchPool = new BatchPool<int>(taskCancellationSource.SetResult, itemCount);
            foreach (var item in items)
            {
                batchPool.Add(item);
            }

            var isSuccess = taskCancellationSource.Task.Wait(1000);

            Assert.True(isSuccess, "Не дождались результата");
            Assert.AreEqual(items, taskCancellationSource.Task.Result);
        }

        [Test]
        public void BatchPoolShouldStopWaitWhenAllFlushOperationsIsEnded()
        {
            const int itemCount = 5;
            const int expectedFlushCount = 2;

            int flushCount = 0;
            var items = Enumerable.Range(0, itemCount * expectedFlushCount);
            var batchPool = new BatchPool<int>(o=> Interlocked.Increment(ref flushCount), itemCount);
            foreach (var item in items)
            {
                batchPool.Add(item);
            }

            var isSuccess = batchPool.WaitAsync().Wait(1000);

            Assert.True(isSuccess, "Не дождались результата");
            Assert.AreEqual(expectedFlushCount, flushCount);
        }

        [Test]
        public void BatchPoolShouldImmediatelyFlushAddedItemsWhenForced()
        {
            const int itemCount = 5;
            var items = Enumerable.Range(0, itemCount);
            var flushedItems = new List<int>();

            var batchPool = new BatchPool<int>(flushedItems.AddRange, itemCount - 1, TimeSpan.FromSeconds(5));
            foreach (var item in items)
            {
                batchPool.Add(item);
            }

            batchPool.ForceFlush();

            var isSuccess = batchPool.WaitAsync().Wait(1000);

            Assert.True(isSuccess, "Не дождались результата");
            Assert.AreEqual(items, flushedItems);
        }

        [Test]
        public void BatchPoolShouldFlushAddedItemsAfterTimeout()
        {
            const int itemCount = 5;
            var items = Enumerable.Range(0, itemCount);
            var flushedItems = new List<int>();

            var batchPool = new BatchPool<int>(flushedItems.AddRange, itemCount - 1, TimeSpan.FromSeconds(0.2));
            foreach (var item in items)
            {
                batchPool.Add(item);
            }

            var isSuccess = batchPool.WaitAsync().Wait(1000);

            Assert.True(isSuccess, "Не дождались результата");
            Assert.AreEqual(items, flushedItems);
        }
    }
}