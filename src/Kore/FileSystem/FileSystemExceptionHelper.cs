// Copyright (c) 2017-2019, Alexandre Mutel
// All rights reserved.
// Modifications by onepiecefreak are as follows:
// - Documentation added

using System.IO;
using Kontract.Models;

namespace Kore.FileSystem
{
    /// <summary>
    /// An exception thrower for file systems.
    /// </summary>
    internal static class FileSystemExceptionHelper
    {
        /// <summary>
        /// Throws a <see cref="FileNotFoundException"/>.
        /// </summary>
        /// <param name="path">The path to the file not found.</param>
        /// <returns>The exception.</returns>
        public static FileNotFoundException NewFileNotFoundException(UPath path)
        {
            return new FileNotFoundException($"Could not find file `{path}`.");
        }

        /// <summary>
        /// Throws a <see cref="DirectoryNotFoundException"/>.
        /// </summary>
        /// <param name="path">The path to the directory not found.</param>
        /// <returns>The exception.</returns>
        public static DirectoryNotFoundException NewDirectoryNotFoundException(UPath path)
        {
            return new DirectoryNotFoundException($"Could not find a part of the path `{path}`.");
        }

        /// <summary>
        /// Throws a <see cref="IOException"/>.
        /// </summary>
        /// <param name="path">The path of the directory being not empty.</param>
        /// <returns>The exception.</returns>
        public static IOException NewDirectoryIsNotEmpty(UPath path)
        {
            return new IOException($"The destination path `{path}` is not empty.");
        }
    }
}
