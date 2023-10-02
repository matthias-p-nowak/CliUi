using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CliUi
{
    /// <summary>
    /// Exception thrown by Pager when the user enters a line other than Return
    /// </summary>
    [Serializable]
    public class CmdLineInterrupt : Exception
    {
        /// <summary>
        /// list of letters
        /// </summary>
        public readonly List<string> Words;
        /// <summary>
        /// list of integers, enter as single number or as a range, multiple can be specified separated by ','
        /// </summary>
        public readonly List<int> Numbers;

 
        /// <summary>
        /// create an interupt that only contains one integer - like the default one
        /// </summary>
        /// <param name="number">the number to store</param>
        public CmdLineInterrupt(int number)
        {
            Words = new List<string>();
            Numbers = new List<int>(1)
            {
                number
            };
        }

      
        /// <summary>
        /// Create a new interrupt exception
        /// </summary>
        /// <param name="txts">the words to return</param>
        /// <param name="numbers">the number to return</param>
        public CmdLineInterrupt(List<string> txts, List<int> numbers) : base($"user response {txts}, {numbers}")
        {
            this.Words = txts;
            this.Numbers = numbers;
        }

   }
}