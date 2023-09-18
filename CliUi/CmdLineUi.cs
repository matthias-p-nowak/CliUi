using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        /// when using cursor keys to go up in history, this is idle time in 100ms , 50==5 seconds
        /// </summary>
        private int maxIdle = 50;
        /// <summary>
        /// Contains all possible commands, 
        /// </summary>
        private Dictionary<string, (Action ac, int priority)> commands = new Dictionary<string, (Action, int)>();
        /// <summary>
        /// recent commands or recently added are on the top of the list
        /// </summary>
        private int maxPriority = 10;
        /// <summary>
        /// .when false, the loop of executing commands will end
        /// </summary>
        private bool running = true;
        /// <summary>
        /// current position in the list of matching commands
        /// </summary>
        private int commandPos = 0;
        private int lastRowPos = 0;
        public bool Debug = false;
        public Action<string, Exception> ExLog=DummyLog;
        private string currentCommand;

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
        public int Add(string cmdLine, Action action, int p)
        {
            if (string.IsNullOrWhiteSpace(cmdLine))
                return p;
            if (p == 0)
                p = ++maxPriority;
            else if (p == 1)
                p = maxPriority;
            if (p > maxPriority)
                maxPriority = p;
            lock (commands)
            {
                commands[cmdLine] = (action, p);
            }
            return p;
        }

        /// <summary>
        /// removes a command line
        /// </summary>
        /// <param name="cmdLine">the command line to remove</param>
        public void Remove(string cmdLine)
        {
            lock (commands)
            {
                commands.Remove(cmdLine);
            }
        }

        /// <summary>
        /// Clears the bottom of the console, reduces buffer if necessary, increases with windowheight
        /// </summary>
        /// <param name="indictor">written at the lower right corner</param>
        /// <param name="bgColor">background color</param>
        /// <param name="fgColor">foreground color</param>
        public void NextPage(ConsoleColor fgColor = ConsoleColor.Gray, ConsoleColor bgColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = fgColor;
            Console.BackgroundColor = bgColor;
            // remember where the cursor was on the screen
            var posInWindow = Console.CursorTop - Console.WindowTop;
            // blank string covering the whole screen
            var sl = Console.BufferWidth * Console.WindowHeight;
            var s = new string(' ', sl);
            Console.CursorLeft = 0;
            // writing might move top content out of the buffer making space for new
            Console.Write(s);
            Console.CursorLeft = 0;
            var backPos = Console.CursorTop - Console.WindowHeight;
            if (backPos < 0)
                backPos = 0;
            Console.CursorTop = backPos;
            // cursor is back to where it was in the printout, however, the top part might have been 
            // remembering lastRowPos for the pager
            lastRowPos = Console.CursorTop;
            // scrolling the window back, need to calculate it from current cursor position
            try
            {

                var backWPos = lastRowPos - posInWindow;
                if (backWPos < 0)
                    backWPos = 0;
                Console.WindowTop = backWPos;
            }
            catch (ArgumentOutOfRangeException aore)
            {
                ExLog("handled by increasing buffer height", aore);
                Console.BufferHeight += Console.WindowHeight;
                Console.WindowTop = lastRowPos - posInWindow;
            }
        }

        /// <summary>
        /// This methods moves the curser up and down according to keypresses Arrow up/down, page up/down
        /// Any other key will end the cursor movement, also when idling too long.
        /// </summary>
        /// <param name="key">the key already pressed</param>
        public void GoUp(ConsoleKey key)
        {
            lock (Console.Out)
            {
                int oldCursorTop = Console.CursorTop;
                int wh = Console.WindowHeight;
                int idle = 0;
                bool stop = false;
                while (idle < maxIdle)
                {
                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                            if (Console.CursorTop > 0)
                                Console.CursorTop -= 1;
                            break;
                        case ConsoleKey.DownArrow:
                            if (Console.CursorTop < Console.BufferHeight - 1)
                                Console.CursorTop += 1;
                            break;
                        case ConsoleKey.PageUp:
                            if (Console.CursorTop > wh / 2)
                                Console.CursorTop -= wh / 2;
                            break;
                        case ConsoleKey.PageDown:
                            if (Console.CursorTop < Console.BufferHeight - wh / 2 - 1)
                                Console.CursorTop += wh / 2;
                            break;
                        default:
                            // any other key will end
                            stop = true;
                            break;
                    }
                    if (stop)
                        break;
                    for (idle = 0; idle < maxIdle; idle++)
                    {
                        if (Console.KeyAvailable)
                        {
                            key = Console.ReadKey(true).Key;
                            break;
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                    // if no key was pressed, then idle==maxIdle
                }
                Console.CursorTop = oldCursorTop;
                Console.CursorLeft = 0;
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
            Action a = () => { running = false; };
            Add("Exit application", a, 2);
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
                    if (!Console.KeyAvailable)
                        continue; // someone else took the keypress during the lock
                    var key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                        case ConsoleKey.PageUp:
                            GoUp(key.Key);
                            continue;
                    }
                    // got something but not movement
                    if (Console.CursorLeft > 0)
                        Console.WriteLine();
                    // now have space to print all commands
                    // remembering the positions
                    NextPage(ConsoleColor.White, ConsoleColor.DarkGreen);
                    var oldWindowTop = Console.WindowTop;
                    var oldCursorTop = Console.CursorTop;
                    // scrolls windows down a bit, so cursor is on top
                    try
                    {
                        Console.WindowTop = Console.CursorTop;
                    }
                    catch (ArgumentOutOfRangeException aore)
                    {
                        ExLog("handled", aore);
                        Console.BufferHeight += Console.WindowHeight;
                        Console.WindowTop = Console.CursorTop;
                    }
                    // working with those variables
                    commandPos = 0;
                    var cmdList = new List<string>();
                    var keystrokes = string.Empty;
                    bool filterAnew = false;
                    var positions = new Dictionary<string, List<int>>();
                    // entering loop with a key
                    while (true)
                    {
                        if (key.Key == ConsoleKey.Enter)
                        {
                            Console.CursorTop = oldCursorTop;
                            Console.WindowTop = oldWindowTop;
                            Console.CursorLeft = 0;
                            NextPage();
                            // Thread.Sleep(500);
                            if (string.IsNullOrWhiteSpace(keystrokes))
                                break;
                            if (commandPos < cmdList.Count)
                            {
                                currentCommand = cmdList[commandPos];
                                Action cmdAction;
                                lock (commands)
                                {
                                    var cmd = commands[currentCommand];
                                    cmd.priority = maxPriority + 1;
                                    cmdAction = cmd.ac;
                                }
                                // writing executed command on screen
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write((char)187);
                                Console.WriteLine(currentCommand);
                                Console.ResetColor();
                                cmdAction();
                            }
                            else
                            {
                                Console.Error.WriteLine($"mismatch between pos={commandPos} and list length={cmdList.Count}");
                                break;
                            }
                            break;
                        }
                        else if (key.Key == ConsoleKey.Escape)
                        {
                            Console.CursorTop = oldCursorTop;
                            Console.WindowTop = oldWindowTop;
                            Console.CursorLeft = 0;
                            NextPage();
                            break;
                        }
                        else if (key.Key == ConsoleKey.DownArrow)
                        {
                            ++commandPos;
                        }
                        else if (key.Key == ConsoleKey.UpArrow)
                        {
                            --commandPos;
                        }
                        else if (key.Key == ConsoleKey.Home)
                        {
                            commandPos = 0;
                        }
                        else if (key.Key == ConsoleKey.Backspace)
                        {
                            commandPos = 0;
                            cmdList.Clear();
                            if (keystrokes.Length > 0)
                                keystrokes = keystrokes.Substring(0, keystrokes.Length - 1);
                        }
                        else if (Char.IsLetterOrDigit(key.KeyChar))
                        {
                            keystrokes += key.KeyChar;
                            filterAnew = true;
                        }
                        else
                        {
                            // the part of || okChars.Contains(key.KeyChar))
                            foreach (char c in okChars)
                            {
                                if (c == key.KeyChar)
                                {
                                    keystrokes += key.KeyChar;
                                    filterAnew = true;
                                    break;
                                }
                            }
                        }
                        if (cmdList.Count == 0)
                        {
                            // initialize the list
                            cmdList.AddRange(commands.Keys);
                            cmdList.Sort(SortCommands);
                            filterAnew = true;
                        }
                        if (filterAnew)
                        {
                            for (int i = 0; i < cmdList.Count;)
                            {
                                var cmd = cmdList[i];
                                var posList = CheckString(cmd, keystrokes);
                                if (posList.Count != keystrokes.Length)
                                    cmdList.RemoveAt(i);
                                else
                                {
                                    positions[cmd] = posList;
                                    ++i;
                                }
                            }
                            if (cmdList.Count == 0)
                            {
                                Console.CursorTop = oldCursorTop;
                                NextPage(ConsoleColor.Yellow, ConsoleColor.DarkRed);
                                Console.WriteLine($"no matching command for {keystrokes}");
                                keystrokes = string.Empty;
                                key = Console.ReadKey(true);
                                continue;
                            }
                        }
                        if (commandPos >= cmdList.Count)
                            commandPos = cmdList.Count - 1;
                        if (commandPos < 0)
                            commandPos = 0;
                        var wh = Console.WindowHeight - 1;
                        // int skip = int.Max(0, int.Min(commandPos - wh / 2, cmdList.Count - wh));
                        int skip = commandPos - wh / 2;
                        int x = cmdList.Count - wh;
                        if (x < skip) skip = x;
                        if (skip < 0) skip = 0;
                        //Thread.Sleep(500);
                        Console.CursorTop = oldCursorTop;
                        NextPage(ConsoleColor.Gray, ConsoleColor.DarkBlue);
                        oldWindowTop = Console.CursorTop;
                        //Thread.Sleep(500);
                        for (int i = 0; i < wh; ++i)
                        {
                            // if another key is pressed, abandon printing out the remaining parts
                            if (Console.KeyAvailable)
                                break;
                            if (skip + i >= cmdList.Count)
                                break;
                            var cmd = cmdList[skip + i];
                            List<int> posList;
                            try
                            {
                                posList = positions[cmd];
                            }
                            catch (Exception ex)
                            {
                                ExLog("should not happen",ex);
                                // this should not happen
                                posList = new List<int>();
                            }
                            int pos = 0;
                            Console.CursorLeft = 1; // one space for indicator
                            foreach (int p in posList)
                            {
                                if (p > pos)
                                {
                                    Console.ForegroundColor = ConsoleColor.Blue;
                                    Console.Write(cmd.Substring(pos, p - pos));
                                }
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.Write(cmd.Substring(p, 1));
                                pos = p + 1;
                            }
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine(cmd.Substring(pos));
                        }
                        // marking the current selected command
                        Console.CursorTop = oldWindowTop + commandPos - skip;
                        Console.CursorLeft = 0;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write((char)187);
                        Console.CursorLeft = 0;
                        //Console.CursorTop = oldCursorTop;
                        // reading new key for the loop
                        key = Console.ReadKey(true);
                    }
                }
            } // while running
            // Console.WriteLine("loop ended, back to normal");
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
        /// sorts the commands according to preference (higher first), then alphabetically
        /// </summary>
        /// <param name="cmd1"></param>
        /// <param name="cmd2"></param>
        /// <returns>-1,0,1 like Comparison</returns>
        private int SortCommands(string cmd1, string cmd2)
        {
            var ca1 = commands[cmd1];
            var ca2 = commands[cmd2];
            if (ca1.priority > ca2.priority)
                return -1;
            if (ca2.priority > ca1.priority)
                return 1;
            return cmd1.CompareTo(cmd2);
        }

        public string Pager()
        {
            string response = string.Empty;
            if (!Console.KeyAvailable && Console.CursorTop < lastRowPos + Console.WindowHeight - 2)
                return string.Empty;
            while (true)
            {
                var k = Console.ReadKey(true);
                switch (k.Key)
                {
                    case ConsoleKey.Enter:
                        {
                            // if anything is already received, take next line
                            if (!string.IsNullOrEmpty(response))
                                Console.WriteLine();
                            NextPage(Console.ForegroundColor, Console.BackgroundColor);
                            return response;
                        }
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.PageUp:
                    case ConsoleKey.PageDown:
                        GoUp(k.Key);
                        break;
                    case ConsoleKey.Escape:
                        Console.WriteLine();
                        return string.Empty;
                    case ConsoleKey.Delete:
                        response = string.Empty;
                        break;
                    case ConsoleKey.Backspace:
                        response = response.Substring(0, response.Length - 1);
                        break;
                    default:
                        response += k.KeyChar;
                        break;
                }
                Console.CursorLeft = 0;
                Console.Write($"{response}  \b\b");
            }

        }

        public void Scan4Commands()
        {
            var cmdLineAssemblyName = this.GetType().Assembly.GetName();
            int priority = 0;
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
                                        priority = Add(attr.CmdLine, ac, priority);
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
                    ExLog("uncatched",ex);
                }

            }
        }
    }
}
