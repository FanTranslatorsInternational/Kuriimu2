using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.FileSystem2
{
    public static class Common
    {
        /// <summary>
        /// Unify all directory separators in a given path.
        /// </summary>
        /// <param name="path">RelativePath to unify.</param>
        /// <returns>Unified path.</returns>
        public static string UnifyPath(string path)
        {
            return path.Replace((System.IO.Path.DirectorySeparatorChar == '\\' ? '/' : '\\'), System.IO.Path.DirectorySeparatorChar);
        }
    }
}
