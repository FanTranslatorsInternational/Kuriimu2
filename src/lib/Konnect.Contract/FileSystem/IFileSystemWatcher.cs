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

// Modifications made by onepiecefreak are as follows:
// - Add FileOpened event
// - Remove internal buffer
// - NotifyFilter
// - Remove EnableRaisingEvent
// - Remove Filter
// - Remove IncludeSubdirectories

using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.FileSystem.Events;

namespace Konnect.Contract.FileSystem;

/// <summary>
/// Interface for a filesystem watcher.
/// </summary>
/// <inheritdoc />
public interface IFileSystemWatcher : IDisposable
{
    /// <summary>
    /// Event for when a file was opened.
    /// </summary>
    event EventHandler<FileOpenedEventArgs> Opened;

    /// <summary>
    /// Event for when a file or directory changes.
    /// </summary>
    event EventHandler<FileChangedEventArgs> Changed;

    /// <summary>
    /// Event for when a file or directory is created.
    /// </summary>
    event EventHandler<FileChangedEventArgs> Created;

    /// <summary>
    /// Event for when a file or directory is deleted.
    /// </summary>
    event EventHandler<FileChangedEventArgs> Deleted;

    /// <summary>
    /// Event for when the filesystem encounters an error.
    /// </summary>
    event EventHandler<FileSystemErrorEventArgs> Error;

    /// <summary>
    /// Event for when a file or directory is renamed.
    /// </summary>
    event EventHandler<FileRenamedEventArgs> Renamed;

    /// <summary>
    /// The <see cref="IFileSystem"/> this instance is watching.
    /// </summary>
    IFileSystem FileSystem { get; }

    /// <summary>
    /// The path being watched by the filesystem.
    /// </summary>
    UPath Path { get; }
}