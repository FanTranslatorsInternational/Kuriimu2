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

using System.IO;
using Kontract.Extensions;
using Kontract.Models.IO;

namespace Kontract.Interfaces.FileSystem.EventArgs
{
    /// <summary>
    /// Represents a file or directory rename event.
    /// </summary>
    /// <inheritdoc />
    public class FileRenamedEventArgs : FileChangedEventArgs
    {
        /// <summary>
        /// Absolute path to the old location of the file or directory.
        /// </summary>
        public UPath OldFullPath { get; }

        /// <summary>
        /// Old name of the file or directory.
        /// </summary>
        public string OldName { get; }

        public FileRenamedEventArgs(IFileSystem fileSystem, WatcherChangeTypes changeType, UPath fullPath, UPath oldFullPath)
            : base(fileSystem, changeType, fullPath)
        {
            fullPath.AssertNotNull(nameof(oldFullPath));
            fullPath.AssertAbsolute(nameof(oldFullPath));

            OldFullPath = oldFullPath;
            OldName = oldFullPath.GetName();
        }
    }
}
