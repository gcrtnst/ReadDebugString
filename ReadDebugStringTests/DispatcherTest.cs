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
    }
}
