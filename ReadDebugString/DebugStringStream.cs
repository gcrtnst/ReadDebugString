using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReadDebugString
{
    internal class DebugStringStream : IAsyncEnumerable<string>, IAsyncEnumerator<string>, IEnumerable<string>, IEnumerator<string>
    {
        public readonly uint processId;

        private readonly Debugger debugger = new();
        private SafeProcessHandle? process;
        private CancellationToken token;

        public string Current { get; private set; } = "";
        object IEnumerator.Current => Current;

        public DebugStringStream(string? applicationName, string? commandLine)
        {
            var processInformation = debugger.CreateProcess(applicationName, commandLine, Win32.Constants.ProcessCreationFlags.DebugOnlyThisProcess);
            debugger.DebugSetProcessKillOnExit(false);
            processId = processInformation.ProcessId;
            process = processInformation.Process;
        }

        public DebugStringStream(uint processId)
        {
            debugger.DebugActiveProcess(processId);
            debugger.DebugSetProcessKillOnExit(false);
            this.processId = processId;
        }

        public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken token = default)
        {
            this.token = token;
            return this;
        }

        public IEnumerator<string> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public bool MoveNext()
        {
            while (true)
            {
                var debugEvent = debugger.WaitForDebugEvent(token);
                var ok = HandleDebugEvent(debugEvent);
                if (ok.HasValue) return ok.Value;
            }
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            while (true)
            {
                var debugEvent = await debugger.WaitForDebugEventAsync(token);
                var ok = HandleDebugEvent(debugEvent);
                if (ok.HasValue) return ok.Value;
            }
        }

        private bool? HandleDebugEvent(Win32.DebugEvent debugEvent)
        {
            var continueStatus = Win32.Constants.DebugContinueStatus.DbgExceptionNotHandled;
            try
            {
                switch (debugEvent)
                {
                    case Win32.CreateProcessDebugEvent createProcessDebugEvent:
                        createProcessDebugEvent.File.Close();
                        createProcessDebugEvent.Thread.Close();
                        if (process is null) process = createProcessDebugEvent.Process;
                        break;
                    case Win32.CreateThreadDebugEvent createThreadDebugEvent:
                        createThreadDebugEvent.Thread.Close();
                        break;
                    case Win32.ExitProcessDebugEvent exitProcessDebugEvent:
                        return false;
                    case Win32.LoadDllDebugEvent loadDllDebugEvent:
                        loadDllDebugEvent.File.Close();
                        break;
                    case Win32.OutputDebugStringEvent outputDebugStringEvent:
                        continueStatus = Win32.Constants.DebugContinueStatus.DbgContinue;
                        if (process is null) throw new InvalidOperationException();
                        Current = outputDebugStringEvent.ReadDebugStringData(process);
                        return true;
                }
                return null;
            }
            finally
            {
                debugger.ContinueDebugEvent(debugEvent, continueStatus);
            }
        }

        public void Reset() => throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask();
        }

        public void Dispose()
        {
            process?.Dispose();
            debugger.Dispose();
        }
    }
}
