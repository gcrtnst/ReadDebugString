using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReadDebugString
{
    class DebugStringStream : IAsyncEnumerable<string>, IAsyncEnumerator<string>, IEnumerable<string>, IEnumerator<string>
    {
        public readonly int processId;

        private readonly Debugger debugger = new();
        private SafeProcessHandle? process;
        private CancellationToken cancellationToken;

        public string Current { get; private set; } = "";
        object IEnumerator.Current => Current;

        public DebugStringStream(string? applicationName, string? commandLine)
        {
            var processInformation = debugger.CreateProcess(applicationName, commandLine, Win32.Constants.ProcessCreationFlags.DebugOnlyThisProcess);
            processId = processInformation.ProcessId;
            process = processInformation.Process;
        }

        public DebugStringStream(int processId)
        {
            debugger.DebugActiveProcess(processId);
            this.processId = processId;
        }

        public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            this.cancellationToken = cancellationToken;
            return this;
        }

        public IEnumerator<string> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public bool MoveNext()
        {
            while (true)
            {
                var continueStatus = Win32.Constants.DbgExceptionNotHandled;
                var debugEvent = debugger.WaitForDebugEvent(cancellationToken);
                try
                {
                    switch (debugEvent)
                    {
                        case Win32.CreateProcessDebugEvent createProcessDebugEvent when process is null:
                            process = createProcessDebugEvent.Process;
                            break;
                        case Win32.ExitProcessDebugEvent exitProcessDebugEvent:
                            return false;
                        case Win32.OutputDebugStringEvent outputDebugStringEvent:
                            continueStatus = Win32.Constants.DbgContinue;
                            if (process is null) throw new InvalidOperationException();
                            Current = outputDebugStringEvent.ReadDebugStringData(process);
                            return true;
                    }
                }
                finally
                {
                    debugger.ContinueDebugEvent(debugEvent, continueStatus);
                }
            }
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            while (true)
            {
                var continueStatus = Win32.Constants.DbgExceptionNotHandled;
                var debugEvent = await debugger.WaitForDebugEventAsync(cancellationToken);
                try
                {
                    switch (debugEvent)
                    {
                        case Win32.CreateProcessDebugEvent createProcessDebugEvent when process is null:
                            process = createProcessDebugEvent.Process;
                            break;
                        case Win32.ExitProcessDebugEvent exitProcessDebugEvent:
                            return false;
                        case Win32.OutputDebugStringEvent outputDebugStringEvent:
                            continueStatus = Win32.Constants.DbgContinue;
                            if (process is null) throw new InvalidOperationException();
                            Current = outputDebugStringEvent.ReadDebugStringData(process);
                            return true;
                    }
                }
                finally
                {
                    debugger.ContinueDebugEvent(debugEvent, continueStatus);
                }
            }
        }

        public void Reset() => throw new NotSupportedException();

        public async ValueTask DisposeAsync() => await Task.Run(Dispose);

        public void Dispose()
        {
            DisposeImpl();
            GC.SuppressFinalize(this);
        }

        ~DebugStringStream() => DisposeImpl();

        private void DisposeImpl()
        {
            debugger.DebugActiveProcessStop(processId);
            debugger.Dispose();
        }
    }
}
