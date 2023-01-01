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
using Kontract.Models.FileSystem;
using Kontract.Models.FileSystem.EventArgs;
using Kore.FileSystem.Implementations;

namespace Kore.FileSystem.Watcher
{
    class PhysicalFileSystemWatcher : FileSystemWatcher
    {
        private readonly PhysicalFileSystem _fileSystem;
        private readonly System.IO.FileSystemWatcher _watcher;

        public PhysicalFileSystemWatcher(PhysicalFileSystem fileSystem, UPath path):
            base(fileSystem, path)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _watcher = new System.IO.FileSystemWatcher(_fileSystem.ConvertPathToInternal(path))
            {
                Filter = "*"
            };

            _watcher.Changed += (sender, args) => RaiseChanged(Remap(args));
            _watcher.Created += (sender, args) => RaiseCreated(Remap(args));
            _watcher.Deleted += (sender, args) => RaiseDeleted(Remap(args));
            _watcher.Error += (sender, args) => RaiseError(Remap(args));
            _watcher.Renamed += (sender, args) => RaiseRenamed(Remap(args));
        }

        ~PhysicalFileSystemWatcher()
        {
            Dispose(false);
        }

        private FileChangedEventArgs Remap(FileSystemEventArgs args)
        {
            var newChangeType = args.ChangeType;
            var newPath = _fileSystem.ConvertPathFromInternal(args.FullPath);
            return new FileChangedEventArgs(FileSystem, newChangeType, newPath);
        }

        private FileSystemErrorEventArgs Remap(ErrorEventArgs args)
        {
            return new FileSystemErrorEventArgs(args.GetException());
        }

        private FileRenamedEventArgs Remap(RenamedEventArgs args)
        {
            var newChangeType = args.ChangeType;
            var newPath = _fileSystem.ConvertPathFromInternal(args.FullPath);
            var newOldPath = _fileSystem.ConvertPathFromInternal(args.OldFullPath);
            return new FileRenamedEventArgs(FileSystem, newChangeType, newPath, newOldPath);
        }
    }
}
