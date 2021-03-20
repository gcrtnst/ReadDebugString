using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ReadDebugString
{
    public class Dispatcher : IDisposable
    {
        private readonly Manager manager = new();

        public async Task InvokeAsync(Action action)
        {
            using var job = new ActionJob(action);
            manager.Add(job);
            await job.ResultAsync();
        }

        public async Task<T> InvokeAsync<T>(Func<T> func)
        {
            using var job = new FuncJob<T>(func);
            manager.Add(job);
            return await job.ResultAsync();
        }

        public void Invoke(Action action)
        {
            using var job = new ActionJob(action);
            manager.Add(job);
            job.Result();
        }

        public T Invoke<T>(Func<T> func)
        {
            using var job = new FuncJob<T>(func);
            manager.Add(job);
            return job.Result();
        }

#pragma warning disable CA1816
        public void Dispose() => manager.Dispose();
#pragma warning restore CA1816
    }

    internal class Manager : IDisposable
    {
        private readonly Worker worker = new();

        public void Add(Job job)
        {
            if (Thread.CurrentThread.ManagedThreadId == worker.thread.ManagedThreadId) throw new InvalidOperationException();
            try
            {
                worker.queue.Add(job);
            }
            catch (InvalidOperationException)
            {
                throw new ObjectDisposedException(nameof(Manager));
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
            worker.queue.Dispose();
        }

        ~Manager() => Stop();

        private void Stop()
        {
            try
            {
                worker.queue.CompleteAdding();
            }
            catch (ObjectDisposedException) { }
            worker.thread.Join();
        }
    }

    internal class Worker
    {
        public readonly Thread thread;
        public readonly BlockingCollection<Job> queue;

        public Worker()
        {
            thread = new Thread(Run) { IsBackground = true };
            queue = new BlockingCollection<Job>(1);

            thread.Start();
        }

        private void Run()
        {
            Thread.BeginThreadAffinity();
            try
            {
                foreach (var job in queue.GetConsumingEnumerable()) job.Run();
            }
            finally
            {
                Thread.EndThreadAffinity();
            }
        }
    }

    internal class ActionJob : Job
    {
        private readonly Action action;

        public ActionJob(Action action) : base()
        {
            this.action = action;
        }

        protected override void RunImpl() => action();

        public async Task ResultAsync()
        {
            await WaitAsync();
            ThrowIfFailed();
        }

        public void Result()
        {
            Wait();
            ThrowIfFailed();
        }
    }

    internal class FuncJob<T> : Job
    {
        private readonly Func<T> func;
        private T? result;

        public FuncJob(Func<T> func) : base()
        {
            this.func = func;
        }

        protected override void RunImpl() => result = func();

        public async Task<T> ResultAsync()
        {
            await WaitAsync();
            ThrowIfFailed();
            if (result is null) throw new InvalidOperationException();
            return result;
        }

        public T Result()
        {
            Wait();
            ThrowIfFailed();
            if (result is null) throw new InvalidOperationException();
            return result;
        }
    }

    internal abstract class Job : IDisposable
    {
        private readonly CultureInfo culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo uiCulture = CultureInfo.CurrentUICulture;
        private readonly ManualResetEvent completed = new(false);
        private ExceptionDispatchInfo? exception;

        public void Run()
        {
            var culture = CultureInfo.CurrentCulture;
            var uiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = this.culture;
                CultureInfo.CurrentUICulture = this.uiCulture;
                try
                {
                    RunImpl();
                }
                catch (Exception e)
                {
                    exception = ExceptionDispatchInfo.Capture(e);
                }
                _ = completed.Set();
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = uiCulture;
            }
        }

        protected abstract void RunImpl();
        protected void Wait() => completed.WaitOne();
        protected void ThrowIfFailed() => exception?.Throw();

        protected Task WaitAsync()
        {
            var tcs = new TaskCompletionSource();
            var rwh = ThreadPool.RegisterWaitForSingleObject(completed, delegate { tcs.SetResult(); }, null, -1, true);
            var task = tcs.Task;
            return task.ContinueWith((t) => _ = rwh.Unregister(completed));
        }

        public void Dispose() => completed.Dispose();
    }
}
