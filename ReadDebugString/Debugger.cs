﻿using ReadDebugString.Threading;
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
        public void DebugActiveProcess(int processId) => dispatcher.Invoke(() => Win32.Methods.DebugActiveProcess(processId));
        public Win32.DebugEvent WaitForDebugEvent() => dispatcher.Invoke(Win32.Methods.WaitForDebugEvent);
        public Win32.DebugEvent WaitForDebugEvent(int milliseconds) => dispatcher.Invoke(() => Win32.Methods.WaitForDebugEvent(milliseconds));
        public Task<Win32.DebugEvent> WaitForDebugEventAsync() => dispatcher.InvokeAsync(Win32.Methods.WaitForDebugEvent);
        public Task<Win32.DebugEvent> WaitForDebugEventAsync(int milliseconds) => dispatcher.InvokeAsync(() => Win32.Methods.WaitForDebugEvent(milliseconds));
        public void ContinueDebugEvent(Win32.DebugEvent debugEvent, uint continueStatus) => dispatcher.Invoke(() => Win32.Methods.ContinueDebugEvent(debugEvent, continueStatus));
        public void DebugActiveProcessStop(int processId) => dispatcher.Invoke(() => Win32.Methods.DebugActiveProcessStop(processId));
        public void Dispose() => dispatcher.Dispose();

        public Win32.DebugEvent WaitForDebugEvent(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    return WaitForDebugEvent(100);
                }
                catch (Win32Exception e) when (e.NativeErrorCode == 121)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public async Task<Win32.DebugEvent> WaitForDebugEventAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    return await WaitForDebugEventAsync(100);
                }
                catch (Win32Exception e) when (e.NativeErrorCode == 121)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }
    }
}