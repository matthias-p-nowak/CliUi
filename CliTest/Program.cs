using System;
using System.Security.Cryptography;
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
                cui.StartNewPage();
                for(int i = 0; i < 200; ++i)
                {
                    Console.WriteLine($"{i,3} ct={Console.CursorTop} bh={Console.BufferHeight}");
                    var resp = cui.Pager();
                    if(!string.IsNullOrEmpty(resp) )
                    {
                        Console.WriteLine($"got {resp}");
                        break;
                    }    
                }
            }
        }
        static void Main(string[] args)
        {
            var cui = CmdLineUi.Instance;
            cui.Add("hello", WriteHello, 0);
            cui.Add("lines",PrintLines, 0);
            cui.Add("pages", () => { TestPages(cui); }, 0);
            cui.CommandLoop();
        }
    }
}
