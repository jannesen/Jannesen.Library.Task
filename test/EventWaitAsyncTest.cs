using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jannesen.Library.Tasks;

namespace Jannesen.Library.Tasks.UnitTest
{
    [TestClass]
    public class EventWaitAsyncTest
    {
        [TestMethod]
        public  async   Task        LockedTimeout()
        {
            var start = DateTime.UtcNow;

            try { 
                var ewa = new EventWaitAsync();
                ewa.Signal();
                Assert.IsTrue(await ewa.WaitAsync(2000));
                Assert.IsFalse(await ewa.WaitAsync(2000));
            }
            catch(TimeoutException) {
                var time = (DateTime.UtcNow - start).Ticks;

                Assert.IsTrue((TimeSpan.TicksPerMillisecond * 1900) <= time && time <= (TimeSpan.TicksPerMillisecond * 2100));
            }
        }

        [TestMethod]
        public  async   Task        LockedCancellation()
        {
            var start = DateTime.UtcNow;

            try { 
                var ewa = new EventWaitAsync();
    
                using (var cts = new CancellationTokenSource(2000)) { 
                    ewa.Signal();
                    await ewa.WaitAsync(cts.Token);
                    await ewa.WaitAsync(cts.Token);
                }

                Assert.Fail();
            }
            catch(OperationCanceledException) {
                var time = (DateTime.UtcNow - start).Ticks;
                Assert.IsTrue((TimeSpan.TicksPerMillisecond * 1900) <= time && time <= (TimeSpan.TicksPerMillisecond * 2100));
            }
        }

        [TestMethod]
        public  async   Task        LoopTest()
        {
            Func<EventWaitAsync,EventWaitAsync, Task> loop = async (EventWaitAsync ewa1, EventWaitAsync ewa2) => {
                for (int i = 0 ; i < 10 ; ++i) {
                    Assert.IsTrue(await ewa1.WaitAsync(1000));
                    await Task.Delay(100);
                    ewa2.Signal();
                }
            };

            Func<EventWaitAsync,Task> starter = async (EventWaitAsync ewa) => {
                await Task.Delay(100);
                ewa.Signal();
            };

            var start = DateTime.UtcNow;
            var ewa1 = new EventWaitAsync();
            var ewa2 = new EventWaitAsync();

            await Task.WhenAll(loop(ewa1, ewa2), loop(ewa2, ewa1), starter(ewa1));

            var time = (DateTime.UtcNow - start).Ticks;
            Assert.IsTrue((TimeSpan.TicksPerMillisecond * 2200) <= time && time <= (TimeSpan.TicksPerMillisecond * 2400));
        }
    }
}
