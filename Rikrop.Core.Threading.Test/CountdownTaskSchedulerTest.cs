using System.Threading.Tasks;
using NUnit.Framework;

namespace Rikrop.Core.Threading.Test
{
    [TestFixture]
    public class CountdownTaskSchedulerTest
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            _countdownTaskScheduler = new CountdownTaskScheduler(0);
        }

        #endregion

        private CountdownTaskScheduler _countdownTaskScheduler;
        private bool _isStopped;

        [Test, Timeout(3000)]
        public async void CountdownTaskSchedulerShouldTrackExternalTaskExecution()
        {
            var task = new Task(() => _isStopped = true, TaskCreationOptions.LongRunning);
            _countdownTaskScheduler.TrackTaskExecution(task);
            
            task.Start(TaskScheduler.Default);

            await _countdownTaskScheduler.WaitAsync();

            Assert.True(_isStopped);
        }

        [Test, Timeout(3000)]
        public async void CountdownTaskSchedulerShouldTrackInternalTaskExecution()
        {
            var task = new Task(() => _isStopped = true, TaskCreationOptions.LongRunning);

            task.Start(_countdownTaskScheduler);

            await _countdownTaskScheduler.WaitAsync();

            Assert.True(_isStopped);
        }

        [Test, Timeout(3000)]
        public async void CountdownTaskSchedulerShouldStopAwaitWhenAllTrackableTasksCompleted()
        {
            var task = new Task(() => Task.Factory.StartNew(()=> _isStopped = true, TaskCreationOptions.LongRunning), TaskCreationOptions.LongRunning);

            task.Start(_countdownTaskScheduler);

            await _countdownTaskScheduler.WaitAsync();

            Assert.True(_isStopped);
        }
    }
}