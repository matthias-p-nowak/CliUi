using System;
using System.Runtime.Serialization;

namespace CliUi
{
    [Serializable]
    public class CmdLineInterrupt : Exception
    {
        public readonly string Text;
        public readonly int Number;

        public CmdLineInterrupt()
        {
        }

        public CmdLineInterrupt(string message) : base(message)
        {
        }

        public CmdLineInterrupt(string str, int number) : base($"User returned {str},{number}")
        {
            this.Text = str;
            this.Number = number;
        }

        public CmdLineInterrupt(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CmdLineInterrupt(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}