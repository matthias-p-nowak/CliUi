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
        /// <summary>
        /// Be more verbose during execution - especially for Scan4Commands
        /// </summary>
        public bool Debug = false;
        /// <summary>
        /// A simple way to log some exception to a file when exceptions happen
        /// Assign a function <code>void Func(string msg, Exception ex)</code>
        /// </summary>
        public Action<string, Exception> ExLog = DummyLog;
        // field for the property
        private string currentCommand;
        /// <summary>
        /// The phrase to use for exiting the application
        /// </summary>
        public string ExitPhrase = "Exit application";
        /// <summary>
        /// Response when a command was not found
        /// </summary>
        public string CommandNotFound = "sorry";
        /// <summary>
        /// Response when the choice was not recognized
        /// </summary>
        public string WrongChoiceAnswer = "an invalid choice you made";
        /// <summary>
        /// Response when the entered keystrokes did not match any command
        /// </summary>
        public string CouldNotMatchResponse = "match not succeeded";
        /// <summary>
        /// Raising event before execution with the intended command as argument
        /// </summary>
        public event Action<string> Cmd2Execute;

        // for the Pager to work
        private int pagerRowCount = 0;
        private static void DummyLog(string msg, Exception exception)
        {
        }

        /// <summary>
        /// Adds a new possible command line to the dictionary, 
        /// if p==0, then a new group of highest priority commands is added
        /// </summary>
        /// <param name="cmdLine">the command line to add</param>
        /// <param name="action">the action to be executed</param>
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
            long loopStream = 0;
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
                (string cmdLine, Action action)? selectedCommand = null;
                lock (Console.Out)
                {
                    var keystrokes = string.Empty;
                    if (!Console.KeyAvailable)
                        continue; // someone else took the keypress during the lock
                    CheckSameStream(ref loopStream); // just advancing that counter
                    var ct = Console.CursorTop;
                    var cl = Console.CursorLeft;
                    var ww = Console.WindowWidth;
                    Console.SetCursorPosition(ww - 7, Console.WindowTop);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("  cmd  ");
                    Console.ResetColor();
                    Console.SetCursorPosition(cl, ct);
                    keystrokes = Console.ReadLine();
                    Console.CursorTop -= 1;
                    int offset = 1;
                    var remainingChoices = new List<(string cmdLine, Action action)>();
                    try
                    {
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
                                WriteLineWithMatches(cmd.cmdLine, posList, ConsoleColor.Gray, ConsoleColor.White);
                                Pager();
                            }
                            if (remainingChoices.Count == 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine(CommandNotFound);
                                continue;
                            }
                            if (remainingChoices.Count == 1)
                            {
                                Console.CursorTop -= 1;
                                Console.ResetColor();
                                var s = new string(' ', Console.BufferWidth);
                                Console.Write(s);
                                Console.CursorLeft = 0;
                                throw new CmdLineInterrupt(1);
                            }
                            Pager(true);
                            continue;
                        }
                    }
                    catch (CmdLineInterrupt interrupt)
                    {
                        if (interrupt.Numbers.Count > 0 && interrupt.Numbers[0] > 0)
                        {
                            if (interrupt.Numbers[0] > remainingChoices.Count)
                            {
                                Console.ResetColor();
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write($"choice exceeds available items");
                                Console.ResetColor();
                                Console.WriteLine();
                                continue;
                            }
                            selectedCommand = remainingChoices[interrupt.Numbers[0] - 1];
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(WrongChoiceAnswer);
                            Console.ResetColor();
                            Console.WriteLine();
                        }
                    }
                    finally
                    {
                        Console.ResetColor();
                    }
                }
                if (selectedCommand != null)
                {
                    currentCommand = selectedCommand.Value.cmdLine;
                    Add(selectedCommand.Value.cmdLine, selectedCommand.Value.action); // moving it up
                    lock (Console.Out)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.Write((char)187);
                        Console.WriteLine(selectedCommand.Value.cmdLine);
                        Console.ResetColor();
                    }
                    pagerRowCount = 0;
                    if (Cmd2Execute != null)
                        Cmd2Execute(currentCommand);
                    selectedCommand.Value.action();
                }
            } // while running
        }

        /// <summary>
        /// Writes the string with the positions highlighted
        /// </summary>
        /// <param name="cmdLine">the line to print</param>
        /// <param name="posList">the positions to highlight</param>
        /// <param name="low">the lower contrast color</param>
        /// <param name="high">the high contrast color</param>
        public static void WriteLineWithMatches(string cmdLine, List<int> posList, ConsoleColor low, ConsoleColor high)
        {
            int pos = 0;
            foreach (int p in posList)
            {
                if (p > pos)
                {
                    Console.ForegroundColor = low;
                    Console.Write(cmdLine.Substring(pos, p - pos));
                }
                Console.ForegroundColor = high;
                Console.Write(cmdLine.Substring(p, 1));
                pos = p + 1;
            }
            Console.ForegroundColor = low;
            Console.Write(cmdLine.Substring(pos));
            var s = new string(' ', Console.BufferWidth - Console.CursorLeft - 1);
            Console.WriteLine(s);
        }

        /// <summary>
        /// Resetting the row count for the Pager
        /// </summary>
        public void PagerReset()
        {
            pagerRowCount = 0;
        }

        private Regex responseRegExp = new Regex(@"\s*(?:(?<txt>[a-zA-Z]+)|(?<le>\d+)-(?<ue>\d+)|(?<sn>\d+))[,\s]*");
        private long currentOutputCounter = 0;

        /// <summary>
        /// Check if the output from this stream was interrupted or not.
        /// </summary>
        /// <param name="outputCounter">keeps track of streams, use one for each stream</param>
        /// <returns>returns true if the previous call was from the same thread, returns false if another function interrupted this output</returns>
        public bool CheckSameStream(ref long outputCounter)
        {
            var same = outputCounter == currentOutputCounter;
            outputCounter = ++currentOutputCounter;
            return same;
        }

        /// <summary>
        /// Returns immediately when no key is pressed and called less times than the window is high, counter is reset by PagerReset
        /// </summary>
        /// <param name="force">wait for user input</param>
        /// <exception cref="CmdLineInterrupt">throws an exception if user enters something else than return</exception>
        public void Pager(bool force = false)
        {
            var fgColor = Console.ForegroundColor;
            var bgColor = Console.BackgroundColor;
            if (Console.KeyAvailable || ++pagerRowCount > Console.WindowHeight - 2 || force)
            {
                try
                {
                    var ct = Console.CursorTop;
                    var cl = Console.CursorLeft;
                    var ww = Console.WindowWidth;
                    Console.SetCursorPosition(ww - 7, Console.WindowTop);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(" pager ");
                    Console.ResetColor();
                    Console.SetCursorPosition(cl, ct);
                    var response = Console.ReadLine();
                    pagerRowCount = 0;
                    if (!force && string.IsNullOrWhiteSpace(response))
                    {
                        Console.CursorTop -= 1; // going up one line
                        return;
                    }
                    var txts = new List<string>();
                    var numbers = new List<int>();
                    foreach (Match ma in responseRegExp.Matches(response))
                    {
                        if (!ma.Success)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(CouldNotMatchResponse);
                            return;
                        }
                        var txt = ma.Groups["txt"].Value;
                        var le = ma.Groups["le"].Value;
                        var ue = ma.Groups["ue"].Value;
                        var sn = ma.Groups["sn"].Value;
                        if (!string.IsNullOrEmpty(txt))
                        {
                            txts.Add(txt);
                        }
                        else if (!string.IsNullOrEmpty(le) && !string.IsNullOrEmpty(ue))
                        {
                            var ile = int.Parse(le);
                            var iue = int.Parse(ue);
                            for (int i = ile; i <= iue; i++)
                            {
                                numbers.Add(i);
                            }
                        }
                        else if (!string.IsNullOrEmpty(sn))
                        {
                            numbers.Add(int.Parse(sn));
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("parsing matched response failed");
                            return;
                        }
                    }
                    throw new CmdLineInterrupt(txts, numbers);
                }
                finally
                {
                    Console.ForegroundColor = fgColor;
                    Console.BackgroundColor = bgColor;
                }
            }
        }

        /// <summary>
        /// Creates a list of matching positions
        /// </summary>
        /// <param name="heystack">the full string to match</param>
        /// <param name="keystrokes">the needle to find</param>
        /// <returns>a list of matching positions</returns>
        public static List<int> CheckString(string heystack, string keystrokes)
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
            if (Debug)
            {
                Console.WriteLine($"CmdLineUi assembly is {cmdLineAssemblyName}");
            }
            foreach (var loaded_assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (Debug)
                {
                    Console.WriteLine($"loaded assembly {loaded_assembly.FullName}");
                }
                try
                {
                    foreach (var dep_assembly in loaded_assembly.GetReferencedAssemblies())
                    {
                        if (cmdLineAssemblyName.Name.CompareTo(dep_assembly.Name) == 0)
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

        /// <summary>
        /// Saves the cursor position and colors for restore
        /// </summary>
        /// <returns>Saved datastructure with data</returns>
        public static SavedCursor SaveCursor()
        {
            return new SavedCursor()
            {
                cl = Console.CursorLeft,
                ct = Console.CursorTop,
                cfg = Console.ForegroundColor,
                cbg = Console.BackgroundColor
            };
        }
    }
}
