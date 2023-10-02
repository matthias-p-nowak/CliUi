using System;

namespace CliUi
{
    /// <summary>
    /// Simplifies save/restore when writing text somewhere else
    /// </summary>
    public class SavedCursor
    {
        internal int cl;
        internal int ct;
        internal ConsoleColor cfg;
        internal ConsoleColor cbg;

        /// <summary>
        /// Restores cursor position and colors
        /// </summary>
        public void Restore()
        {
            Console.ForegroundColor = cfg;
            Console.BackgroundColor = cbg;
            Console.CursorLeft = cl;
            Console.CursorTop= ct;
        }
    }
}