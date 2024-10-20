using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jannesen.Library.Tasks;

namespace Jannesen.Library.Tasks.UnitTest
{
    [TestClass]
    public class MessageQueueTest
    {
        [TestMethod]
        public  async   Task        SimpleTest()
        {
            var q = new MessageQueue<string>(10);
            q.Send("1");
            Assert.AreEqual(await q.Receive(CancellationToken.None), "1");
        }
        [TestMethod]
        public  async   Task        CancelTest()
        {
            var q = new MessageQueue<string>(10);
            q.Send("1");
            Assert.AreEqual(await q.Receive(CancellationToken.None), "1");

            using (var ct = new CancellationTokenSource(10)) {
                try {
                    await q.Receive(ct.Token);
                    Assert.Fail("failed.");
                }
                catch(TaskCanceledException) {
                }
            }
        }

        [TestMethod]
        public  async   Task        QueueTest1()
        {
            var q = new MessageQueue<string>(100);

            await Task.WhenAll(_queueTest1_Send(q),
                               _queueTest1_Recieve(q));
        }
        public  async   Task        _queueTest1_Send(MessageQueue<string> q)
        {
            for (int i = 1 ; i < 1000 ; ++i) {
                q.Send(i.ToString());
                if ((i % 37) == 0) {
                    await Task.Delay(10);
                }
            }

            q.Close();
        }
        public  async   Task        _queueTest1_Recieve(MessageQueue<string> q)
        {
            int     i = 1;
            string  m;

            while ((m = await q.Receive(CancellationToken.None)) != null) {
                Assert.AreEqual(m, i.ToString());
                ++i;
                if ((i % 41) == 0) {
                    await Task.Delay(10);
                }
            }

            Assert.AreEqual(i, 1000);
        }

        [TestMethod]
        public  async   Task        QueueTest2()
        {
            var l = new byte[1000];
            var q = new MessageQueue<string>(100);

            await Task.WhenAll(_queueTest2_Send(l, q),
                               _queueTest2_Recieve(l, q, 1),
                               _queueTest2_Recieve(l, q, 2));
        }
        public  async   Task        _queueTest2_Send(byte[] l, MessageQueue<string> q)
        {
            for (int i = 1 ; i < l.Length ; ++i) {
                lock(l) {
                    Assert.AreEqual(l[i], 0);
                    l[i] = 1;
                }
                q.Send(i.ToString());
                if ((i % 37) == 0) {
                    await Task.Delay(10);
                }
            }

            q.Close();
        }
        public  async   Task        _queueTest2_Recieve(byte[] l, MessageQueue<string> q, byte n)
        {
            string  m;

            while ((m = await q.Receive(CancellationToken.None)) != null) {
                var i = Convert.ToInt32(m);

                lock(l) {
                    Assert.AreEqual(l[i], 1);
                    l[i] = n;
                }

                if ((i % 41) == 0) {
                    await Task.Delay(10);
                }
            }
        }
    }
}
