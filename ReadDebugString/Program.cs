using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReadDebugString
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var attachCommand = new Command("attach")
            {
                new Argument<uint>("pid"),
            };
            attachCommand.Handler = CommandHandler.Create((uint pid, IConsole console, CancellationToken token) => AttachMain(pid, console, token));

            var startCommand = new Command("start")
            {
                new Argument<List<string>>("cmdline")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
            };
            startCommand.Handler = CommandHandler.Create((IList<string> cmdline, IConsole console, CancellationToken token) => StartMain(cmdline, console, token));

            var rootCommand = new RootCommand()
            {
                attachCommand,
                startCommand,
            };
            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> AttachMain(uint pid, IConsole console, CancellationToken token)
        {
            if (pid < 0)
            {
                console.Error.Write("ERROR: PID must be positive\n");
                return 1;
            }

            DebugStringStream stream;
            try
            {
                stream = new DebugStringStream(pid);
            }
            catch (Win32Exception e)
            {
                console.Error.Write($"ERROR: Win32({e.NativeErrorCode}): {e.Message}\n");
                return 1;
            }

            await MainLoop(stream, console, token);
            return 0;
        }

        private static async Task<int> StartMain(IList<string> commandLine, IConsole console, CancellationToken token)
        {
            var s = BuildCommandLine(commandLine.First(), commandLine.Skip(1));

            DebugStringStream stream;
            try
            {
                stream = new DebugStringStream(null, s);
            }
            catch (Win32Exception e)
            {
                console.Error.Write($"ERROR: Win32({e.NativeErrorCode}): {e.Message}\n");
                return 1;
            }

            await MainLoop(stream, console, token);
            return 0;
        }

        private static async Task MainLoop(DebugStringStream stream, IConsole console, CancellationToken token)
        {
            await using var enumerator = stream.GetAsyncEnumerator(token);
            while (await enumerator.MoveNextAsync())
            {
                console.Out.Write(enumerator.Current);
            }
        }

        private static string BuildCommandLine(string moduleName, IEnumerable<string> commandLine)
        {
            if (moduleName.Contains('"')) throw new ArgumentException(null, nameof(moduleName));
            return "\"" + moduleName + "\" " + BuildCommandLine(commandLine);
        }

        private static readonly Regex buildCommandLineRegex = new(@"(\\+)(""|$)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        private static string BuildCommandLine(IEnumerable<string> commandLine)
        {
            var args = new List<string>();
            foreach (var c in commandLine)
            {
                var s = c;
                s = buildCommandLineRegex.Replace(s, "$1$1$2");
                s = s.Replace("\"", @"\""");
                s = "\"" + s + "\"";
                args.Add(s);
            }
            return string.Join(" ", args);
        }
    }
}
