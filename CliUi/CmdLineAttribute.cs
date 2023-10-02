using System;
using System.Collections.Generic;
using System.Text;

namespace CliUi
{
    /// <summary>
    /// denotes a single <code>public static void</code> method which gets executed when the command line is entered
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CmdLineAttribute: Attribute
    {
        /// <summary>
        /// The string that represents the command line
        /// </summary>
        public string CmdLine;
        /// <summary>
        /// the command line the user has to enter
        /// </summary>
        public CmdLineAttribute(string cmdLine)
        {
            CmdLine = cmdLine;
        }
    }

    /// <summary>
    /// a <code>public static void method(CmdLineUi ui)</code> method that is called during the <code>Scan4Commands</code> method.
    /// </summary>
    [AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
    public class CmdLineAdderAttribute: Attribute
    {

    }

}
