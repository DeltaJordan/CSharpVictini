using System;

namespace CSharpDewott.Extensions
{
    public static class ConsoleHelper
    {

        /// <summary>
        /// Writes exception with full verbose
        /// </summary>
        /// <param name="exception"></param>
        public static void WriteLine(Exception exception)
        {
            Console.WriteLine(exception.Message);
            Console.WriteLine(exception.StackTrace);
            Console.WriteLine(exception.Source);
        }
    }
}
