using System;
using System.Collections.Generic;
using System.Threading;

namespace CliUi
{
    /// <summary>
    /// The user interface class that maintains a dictionary over all command lines and actions.
    /// </summary>
    public class CmdLineUi
    { 
        /// <summary>
        /// singleton instance, lazy initialized
        /// </summary>
        private static Lazy<CmdLineUi> _instance = new Lazy<CmdLineUi>();
        /// <summary>
        /// provides access to a singleton instance, use of others is possible
        /// </summary>
        public static CmdLineUi Instance { get => _instance.Value; }

        /// <summary>
        /// console buffer is regularly extended and truncated when it exceeds this limit
        /// </summary>
        private int maxHeight = 8000;
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
        private int maxPriority = 2;
        /// <summary>
        /// .when false, the loop of executing commands will end
        /// </summary>
        private bool running=true;

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
            if(p>maxPriority)
                maxPriority= p;
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
        public void Clear(string indictor, ConsoleColor bgColor = ConsoleColor.Black, ConsoleColor fgColor = ConsoleColor.Gray)
        {
            lock (Console.Out)
            {
                Console.BufferWidth = Console.WindowWidth;
                Console.CursorLeft = 0;
                if (Console.BufferHeight > maxHeight)
                    Console.BufferHeight = maxHeight;
                var oldCursorTop = Console.CursorTop;
                var oldWindowTop = Console.WindowTop;
                try
                {
                    int newHeight = Console.CursorTop + Console.WindowHeight + 2;
                    if (Console.BufferHeight < newHeight)
                        Console.BufferHeight = newHeight;
                }
                catch
                {
                    Console.WriteLine("no new BufferHeight");
                }
                Console.BackgroundColor = bgColor;
                Console.ForegroundColor = fgColor;
                var es = new String(' ', Console.WindowWidth * Console.WindowHeight);
                Console.Write(es);
                if (!string.IsNullOrWhiteSpace(indictor))
                {
                    Console.CursorLeft = Console.WindowWidth - indictor.Length;
                    Console.Write(indictor);
                }
                Console.CursorLeft = 0;
                Console.CursorTop = oldCursorTop;
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
        /// Creates a list of matching positions
        /// </summary>
        /// <param name="heystack">the full string to match</param>
        /// <param name="keystrokes">the needle to find</param>
        /// <returns>a list of matching positions</returns>
        private static List<int> CheckString(string heystack, string keystrokes)
        {
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

    }
}
