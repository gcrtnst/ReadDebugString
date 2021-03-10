using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReadDebugString
{
    class Program
    {
        static async Task<int> Main(string[] args)
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

        static async Task<int> AttachMain(uint pid, IConsole console, CancellationToken token)
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

        static async Task<int> StartMain(IList<string> commandLine, IConsole console, CancellationToken token)
        {
            var s = BuildCommandLine(commandLine);

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

        static async Task MainLoop(DebugStringStream stream, IConsole console, CancellationToken token)
        {
            await using var enumerator = stream.GetAsyncEnumerator(token);
            while (await enumerator.MoveNextAsync())
            {
                console.Out.Write(enumerator.Current);
            }
        }

        static string BuildCommandLine(IList<string> args)
        {
            if (args.Count < 1) throw new ArgumentException(null, nameof(args));
            var regex = new Regex(@"(\+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

            var escaped = new List<string>
            {
                "\"" + args[0] + "\"",
            };
            for (var i = 1; i < args.Count; i++)
            {
                var s = args[i];
                s = regex.Replace(s, "$1$1\"");
                s = s.Replace("\"", @"\""");
                s = "\"" + s + "\"";
                escaped.Add(s);
            }
            return string.Join(" ", escaped);
        }
    }
}
