using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReadDebugString;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReadDebugStringTests
{
    [TestClass]
    public class DispatcherTest
    {
        [TestMethod]
        public void Invoke_Action_ReturnsAfterCompletion()
        {
            var ok = false;
            using var dispatcher = new Dispatcher();
            dispatcher.Invoke(() =>
            {
                Thread.Sleep(100);
                ok = true;
                return;
            });
            Assert.IsTrue(ok);
        }

        [TestMethod]
        public async Task InvokeAsync_Action_ReturnsAfterCompletion()
        {
            var ok = false;
            using var dispatcher = new Dispatcher();
            await dispatcher.InvokeAsync(() =>
            {
                Thread.Sleep(100);
                ok = true;
                return;
            });
            Assert.IsTrue(ok);
        }

        [TestMethod]
        public void Invoke_Func_ReturnsValue()
        {
            const int expected = 1;

            using var dispatcher = new Dispatcher();
            var actual = dispatcher.Invoke(() => expected);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task InvokeAsync_Func_ReturnsValue()
        {
            const int expected = 1;

            using var dispatcher = new Dispatcher();
            var actual = await dispatcher.InvokeAsync(() => expected);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Invoke_ThrowingAction_Throws()
        {
            var expected = new Exception("a");
            Exception? actual = null;

            using var dispatcher = new Dispatcher();
            try
            {
                dispatcher.Invoke(() => throw expected);
            }
            catch (Exception e)
            {
                actual = e;
            }
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task InvokeAsync_ThrowingAction_Throws()
        {
            var expected = new Exception("a");
            Exception? actual = null;

            using var dispatcher = new Dispatcher();
            try
            {
                await dispatcher.InvokeAsync(() => throw expected);
            }
            catch (Exception e)
            {
                actual = e;
            }
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Invoke_ThrowingFunc_Throws()
        {
            var expected = new Exception("a");
            Exception? actual = null;

            using var dispatcher = new Dispatcher();
            try
            {
                _ = dispatcher.Invoke<int>(() => throw expected);
            }
            catch (Exception e)
            {
                actual = e;
            }
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task InvokeAsync_ThrowingFunc_Throws()
        {
            var expected = new Exception("a");
            Exception? actual = null;

            using var dispatcher = new Dispatcher();
            try
            {
                _ = await dispatcher.InvokeAsync<int>(() => throw expected);
            }
            catch (Exception e)
            {
                actual = e;
            }
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Invoke_Func_RunsOutsideCallerThread()
        {
            using var dispatcher = new Dispatcher();
            var tid = dispatcher.Invoke(() => Thread.CurrentThread.ManagedThreadId);
            Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, tid);
        }

        [TestMethod]
        public void Invoke_CallTwice_RunsOnSameThread()
        {
            using var dispatcher = new Dispatcher();
            var tid1 = dispatcher.Invoke(() => Thread.CurrentThread.ManagedThreadId);
            var tid2 = dispatcher.Invoke(() => Thread.CurrentThread.ManagedThreadId);
            Assert.AreEqual(tid1, tid2);
        }

        [TestMethod]
        [Timeout(1000)]
        public void Invoke_CallFromDifferentThreads_NoDeadlock()
        {
            using var dispatcher = new Dispatcher();
            var thread1 = new Thread(() => dispatcher.Invoke(() => Thread.Sleep(100)));
            var thread2 = new Thread(() => dispatcher.Invoke(() => Thread.Sleep(100)));
            thread1.Start();
            thread2.Start();
            thread1.Join();
            thread2.Join();
        }

        [TestMethod]
        public void Invoke_CallAfterDispose_Throws()
        {
            var dispatcher = new Dispatcher();
            dispatcher.Dispose();
            _ = Assert.ThrowsException<ObjectDisposedException>(() => dispatcher.Invoke(() => 1));
        }

        [TestMethod]
        [Timeout(1000)]
        public void Invoke_CallFromWorker_Throws()
        {
            using var dispatcher = new Dispatcher();
            _ = Assert.ThrowsException<InvalidOperationException>(() => dispatcher.Invoke(() => dispatcher.Invoke(() => 1)));
        }
    }
}
