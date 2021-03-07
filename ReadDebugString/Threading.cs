using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ReadDebugString.Threading
{
    public class Dispatcher : IDisposable
    {
        private readonly Worker worker = new();

        public Task InvokeAsync(Action action) => Task.Run(() => Invoke(action));
        public Task<T> InvokeAsync<T>(Func<T> func) => Task.Run(() => Invoke(func));

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

    class Worker : IDisposable
    {
        private readonly Thread thread;
        private readonly BlockingCollection<IJob> queue;

        public Worker()
        {
            thread = new Thread(Run) { IsBackground = true };
            queue = new BlockingCollection<IJob>(1);

            thread.Start();
        }

        public void Add(IJob job) => queue.Add(job);

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

    class ActionJob : IJob, IDisposable
    {
        private readonly Action action;
        private readonly CultureInfo culture;
        private readonly CultureInfo uiCulture;
        private readonly ManualResetEvent completed = new(false);
        private ExceptionDispatchInfo? exception;

        public ActionJob(Action action)
        {
            this.action = action;
            culture = CultureInfo.CurrentCulture;
            uiCulture = CultureInfo.CurrentUICulture;
        }

        public void Run()
        {
            try
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = uiCulture;
                action();
            }
            catch (Exception e)
            {
                exception = ExceptionDispatchInfo.Capture(e);
            }
            completed.Set();
        }

        public void Result()
        {
            completed.WaitOne();
            if (exception is not null) exception.Throw();
        }

        public void Dispose()
        {
            DisposeImpl();
            GC.SuppressFinalize(this);
        }

        ~ActionJob() => DisposeImpl();
        private void DisposeImpl() => completed.Dispose();
    }

    class FuncJob<T> : IJob, IDisposable
    {
        private readonly Func<T> func;
        private readonly CultureInfo culture;
        private readonly CultureInfo uiCulture;
        private readonly ManualResetEvent completed = new(false);
        private T? result;
        private ExceptionDispatchInfo? exception;

        public FuncJob(Func<T> func)
        {
            this.func = func;
            culture = CultureInfo.CurrentCulture;
            uiCulture = CultureInfo.CurrentUICulture;
        }

        public void Run()
        {
            try
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = uiCulture;
                result = func();
            }
            catch (Exception e)
            {
                exception = ExceptionDispatchInfo.Capture(e);
            }
            completed.Set();
        }

        public T Result()
        {
            completed.WaitOne();
            if (exception is not null) exception.Throw();
            if (result is null) throw new InvalidOperationException();
            return result;
        }

        public void Dispose()
        {
            DisposeImpl();
            GC.SuppressFinalize(this);
        }

        ~FuncJob() => DisposeImpl();
        private void DisposeImpl() => completed.Dispose();
    }

    interface IJob
    {
        public void Run();
    }
}
