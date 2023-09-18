using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace CliUi
{
    /// <summary>
    /// The user interface class that maintains a dictionary over all command lines and actions.
    /// </summary>
    public class CmdLineUi
    {
        /// <summary>
        /// also those characters are added to the search string
        /// </summary>
        private static Char[] okChars = new char[] { ' ', '.', '_' };
        /// <summary>
        /// singleton instance, lazy initialized
        /// </summary>
        private static Lazy<CmdLineUi> _instance = new Lazy<CmdLineUi>();
        /// <summary>**/
        /// provides access to a singleton instance, use of others is possible
        /// </summary>
        public static CmdLineUi Instance { get => _instance.Value; }
        /// <summary>
        /// contains the current command line executed
        /// </summary>
        public string CurrentCommand { get => currentCommand; }


        /// <summary>
        /// Contains all possible commands, 
        /// </summary>
        private List<(string cmdLine, Action ac)> commands = new List<(string, Action)>();
        /// <summary>
        /// .when false, the loop of executing commands will end
        /// </summary>
        private bool running = true;
        public bool Debug = false;
        public Action<string, Exception> ExLog = DummyLog;
        // field for the property
        private string currentCommand;
        /// <summary>
        /// The phrase to use for exiting the application
        /// </summary>
        public string ExitPhrase = "Exit application";

        private static void DummyLog(string msg, Exception exception)
        {
        }

        /// <summary>
        /// Adds a new possible command line to the dictionary, 
        /// if p==0, then a new group of highest priority commands is added
        /// </summary>
        /// <param name="cmdLine">the command line to add</param>
        /// <param name="action">the action to be executed</param>
        /// <param name="p">variable priority</param>
        /// <returns></returns>
        public void Add(string cmdLine, Action action)
        {
            lock (commands)
            {
                Remove(cmdLine);
                commands.Insert(0, (cmdLine, action));
            }

        }

        /// <summary>
        /// removes a command line
        /// </summary>
        /// <param name="cmdLine">the command line to remove</param>
        public void Remove(string cmdLine)
        {
            lock (commands)
            {
                int idx;
                for (idx = 0; idx < commands.Count; ++idx)
                {
                    if (string.Equals(cmdLine, commands[idx].cmdLine))
                        break;
                }
                if (idx < commands.Count)
                    commands.RemoveAt(idx);
            }
        }



        /// <summary>
        /// Main loop asking user for input and executing actions
        /// It locks the Console.Out
        /// </summary>
        public void CommandLoop()
        {
            // initial settings
            running = true;
            // for ending the loop
            lock (commands)
            {
                commands.Add((ExitPhrase, () => { running = false; }));
            }
            // entering the loop
            while (running)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(250);
                    continue;
                }
                lock (Console.Out)
                {
                    var keystrokes = string.Empty;
                    if (!Console.KeyAvailable)
                        continue; // someone else took the keypress during the lock
                    keystrokes = Console.ReadLine();
                    Console.CursorTop -= 1;
                    int offset = 1;
                    var remainingChoices = new List<(string cmdLine, Action action)>();
                    try
                    {
                        int curPos = 0;
                        lock (commands)
                        {
                            foreach (var cmd in commands)
                            {
                                var posList = CheckString(cmd.cmdLine, keystrokes);
                                if (posList.Count != keystrokes.Length)
                                    continue;
                                // got a match
                                int cmdPos = offset + remainingChoices.Count;
                                remainingChoices.Add(cmd);
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.BackgroundColor = ConsoleColor.DarkBlue;
                                Console.Write($"{cmdPos,4} ");
                                int pos = 0;
                                foreach (int p in posList)
                                {
                                    if (p > pos)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Blue;
                                        Console.Write(cmd.cmdLine.Substring(pos, p - pos));
                                    }
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.Write(cmd.cmdLine.Substring(p, 1));
                                    pos = p + 1;
                                }
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.Write(cmd.cmdLine.Substring(pos));
                                var s = new string(' ', Console.BufferWidth - Console.CursorLeft - 1);
                                Console.WriteLine(s);
                                Pager(ref curPos);
                            }
                            if (remainingChoices.Count == 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("sorry");
                            }
                            if (remainingChoices.Count == 1)
                            {
                                Console.CursorTop -= 1;
                                Console.ResetColor();
                                var s = new string(' ', Console.BufferWidth);
                                Console.Write(s);
                                Console.CursorLeft = 0;
                                throw new CmdLineInterrupt(string.Empty, 1);
                            }
                            Pager(ref curPos, true);
                            continue;
                        }
                    }
                    catch (CmdLineInterrupt interrupt)
                    {
                        if (interrupt.Number > 0)
                        {
                            if (interrupt.Number > remainingChoices.Count)
                            {
                                Console.ResetColor();
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"choice is invalid");
                                continue;
                            }
                            var cmd = remainingChoices[interrupt.Number - 1];
                            currentCommand = cmd.cmdLine;
                            Add(cmd.cmdLine, cmd.action); // moving it up
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.Write((char)187);
                            Console.WriteLine(cmd.cmdLine);
                            Console.ResetColor();
                            cmd.action();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("invalid choice you made");
                        }
                    }
                    finally
                    {
                        Console.ResetColor();
                    }
                }
            } // while running
              // Console.WriteLine("loop ended, back to normal");
        }

        private Regex responseRegExp = new Regex(@"^\s*(?<txt>[a-zA-Z]*)\s*(?<digits>\d*)\s*$");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="curPos"></param>
        /// <param name="force"></param>
        /// <exception cref="CmdLineInterrupt">throws an exception if user enters something else than return</exception>
        public void Pager(ref int curPos, bool force = false)
        {
            if (Console.KeyAvailable || ++curPos > Console.WindowHeight - 2 || force)
            {
                var response = Console.ReadLine();
                Console.CursorTop -= 1; // going up one line
                curPos = 0;
                if (!force && string.IsNullOrWhiteSpace(response))
                {
                    return;
                }
                var match = responseRegExp.Match(response);
                if (match.Success)
                {
                    var str = match.Groups["txt"].Value.ToLower();
                    var numStr = match.Groups["digits"].Value;
                    int number = 0;
                    if (numStr.Length > 0)
                    {
                        number = int.Parse(numStr);
                    }
                    throw new CmdLineInterrupt(str, number);
                }
                else
                {
                    ExLog($"cannot match {response}", null);
                }
            }
        }

        /// <summary>
        /// Creates a list of matching positions
        /// </summary>
        /// <param name="heystack">the full string to match</param>
        /// <param name="keystrokes">the needle to find</param>
        /// <returns>a list of matching positions</returns>
        private static List<int> CheckString(string heystack, string keystrokes)
        {
            heystack = heystack.ToLower();
            keystrokes = keystrokes.ToLower();
            var l = new List<int>();
            int pos = -1;
            foreach (char c in keystrokes)
            {
                pos = heystack.IndexOf(c, pos + 1);
                if (pos < 0)
                    return l;
                l.Add(pos);
            }
            return l;
        }


        /// <summary>
        /// Scans all loaded assemblies for annotated methods 
        /// </summary>
        public void Scan4Commands()
        {
            var cmdLineAssemblyName = this.GetType().Assembly.GetName();
            foreach (var loaded_assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var dep_assembly in loaded_assembly.GetReferencedAssemblies())
                    {
                        if (cmdLineAssemblyName.Name.CompareTo(dep_assembly.Name) == 0 &&
                            cmdLineAssemblyName.Version.CompareTo(dep_assembly.Version) == 0)
                        {
                            if (Debug)
                            {
                                Console.WriteLine($"considering assembly {dep_assembly.Name}");
                            }
                            foreach (var loaded_type in loaded_assembly.GetTypes())
                            {
                                foreach (var method in loaded_type.GetMethods())
                                {
                                    if (!method.IsStatic)
                                        continue;
                                    foreach (CmdLineAttribute attr in method.GetCustomAttributes(typeof(CmdLineAttribute), false))
                                    {
                                        var this_method = method;
                                        Action ac = () => { this_method.Invoke(null, new object[] { }); };
                                        Add(attr.CmdLine, ac);
                                        if (Debug)
                                        {
                                            Console.WriteLine($"added '{attr.CmdLine}' -> {loaded_assembly.GetName().Name}.{loaded_type.Name}.{this_method.Name}");
                                        }
                                    }
                                    foreach (CmdLineAdderAttribute attr in method.GetCustomAttributes(typeof(CmdLineAdderAttribute), false))
                                    {
                                        method.Invoke(null, new object[] { this });
                                        if (Debug)
                                        {
                                            Console.WriteLine($"executed {loaded_assembly.GetName().Name}.{loaded_type.Name}.{method.Name}");
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExLog("uncatched", ex);
                }

            }
        }
    }
}
