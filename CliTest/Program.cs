using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using CliUi;

namespace CliTest
{
    internal class Program
    {
        /// <summary>
        /// An object to lock on
        /// </summary>
        private static object locker = new object();
        private static StreamWriter dbgFile;

        static void WriteHello()
        {
            Console.WriteLine("hello");
        }

        [CmdLine("print a lot of lines")]
        static void PrintLines()
        {
            for (int i = 0; i < 50; ++i)
            {
                Console.WriteLine($"ct={Console.CursorTop} bh={Console.BufferHeight}");
            }
        }
        static void TestPages(CmdLineUi cui)
        {
            lock (Console.Out)
            {
                cui.NextPage();
                for (int i = 0; i < 200; ++i)
                {
                    Console.WriteLine($"{i,3} ct={Console.CursorTop} bh={Console.BufferHeight}");
                    Thread.Sleep(100);
                    var resp = cui.Pager();
                    if (!string.IsNullOrEmpty(resp))
                    {
                        Console.WriteLine($"got {resp}");
                        break;
                    }
                }
            }
        }
        [CmdLineAdder]
        public static void AddMyCommands(CmdLineUi cui)
        {
            cui.Add("pages", () => { TestPages(cui); }, 0);
        }

        [CmdLine("add just a few more commands")]
        public static void AddMore()
        {
            var cui = CmdLineUi.Instance;
            Console.WriteLine("adding more commands");
            for (int i = 0; i < 3; ++i)
            {
                var guid = Guid.NewGuid();
                var cmd = $"new command {guid}";
                cui.Add(cmd, AddMore, 0);
            }
        }
        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Launch();
            var cui = CmdLineUi.Instance;
            while (true)
            {
                var k = Console.ReadKey(true);
                switch (k.Key)
                {
                    case ConsoleKey.Escape:
                    case ConsoleKey.X:
                    case ConsoleKey.Q:
                        return;
                    case ConsoleKey.D:
                        cui.ExLog = WriteExceptions;
                        break;
                    case ConsoleKey.F:
                        {
                            //cui.Debug = true;
                            cui.Add("hello", WriteHello, 0);
                            cui.Scan4Commands();
                            Console.WriteLine("looping");
                            cui.CommandLoop();
                        }
                        break;
                    case ConsoleKey.L:
                        PrintLines();
                        break;
                    case ConsoleKey.M:
                        {
                            Console.WriteLine($"modifiers {k.Modifiers}");
                        }
                        break;
                    case ConsoleKey.N:
                        cui.NextPage(ConsoleColor.Gray, ConsoleColor.DarkBlue);
                        break;
                    case ConsoleKey.P:
                        Console.WriteLine($"c: {Console.CursorLeft}/{Console.CursorTop} w: {Console.WindowLeft}/{Console.WindowTop}" +
                            $"({Console.WindowWidth}x{Console.WindowHeight}) b: {Console.BufferWidth}x{Console.BufferHeight}");
                        break;
                    case ConsoleKey.R:
                        {
                            Console.ResetColor();
                            var s = new string(' ', Console.BufferHeight * Console.BufferWidth);
                            Console.CursorLeft = 0;
                            Console.CursorTop = 0;
                            Console.Write(s);
                            Console.CursorTop = 0;
                            Console.CursorLeft = 0;
                        }
                        break;
                    case ConsoleKey.T:
                        TestConsole();
                        break;
                    case ConsoleKey.V:
                        {
                            Console.WriteLine($" version {BuildVersion.Version}");
                        }
                        break;
                    default:
                        Console.Write("?");
                        break;
                }
            }
        }

        private static void WriteExceptions(Exception ex)
        {
            if (dbgFile == null)
            {
                lock (locker)
                {
                    if (dbgFile == null)
                    {
                        dbgFile = File.CreateText("debug.txt");
                    }
                }
            }
            var dts = DateTime.Now.ToString("hh:mm:ss.FFF");
            dbgFile.WriteLine("--- Exception {dts} ---");
            for(var cex=ex;cex != null;cex = ex.InnerException)
            {
                dbgFile.WriteLine($"{cex.GetType()}: {cex.Message}");
            }
            dbgFile.WriteLine($" at {ex.StackTrace}");
        }

        private static void TestConsole()
        {

        }
    }
}
