using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rikrop.Core.Threading.Test
{
    [TestFixture]
    public class CountdownEventAsyncTest
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            _countdownEventAsync = new CountdownEventAsync(5);
        }

        #endregion

        private CountdownEventAsync _countdownEventAsync;
        private bool _isStopped;

        private void StopAwaitInThread()
        {
            var timer = new System.Timers.Timer(50) {AutoReset = false};
            timer.Elapsed += delegate
                                 {
                                     _isStopped = true;
                                     _countdownEventAsync.Signal();
                                 };
            timer.Start();
        }

        [Test, Timeout(1000)]
        public async void CountdownEventAsyncShouldStopAwaitWhenLessOrEqualMinimalThresholdReached()
        {
            _countdownEventAsync.Add(4);

            StopAwaitInThread();

            await _countdownEventAsync;

            Assert.True(_isStopped);
        }

        [Test, Timeout(1000)]
        public async void CountdownEventAsyncShouldStopAwaitWhenZeroReached()
        {
            _countdownEventAsync.AddOnce();

            StopAwaitInThread();

            await _countdownEventAsync;

            Assert.True(_isStopped);
        }

        [Test, Timeout(1000)]
        public async void CountdownEventAsyncShouldStopAwaitAllAwaitersWhenZeroReached()
        {
            _countdownEventAsync = new CountdownEventAsync(0);
            _countdownEventAsync.AddOnce();
            var threadStarted = new TaskCompletionSource<bool>();
            var completionSource = new TaskCompletionSource<bool>();

            new Thread(o =>
                           {
                               threadStarted.SetResult(true);
                               _countdownEventAsync.WaitAsync().Wait();
                               completionSource.SetResult(true);
                           }).Start();

            await threadStarted.Task;

            StopAwaitInThread();

            await _countdownEventAsync;

            Assert.True(_isStopped);

            _isStopped = await completionSource.Task;

            Assert.True(_isStopped);
        }
    }
}