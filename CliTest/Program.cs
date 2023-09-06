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
        static void AddMore() {
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
            cui.Add("hello", WriteHello, 0);
            cui.Add("lines",PrintLines, 0);
            cui.Add("pages", () => { TestPages(cui); }, 0);
            cui.Add("add just a few more commands", AddMore, 0);
            cui.CommandLoop();
        }
    }
}
