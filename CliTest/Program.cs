using System;
using System.Security.Cryptography;
using System.Threading;
using CliUi;

namespace CliTest
{
    internal class Program
    {
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
                for(int i = 0; i < 200; ++i)
                {
                    Console.WriteLine($"{i,3} ct={Console.CursorTop} bh={Console.BufferHeight}");
                    Thread.Sleep(100);
                    var resp = cui.Pager();
                    if(!string.IsNullOrEmpty(resp) )
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
        public static void AddMore() {
            var cui=CmdLineUi.Instance;
            Console.WriteLine("adding more commands");
            for(int i=0;i < 3; ++i) {
                var guid=Guid.NewGuid();
                var cmd = $"new command {guid}";
                cui.Add(cmd, AddMore, 0);
            }
        }
        static void Main(string[] args)
        {
            var cui = CmdLineUi.Instance;
            //cui.Debug = true;
            cui.Add("hello", WriteHello, 0);
            cui.Scan4Commands();
            cui.CommandLoop();
        }
    }
}
