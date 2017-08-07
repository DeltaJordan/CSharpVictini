using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpDewott.Extensions
{
    public static class StringExtension
    {

        public static bool Contains(this string input, params char[] chars)
        {
            return input.ToCharArray().Any(chars.Contains);
        }
    }
}
