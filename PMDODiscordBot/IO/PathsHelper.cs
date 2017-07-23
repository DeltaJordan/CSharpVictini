using System.IO;

namespace CSharpDewott.IO
{
    public static class PathsHelper
    {
        /// <summary>
        /// Combines strings into directory, creates resulting directory on disk if it does not already exist, then returns the directory as a <see cref="string"/>.
        /// </summary>
        /// <param name="directory">Array of parts of the path to create directory from.</param>
        /// <returns>Resulting directory.</returns>
        public static string CreateIfDoesNotExist(params string[] directory)
        {
            if (!Directory.Exists(Path.Combine(directory)))
            {
                Directory.CreateDirectory(Path.Combine(directory));
            }

            return Path.Combine(directory);
        }

    }
}
