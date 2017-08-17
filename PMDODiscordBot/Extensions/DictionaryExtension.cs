using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Scripting.Runtime;

namespace CSharpDewott.Extensions
{
    public static class DictionaryExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1">Key of the KeyValuePair.</typeparam>
        /// <typeparam name="T2">Value of the KeyValuePair.</typeparam>
        /// <param name="sourceDictionary"></param>
        /// <param name="inputDictionary">Dictionary to add to the source Dictionary.</param>
        /// <param name="deleteDuplicateEntries">If true, deletes duplicate entries in the <see cref="inputDictionary"/>. Otherwise, throws an <see cref="ArgumentException"/> if there are duplicates.</param>
        public static Dictionary<T1, T2> AddRange<T1, T2>([NotNull] this Dictionary<T1, T2> sourceDictionary, [NotNull] Dictionary<T1, T2> inputDictionary, bool deleteDuplicateEntries = false)
        {
            if (sourceDictionary.Keys.Any(inputDictionary.ContainsKey))
            {
                if (deleteDuplicateEntries)
                {
                    inputDictionary = inputDictionary.RemoveDuplicates(sourceDictionary);
                }
                else
                {
                    throw new ArgumentException("Input dictionary can not contain the same keys as the source.");
                }
            }

            return sourceDictionary.Concat(inputDictionary).ToDictionary(e => e.Key, e => e.Value);
        }

        private static Dictionary<T1, T2> RemoveDuplicates<T1, T2>([NotNull] this Dictionary<T1, T2> sourceDictionary, [NotNull] Dictionary<T1, T2> inputDictionary)
        {
            return sourceDictionary.Where(e => !inputDictionary.ContainsKey(e.Key)).ToDictionary(e => e.Key, e => e.Value);
        }
    }
}
