using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Sdk = Microsoft.Windows.Sdk;

namespace ReadDebugString.Win32
{
    internal static class Methods
    {
        internal static void DebugActiveProcess(uint processId)
        {
            var result = Sdk.PInvoke.DebugActiveProcess(processId);
            if (!result) throw new Win32Exception();
        }

        internal static DebugEvent WaitForDebugEvent() => WaitForDebugEvent(Sdk.Constants.INFINITE);

        internal static DebugEvent WaitForDebugEvent(uint milliseconds)
        {
            var result = Sdk.PInvoke.WaitForDebugEventEx(out var debugEvent, milliseconds);
            if (!result) throw new Win32Exception();
            try
            {
                return DebugEvent.Unmarshal(debugEvent);
            }
            catch (ArgumentException e)
            {
                throw new NotImplementedException(null, e);
            }
        }

        internal static void ContinueDebugEvent(DebugEvent debugEvent, Constants.DebugContinueStatus continueStatus)
        {
            var result = Sdk.PInvoke.ContinueDebugEvent(debugEvent.ProcessId, debugEvent.ThreadId, (uint)continueStatus);
            if (!result) throw new Win32Exception();
        }

        internal static void DebugActiveProcessStop(uint processId)
        {
            var result = Sdk.PInvoke.DebugActiveProcessStop(processId);
            if (!result) throw new Win32Exception();
        }

        internal static void DebugSetProcessKillOnExit(bool killOnExit)
        {
            var result = Sdk.PInvoke.DebugSetProcessKillOnExit(killOnExit);
            if (!result) throw new Win32Exception();
        }

        internal static unsafe T? ReadProcessMemory<T>(SafeProcessHandle process, IntPtr baseAddress) where T : unmanaged
        {
            var buf = new T[1];
            var size = ReadProcessMemory(process, baseAddress, buf.AsSpan());
            if (size < (nuint)sizeof(T)) return null;
            return buf[0];
        }

        internal static unsafe nuint ReadProcessMemory<T>(SafeProcessHandle process, IntPtr baseAddress, Span<T> buffer) where T : unmanaged
        {
            nuint numberOfBytesRead;
            bool result;
            fixed (void* lpBuffer = buffer) result = Sdk.PInvoke.ReadProcessMemory(process, (void*)baseAddress, lpBuffer, (nuint)buffer.Length * (nuint)sizeof(T), &numberOfBytesRead);
            if (!result)
            {
                var e = new Win32Exception();
                if (e.NativeErrorCode != 299) throw e;
            }
            return numberOfBytesRead;
        }

        internal static unsafe ProcessInformation CreateProcess(string? applicationName, string? commandLine, Constants.ProcessCreationFlags creationFlags)
        {
            var arrCommandLine = new char[(commandLine?.Length + 1) ?? 0];
            commandLine?.CopyTo(0, arrCommandLine, 0, commandLine.Length);

            var startupInfo = new Sdk.STARTUPINFOW
            {
                cb = (uint)sizeof(Sdk.STARTUPINFOW),
            };
            var processInformation = new Sdk.PROCESS_INFORMATION();
            bool result;
            fixed (char* lpApplicationName = applicationName) fixed (char* lpCommandLine = commandLine)
            {
                result = Sdk.PInvoke.CreateProcess(lpApplicationName, lpCommandLine, null, null, false, (Sdk.PROCESS_CREATION_FLAGS)creationFlags, null, null, &startupInfo, &processInformation);
            }
            if (!result) throw new Win32Exception();
            return new ProcessInformation(processInformation);
        }
    }

    internal class Constants
    {
        public enum DebugContinueStatus : uint
        {
            DbgContinue = 0x00010002,
            DbgExceptionNotHandled = 0x80010001,
            DbgReplyLater = 0x40010001,
        }

        [Flags]
        public enum ProcessCreationFlags : uint
        {
            DebugProcess = Sdk.PROCESS_CREATION_FLAGS.DEBUG_PROCESS,
            DebugOnlyThisProcess = Sdk.PROCESS_CREATION_FLAGS.DEBUG_ONLY_THIS_PROCESS,
            CreateSuspended = Sdk.PROCESS_CREATION_FLAGS.CREATE_SUSPENDED,
            DetachedProcess = Sdk.PROCESS_CREATION_FLAGS.DETACHED_PROCESS,
            CreateNewConsole = Sdk.PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE,
            NormalPriorityClass = Sdk.PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS,
            IdlePriorityClass = Sdk.PROCESS_CREATION_FLAGS.IDLE_PRIORITY_CLASS,
            HighPriorityClass = Sdk.PROCESS_CREATION_FLAGS.HIGH_PRIORITY_CLASS,
            RealtimePriorityClass = Sdk.PROCESS_CREATION_FLAGS.REALTIME_PRIORITY_CLASS,
            CreateNewProcessGroup = Sdk.PROCESS_CREATION_FLAGS.CREATE_NEW_PROCESS_GROUP,
            // CreateUnicodeEnvironment = Sdk.PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT,
            CreateSeparateWowVdm = Sdk.PROCESS_CREATION_FLAGS.CREATE_SEPARATE_WOW_VDM,
            CreateSharedWowVdm = Sdk.PROCESS_CREATION_FLAGS.CREATE_SHARED_WOW_VDM,
            CreateForceDos = Sdk.PROCESS_CREATION_FLAGS.CREATE_FORCEDOS,
            BelowNormalPriorityClass = Sdk.PROCESS_CREATION_FLAGS.BELOW_NORMAL_PRIORITY_CLASS,
            AboveNormalPriorityClass = Sdk.PROCESS_CREATION_FLAGS.ABOVE_NORMAL_PRIORITY_CLASS,
            InheritParentAffinity = Sdk.PROCESS_CREATION_FLAGS.INHERIT_PARENT_AFFINITY,
            InheritCallerAffinity = Sdk.PROCESS_CREATION_FLAGS.INHERIT_CALLER_PRIORITY,
            CreateProtectedProcess = Sdk.PROCESS_CREATION_FLAGS.CREATE_PROTECTED_PROCESS,
            // ExtendedStartupInfoPresent = Sdk.PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT,
            ProcessModeBackgroundBegin = Sdk.PROCESS_CREATION_FLAGS.PROCESS_MODE_BACKGROUND_BEGIN,
            ProcessModeBackgroundEnd = Sdk.PROCESS_CREATION_FLAGS.PROCESS_MODE_BACKGROUND_END,
            CreateSecureProcess = Sdk.PROCESS_CREATION_FLAGS.CREATE_SECURE_PROCESS,
            CreateBreakawayFromJob = Sdk.PROCESS_CREATION_FLAGS.CREATE_BREAKAWAY_FROM_JOB,
            CreatePreserveCodeAuthzLevel = Sdk.PROCESS_CREATION_FLAGS.CREATE_PRESERVE_CODE_AUTHZ_LEVEL,
            CreateDefaultErrorMode = Sdk.PROCESS_CREATION_FLAGS.CREATE_DEFAULT_ERROR_MODE,
            CreateNoWindow = Sdk.PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW,
            ProfileUser = Sdk.PROCESS_CREATION_FLAGS.PROFILE_USER,
            ProfileKernel = Sdk.PROCESS_CREATION_FLAGS.PROFILE_KERNEL,
            ProfileServer = Sdk.PROCESS_CREATION_FLAGS.PROFILE_SERVER,
            CreateIgnoreSystemDefault = Sdk.PROCESS_CREATION_FLAGS.CREATE_IGNORE_SYSTEM_DEFAULT,
        }
    }

    internal class DebugEvent
    {
        public readonly uint ProcessId;
        public readonly uint ThreadId;

        internal DebugEvent(uint processId, uint threadId)
        {
            ProcessId = processId;
            ThreadId = threadId;
        }

        internal static DebugEvent Unmarshal(Sdk.DEBUG_EVENT debugEvent) => debugEvent.dwDebugEventCode switch
        {
            3 => new CreateProcessDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.u.CreateProcessInfo),
            2 => new CreateThreadDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.u.CreateThread),
            1 => new ExceptionDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.u.Exception),
            5 => new ExitProcessDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.u.ExitProcess),
            4 => new ExitThreadDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.u.ExitThread),
            6 => new LoadDllDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.u.LoadDll),
            8 => new OutputDebugStringEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.u.DebugString),
            9 => new RipEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.u.RipInfo),
            7 => new UnloadDllDebugInfo(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.u.UnloadDll),
            _ => throw new ArgumentException(null, nameof(debugEvent)),
        };

        private protected static unsafe string? ReadImageName(SafeProcessHandle process, IntPtr imageName, bool unicode)
        {
            if (imageName == IntPtr.Zero) return null;
            var imageName2 = Methods.ReadProcessMemory<IntPtr>(process, imageName);
            if (imageName2 is null) return null;
            return ReadString(process, (IntPtr)imageName2, unicode);
        }

        private protected static unsafe string? ReadString(SafeProcessHandle process, IntPtr baseAddress, bool unicode) => unicode switch
        {
            false => ReadAsciiString(process, (byte*)baseAddress),
            _ => ReadUnicodeString(process, (char*)baseAddress),
        };

        private protected static unsafe string? ReadUnicodeString(SafeProcessHandle process, char* baseAddress)
        {
            if (baseAddress is null) return null;

            var buf = new List<char>();
            while (true)
            {
                var c = Methods.ReadProcessMemory<char>(process, (IntPtr)baseAddress);
                if (c is null or (char?)0) break;
                buf.Add(c ?? '\0');
                baseAddress++;
            }
            return new string(buf.ToArray());
        }

        private protected static unsafe string? ReadAsciiString(SafeProcessHandle process, byte* baseAddress)
        {
            if (baseAddress is null) return null;

            var buf = new List<byte>();
            while (true)
            {
                var c = Methods.ReadProcessMemory<byte>(process, (IntPtr)baseAddress);
                if (c is null or (byte?)0) break;
                buf.Add(c ?? 0);
                baseAddress++;
            }
            return Encoding.ASCII.GetString(buf.ToArray());
        }
    }

    internal class CreateProcessDebugEvent : DebugEvent
    {
        public readonly SafeFileHandle File;
        public readonly SafeProcessHandle Process;
        public readonly SafeThreadHandle Thread;
        public readonly IntPtr BaseOfImage;
        public readonly uint DebugInfoFileOffset;
        public readonly uint DebugInfoSize;
        public readonly IntPtr ThreadLocalBase;
        public readonly IntPtr StartAddress;
        private readonly IntPtr imageName;
        private readonly bool unicode;

        internal unsafe CreateProcessDebugEvent(uint processId, uint threadId, Sdk.CREATE_PROCESS_DEBUG_INFO info) : base(processId, threadId)
        {
            File = new SafeFileHandle(info.hFile, true);
            Process = new SafeProcessHandle(info.hProcess, false);
            Thread = new SafeThreadHandle(info.hThread, false);
            BaseOfImage = (IntPtr)info.lpBaseOfImage;
            DebugInfoFileOffset = info.dwDebugInfoFileOffset;
            DebugInfoSize = info.dwDebugInfoFileOffset;
            ThreadLocalBase = (IntPtr)info.lpThreadLocalBase;
            StartAddress = (IntPtr)info.lpStartAddress;
            imageName = (IntPtr)info.lpImageName;
            unicode = info.fUnicode != 0;
        }

        public string? ReadImageName() => ReadImageName(Process);
        public string? ReadImageName(SafeProcessHandle process) => ReadImageName(process, imageName, unicode);
    }

    internal class CreateThreadDebugEvent : DebugEvent
    {
        public readonly SafeThreadHandle Thread;
        public readonly IntPtr ThreadLocalBase;
        public readonly IntPtr StartAddress;

        internal unsafe CreateThreadDebugEvent(uint processId, uint threadId, Sdk.CREATE_THREAD_DEBUG_INFO info) : base(processId, threadId)
        {
            Thread = new SafeThreadHandle(info.hThread, false);
            ThreadLocalBase = (IntPtr)info.lpThreadLocalBase;
            StartAddress = (IntPtr)info.lpStartAddress;
        }
    }

    internal class ExceptionDebugEvent : DebugEvent
    {
        public readonly ExceptionRecord ExceptionRecord;
        public readonly bool FirstChance;

        internal ExceptionDebugEvent(uint processId, uint threadId, Sdk.EXCEPTION_DEBUG_INFO info) : base(processId, threadId)
        {
            ExceptionRecord = new ExceptionRecord(info.ExceptionRecord);
            FirstChance = info.dwFirstChance != 0;
        }
    }

    internal class ExitProcessDebugEvent : DebugEvent
    {
        public readonly uint ExitCode;

        internal ExitProcessDebugEvent(uint processId, uint threadId, Sdk.EXIT_PROCESS_DEBUG_INFO info) : base(processId, threadId)
        {
            ExitCode = info.dwExitCode;
        }
    }

    internal class ExitThreadDebugEvent : DebugEvent
    {
        public readonly uint ExitCode;

        internal ExitThreadDebugEvent(uint processId, uint threadId, Sdk.EXIT_THREAD_DEBUG_INFO info) : base(processId, threadId)
        {
            ExitCode = info.dwExitCode;
        }
    }

    internal class LoadDllDebugEvent : DebugEvent
    {
        public readonly SafeFileHandle File;
        public readonly IntPtr BaseOfDll;
        public readonly uint DebugInfoFileOffset;
        public readonly uint DebugInfoSize;
        private readonly IntPtr imageName;
        private readonly bool unicode;

        internal unsafe LoadDllDebugEvent(uint processId, uint threadId, Sdk.LOAD_DLL_DEBUG_INFO info) : base(processId, threadId)
        {
            File = new SafeFileHandle(info.hFile, true);
            BaseOfDll = (IntPtr)info.lpBaseOfDll;
            DebugInfoFileOffset = info.dwDebugInfoFileOffset;
            DebugInfoSize = info.nDebugInfoSize;
            imageName = (IntPtr)info.lpImageName;
            unicode = info.fUnicode != 0;
        }

        public string? ReadImageName(SafeProcessHandle process) => ReadImageName(process, imageName, unicode);
    }

    internal class OutputDebugStringEvent : DebugEvent
    {
        private readonly IntPtr debugStringData;
        private readonly bool unicode;

        internal unsafe OutputDebugStringEvent(uint processId, uint threadId, Sdk.OUTPUT_DEBUG_STRING_INFO info) : base(processId, threadId)
        {
            debugStringData = (IntPtr)info.lpDebugStringData.Value;
            unicode = info.fUnicode != 0;
        }

        public string ReadDebugStringData(SafeProcessHandle process)
        {
            var debugString = ReadString(process, debugStringData, unicode);
            if (debugString is null) throw new InvalidOperationException();
            return debugString;
        }
    }

    internal class RipEvent : DebugEvent
    {
        public readonly uint Error;
        public readonly uint Type;

        internal RipEvent(uint processId, uint threadId, Sdk.RIP_INFO info) : base(processId, threadId)
        {
            Error = info.dwError;
            Type = info.dwType;
        }
    }

    internal class UnloadDllDebugInfo : DebugEvent
    {
        public readonly IntPtr BaseOfDll;

        internal unsafe UnloadDllDebugInfo(uint processId, uint threadId, Sdk.UNLOAD_DLL_DEBUG_INFO info) : base(processId, threadId)
        {
            BaseOfDll = (IntPtr)info.lpBaseOfDll;
        }
    }

    internal class ExceptionRecord
    {
        public readonly uint Code;
        public readonly uint Flags;
        public readonly ExceptionRecord? Record;
        public readonly IntPtr Address;
        public readonly ulong[] Information;

        internal unsafe ExceptionRecord(Sdk.EXCEPTION_RECORD record)
        {
            Code = record.ExceptionCode;
            Flags = record.ExceptionFlags;
            if (record.ExceptionRecord != null) Record = new ExceptionRecord(*record.ExceptionRecord);
            Address = (IntPtr)record.ExceptionAddress;

            Information = new ulong[record.NumberParameters];
            for (var i = 0; i < record.NumberParameters; i++) Information[i] = record.ExceptionInformation[i];
        }
    }

    internal class ProcessInformation
    {
        public readonly SafeProcessHandle Process;
        public readonly SafeThreadHandle Thread;
        public readonly uint ProcessId;
        public readonly uint ThreadId;

        internal ProcessInformation(Sdk.PROCESS_INFORMATION processInformation)
        {
            Process = new SafeProcessHandle(processInformation.hProcess, true);
            Thread = new SafeThreadHandle(processInformation.hThread, true);
            ProcessId = processInformation.dwProcessId;
            ThreadId = processInformation.dwThreadId;
        }
    }

    internal class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeThreadHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }

        protected override bool ReleaseHandle() => Sdk.PInvoke.CloseHandle((Sdk.HANDLE)handle);
    }
}
