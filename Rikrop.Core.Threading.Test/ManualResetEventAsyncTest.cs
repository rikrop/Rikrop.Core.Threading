using System.Threading;
using NUnit.Framework;

namespace Rikrop.Core.Threading.Test
{
    [TestFixture]
    public class ManualResetEventAsyncTest
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            _resetEventAsync = new ManualResetEventAsync();
        }

        #endregion

        private ManualResetEventAsync _resetEventAsync;
        private bool _isStopped;

        private void StopAwaitInThread()
        {
            var timer = new System.Timers.Timer(50) {AutoReset = false};
            timer.Elapsed += delegate
                                 {
                                     _isStopped = true;
                                     _resetEventAsync.StopAwait();
                                 };
            timer.Start();
        }

        [Test, Timeout(3000)]
        public async void ManualResetEventAsyncShouldNotAwaitWhenStopAwaitCalledBefore()
        {
            _resetEventAsync.StopAwait();

            await _resetEventAsync;
        }

        [Test, Timeout(3000)]
        public void ManualResetEventAsyncShouldAwaitWhenStopAwaitCalledBefore()
        {
            var resetEventAsync = new ManualResetEventAsync(false);
            resetEventAsync.StopAwait();

            bool result = resetEventAsync.WaitAsync().Wait(50);
            Assert.False(result);
        }

        [Test, Timeout(3000)]
        public async void ManualResetEventAsyncShouldStartNewAwaitAfterStop()
        {
            StopAwaitInThread();

            await _resetEventAsync;

            Assert.True(_isStopped, "StopAwait was not called");

            _isStopped = false;
            
            StopAwaitInThread();

            await _resetEventAsync;

            Assert.True(_isStopped, "StopAwait was not called");
        }

        [Test, Timeout(3000)]
        public async void ManualResetEventAsyncShouldStopAwaitOnDemand()
        {
            StopAwaitInThread();

            await _resetEventAsync;

            Assert.True(_isStopped, "StopAwait was not called");
        }
    }
}