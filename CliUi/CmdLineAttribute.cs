using System;
using System.Collections.Generic;
using System.Text;

namespace CliUi
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CmdLineAttribute: Attribute
    {
        public string CmdLine;

        public CmdLineAttribute(string cmdLine)
        {
            CmdLine = cmdLine;
        }
    }

    [AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
    public class CmdLineAdderAttribute: Attribute
    {

    }

}
