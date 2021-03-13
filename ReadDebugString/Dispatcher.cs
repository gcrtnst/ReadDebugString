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
        private readonly Worker worker = new();

        public async Task InvokeAsync(Action action)
        {
            using var job = new ActionJob(action);
            worker.Add(job);
            await job.ResultAsync();
        }

        public async Task<T> InvokeAsync<T>(Func<T> func)
        {
            using var job = new FuncJob<T>(func);
            worker.Add(job);
            return await job.ResultAsync();
        }

        public void Invoke(Action action)
        {
            using var job = new ActionJob(action);
            worker.Add(job);
            job.Result();
        }

        public T Invoke<T>(Func<T> func)
        {
            using var job = new FuncJob<T>(func);
            worker.Add(job);
            return job.Result();
        }

        public void Dispose()
        {
            DisposeImpl();
            GC.SuppressFinalize(this);
        }

        ~Dispatcher() => DisposeImpl();
        private void DisposeImpl() => worker.Dispose();
    }

    internal class Worker : IDisposable
    {
        private readonly Thread thread;
        private readonly BlockingCollection<Job> queue;

        public Worker()
        {
            thread = new Thread(Run) { IsBackground = true };
            queue = new BlockingCollection<Job>(1);

            thread.Start();
        }

        public void Add(Job job)
        {
            if (Thread.CurrentThread.ManagedThreadId == thread.ManagedThreadId) throw new InvalidOperationException();
            queue.Add(job);
        }

        private void Run()
        {
            foreach (var job in queue.GetConsumingEnumerable()) job.Run();
        }

        public void Dispose()
        {
            DisposeImpl();
            GC.SuppressFinalize(this);
        }

        ~Worker() => DisposeImpl();

        private void DisposeImpl()
        {
            queue.CompleteAdding();
            thread.Join();
            queue.Dispose();
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

        public void Dispose()
        {
            DisposeImpl();
            GC.SuppressFinalize(this);
        }

        ~Job() => DisposeImpl();
        private void DisposeImpl() => completed.Dispose();
    }
}
