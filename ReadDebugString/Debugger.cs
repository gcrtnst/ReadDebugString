using ReadDebugString.Threading;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace ReadDebugString
{
    class Debugger : IDisposable
    {
        private readonly Dispatcher dispatcher = new();

        public Win32.ProcessInformation CreateProcess(string? applicationName, string? commandLine, Win32.Constants.ProcessCreationFlags creationFlags) => dispatcher.Invoke(() => Win32.Methods.CreateProcess(applicationName, commandLine, creationFlags));
        public void DebugActiveProcess(uint processId) => dispatcher.Invoke(() => Win32.Methods.DebugActiveProcess(processId));
        public Win32.DebugEvent WaitForDebugEvent() => dispatcher.Invoke(Win32.Methods.WaitForDebugEvent);
        public Win32.DebugEvent WaitForDebugEvent(uint milliseconds) => dispatcher.Invoke(() => Win32.Methods.WaitForDebugEvent(milliseconds));
        public Task<Win32.DebugEvent> WaitForDebugEventAsync() => dispatcher.InvokeAsync(Win32.Methods.WaitForDebugEvent);
        public Task<Win32.DebugEvent> WaitForDebugEventAsync(uint milliseconds) => dispatcher.InvokeAsync(() => Win32.Methods.WaitForDebugEvent(milliseconds));
        public void ContinueDebugEvent(Win32.DebugEvent debugEvent, uint continueStatus) => dispatcher.Invoke(() => Win32.Methods.ContinueDebugEvent(debugEvent, continueStatus));
        public void DebugActiveProcessStop(uint processId) => dispatcher.Invoke(() => Win32.Methods.DebugActiveProcessStop(processId));
        public void DebugSetProcessKillOnExit(bool killOnExit) => dispatcher.Invoke(() => Win32.Methods.DebugSetProcessKillOnExit(killOnExit));

        public Win32.DebugEvent WaitForDebugEvent(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return WaitForDebugEvent(100);
                }
                catch (Win32Exception e) when (e.NativeErrorCode == 121) { }
            }
        }

        public async Task<Win32.DebugEvent> WaitForDebugEventAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return await WaitForDebugEventAsync(100);
                }
                catch (Win32Exception e) when (e.NativeErrorCode == 121) { }
            }
        }

        public void Dispose()
        {
            DisposeImpl();
            GC.SuppressFinalize(this);
        }

        ~Debugger() => DisposeImpl();
        private void DisposeImpl() => dispatcher.Dispose();
    }
}
