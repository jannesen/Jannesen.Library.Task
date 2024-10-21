using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jannesen.Library.Tasks;

namespace Jannesen.Library.Tasks.UnitTest
{
    [TestClass]
    public class TaskLockTest
    {
        sealed class ParalleLockTest: IDisposable
        {
            private readonly    TaskLock        _taskLock;
            private readonly    object          _lockObject;
            private             int             _count;
            private             int             _n;

            public                              ParalleLockTest()
            {
                _taskLock = new TaskLock();
                _lockObject = new object();
                _count = 0;
                _n = 0;
            }
            public              void            Dispose()
            {
                _taskLock.Dispose();
            }

            public      async   Task            Run()
            {
                var tasks = new Task[64];

                for (var i = 0 ; i < tasks.Length ; ++i) {
                    tasks[i] = _run(10000);
                }

                await Task.WhenAll(tasks);

                Assert.AreEqual(0, _n);
                Assert.AreEqual(10000 * tasks.Length, _count);
            }
            public      async   Task            _run(int n)
            {
                for (var i = 0 ; i < n ; ++i) {
                    await _test();
                }
            }
            private     async   Task            _test()
            {
                using (await _taskLock.Enter()) {
                    lock(_lockObject) {
                        _count++;
                        if (_n++ != 0) {
                            Assert.Fail();
                        }
                    }

                    await Task.Yield();

                    lock(_lockObject) {
                        _n--;
                    }
                }
            }
        }

        [TestMethod]
        public  async   Task        LockedTimeout()
        {
            using (var x = new TaskLock()) {
                using (await x.Enter()) {
                    var start = DateTime.UtcNow;

                    try {
                        using (await x.Enter(2000)) {
                            Assert.Fail();
                        }
                    }
                    catch(TimeoutException) {
                        var time = (DateTime.UtcNow - start).Ticks;

                        Assert.IsTrue((TimeSpan.TicksPerMillisecond * 1900) <= time && time <= (TimeSpan.TicksPerMillisecond * 2100));
                    }
                }
            }
        }

        [TestMethod]
        public  async   Task        LockedCancellation()
        {
            using (var x = new TaskLock()) {
                using (await x.Enter()) {
                    var start = DateTime.UtcNow;

                    try {
                        using (var cts = new CancellationTokenSource(2000)) {
                            using (await x.Enter(cts.Token)) {
                                Assert.Fail();
                            }
                        }
                    }
                    catch(OperationCanceledException) {
                        var time = (DateTime.UtcNow - start).Ticks;
                        Assert.IsTrue((TimeSpan.TicksPerMillisecond * 1900) <= time && time <= (TimeSpan.TicksPerMillisecond * 2100));
                    }
                }
            }
        }

        [TestMethod]
        public  async   Task        ParalleTest()
        {
            using (var x = new ParalleLockTest()) {
                await x.Run();
            }
        }
    }
}
