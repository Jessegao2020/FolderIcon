using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderIcon
{
    public class PathHelper
    {
        public static bool IsFolderPath(string path)
        {
            if (path != null)
            {
                // Check for invalid path characters
                foreach (char c in path)
                {
                    if (Path.GetInvalidPathChars().Contains(c))
                    {
                        throw new ArgumentException("Invalid character found in the path.");
                    }
                }

                // Trim any trailing directory separators
                path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // Check if the path exists and is a directory
                return Directory.Exists(path);
            }

            return false;
        }
    }
}
