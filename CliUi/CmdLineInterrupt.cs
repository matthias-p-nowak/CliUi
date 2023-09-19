using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CliUi
{
    [Serializable]
    public class CmdLineInterrupt : Exception
    {
        public readonly List<string> Words;
        public readonly List<int> Numbers;

        public CmdLineInterrupt()
        {
        }

        public CmdLineInterrupt(string message) : base(message)
        {
        }

        public CmdLineInterrupt(int number)
        {
            Words = new List<string>();
            Numbers = new List<int>(1)
            {
                number
            };
        }

        public CmdLineInterrupt(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CmdLineInterrupt(List<string> txts, List<int> numbers) : base($"user response {txts}, {numbers}")
        {
            this.Words = txts;
            this.Numbers = numbers;
        }


        protected CmdLineInterrupt(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}