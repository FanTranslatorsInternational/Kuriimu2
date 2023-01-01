// Copyright(c) 2017-2019, Alexandre Mutel
// All rights reserved.

// Redistribution and use in source and binary forms, with or without modification
// , are permitted provided that the following conditions are met:

// 1. Redistributions of source code must retain the above copyright notice, this
// list of conditions and the following disclaimer.

// 2. Redistributions in binary form must reproduce the above copyright notice,
// this list of conditions and the following disclaimer in the documentation
// and/or other materials provided with the distribution.

// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

// Modifications by onepiecefreak are as follows:
// - Documentation added

using System.IO;
using Kontract.Models.FileSystem;

namespace Kore.FileSystem
{
    /// <summary>
    /// An exception creator for file systems.
    /// </summary>
    internal static class FileSystemExceptionHelper
    {
        /// <summary>
        /// Creates a <see cref="FileNotFoundException"/>.
        /// </summary>
        /// <param name="path">The path to the file not found.</param>
        /// <returns>The exception.</returns>
        public static FileNotFoundException NewFileNotFoundException(UPath path)
        {
            return new FileNotFoundException($"Could not find file `{path}`.");
        }

        /// <summary>
        /// Creates a <see cref="DirectoryNotFoundException"/>.
        /// </summary>
        /// <param name="path">The path to the directory not found.</param>
        /// <returns>The exception.</returns>
        public static DirectoryNotFoundException NewDirectoryNotFoundException(UPath path)
        {
            return new DirectoryNotFoundException($"Could not find a part of the path `{path}`.");
        }

        /// <summary>
        /// Creates a <see cref="IOException"/>.
        /// </summary>
        /// <param name="path">The path of the directory being not empty.</param>
        /// <returns>The exception.</returns>
        public static IOException NewDirectoryIsNotEmpty(UPath path)
        {
            return new IOException($"The destination path `{path}` is not empty.");
        }

        /// <summary>
        /// Creates a <see cref="IOException"/>.
        /// </summary>
        /// <param name="path">The path of the directory that already exists.</param>
        /// <returns>The exception.</returns>
        public static IOException NewDestinationDirectoryExistException(UPath path)
        {
            return new IOException($"The destination path `{path}` is an existing directory.");
        }

        /// <summary>
        /// Creates a <see cref="IOException"/>.
        /// </summary>
        /// <param name="path">The path of the file that already exists.</param>
        /// <returns>The exception.</returns>
        public static IOException NewDestinationFileExistException(UPath path)
        {
            return new IOException($"The destination path `{path}` is an existing file.");
        }
    }
}
