using System.IO;

namespace CSharpDewott.IO
{
    public static class FileHelper
    {
        /// <summary>
        /// Combines strings into file path, creates resulting file on disk if it does not already exist, then returns the file as a <see cref="string"/>.
        /// </summary>
        /// <param name="paths">Array of parts of the path to create file from.</param>
        /// <returns>Resulting directory.</returns>
        public static string CreateIfDoesNotExist(params string[] paths)
        {
            string pathBuilder = paths[0];
            for (int i = 1; i < paths.Length; i++)
            {
                Directory.CreateDirectory(pathBuilder);

                pathBuilder = Path.Combine(pathBuilder, paths[i]);
            }

            if (!File.Exists(Path.Combine(paths)))
            {
                File.Create(Path.Combine(paths)).Dispose();
            }

            return Path.Combine(paths);
        }
    }
}
