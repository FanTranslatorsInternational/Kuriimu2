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

using System;
using System.IO;
using Kontract.Extensions;
using Kontract.Models.IO;

namespace Kontract.Interfaces.FileSystem.EventArgs
{
    /// <summary>
    /// The <see cref="EventArgs"/> base class for file and directory events. Used for
    /// <see cref="WatcherChangeTypes.Created"/>, <see cref="WatcherChangeTypes.Deleted"/>,
    /// and <see cref="WatcherChangeTypes.Changed"/>.
    /// </summary>
    /// <inheritdoc />
    public class FileChangedEventArgs : System.EventArgs
    {
        /// <summary>
        /// The type of change that occurred.
        /// </summary>
        public WatcherChangeTypes ChangeType { get; }

        /// <summary>
        /// The filesystem originating this change.
        /// </summary>
        public IFileSystem FileSystem { get; }

        /// <summary>
        /// Absolute path to the file or directory.
        /// </summary>
        public UPath FullPath { get; }

        /// <summary>
        /// Name of the file or directory.
        /// </summary>
        public string Name { get; }

        public FileChangedEventArgs(IFileSystem fileSystem, WatcherChangeTypes changeType, UPath fullPath)
        {
            if (fileSystem == null) 
                throw new ArgumentNullException(nameof(fileSystem));

            fullPath.AssertNotNull(nameof(fullPath));
            fullPath.AssertAbsolute(nameof(fullPath));

            FileSystem = fileSystem;
            ChangeType = changeType;
            FullPath = fullPath;
            Name = fullPath.GetName();
        }
    }
}
