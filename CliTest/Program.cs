using System;
using CliUi;

namespace CliTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var cui = CmdLineUi.Instance;
            Action action = () => { Console.WriteLine("hello"); };
            cui.Add("hello", action, 2);

        }
    }
}
