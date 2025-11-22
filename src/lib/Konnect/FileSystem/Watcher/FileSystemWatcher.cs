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

using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.FileSystem.Events;
using Konnect.Contract.FileSystem;
using Konnect.Extensions;

namespace Konnect.FileSystem.Watcher;

public class FileSystemWatcher : IFileSystemWatcher
{
    /// <inheritdoc />
    public event EventHandler<FileOpenedEventArgs> Opened;

    /// <inheritdoc />
    public event EventHandler<FileChangedEventArgs> Changed;

    /// <inheritdoc />
    public event EventHandler<FileChangedEventArgs> Created;

    /// <inheritdoc />
    public event EventHandler<FileChangedEventArgs> Deleted;

    /// <inheritdoc />
    public event EventHandler<FileSystemErrorEventArgs> Error;

    /// <inheritdoc />
    public event EventHandler<FileRenamedEventArgs> Renamed;

    /// <summary>
    /// Event for when this watcher is disposed.
    /// </summary>
    public event EventHandler<EventArgs> Disposed;

    /// <inheritdoc />
    public IFileSystem FileSystem { get; }

    /// <inheritdoc />
    public UPath Path { get; }

    public FileSystemWatcher(IFileSystem fileSystem, UPath path)
    {
        if (fileSystem == null)
            throw new ArgumentNullException(nameof(fileSystem));

        path.AssertAbsolute();

        FileSystem = fileSystem;
        Path = path;
    }

    ~FileSystemWatcher()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        RaiseDisposed();
    }

    /// <summary>
    /// Raises the <see cref="Opened"/> event. 
    /// </summary>
    /// <param name="args">Arguments for the event.</param>
    public void RaiseOpened(FileOpenedEventArgs args)
    {
        if (!ShouldRaiseEvent(args))
        {
            return;
        }

        Opened?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the <see cref="Changed"/> event. 
    /// </summary>
    /// <param name="args">Arguments for the event.</param>
    public void RaiseChanged(FileChangedEventArgs args)
    {
        if (!ShouldRaiseEvent(args))
        {
            return;
        }

        Changed?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the <see cref="Created"/> event. 
    /// </summary>
    /// <param name="args">Arguments for the event.</param>
    public void RaiseCreated(FileChangedEventArgs args)
    {
        if (!ShouldRaiseEvent(args))
        {
            return;
        }

        Created?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the <see cref="Deleted"/> event. 
    /// </summary>
    /// <param name="args">Arguments for the event.</param>
    public void RaiseDeleted(FileChangedEventArgs args)
    {
        if (!ShouldRaiseEvent(args))
        {
            return;
        }

        Deleted?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the <see cref="Error"/> event. 
    /// </summary>
    /// <param name="args">Arguments for the event.</param>
    public void RaiseError(FileSystemErrorEventArgs args)
    {
        Error?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the <see cref="Renamed"/> event. 
    /// </summary>
    /// <param name="args">Arguments for the event.</param>
    public void RaiseRenamed(FileRenamedEventArgs args)
    {
        if (!ShouldRaiseEvent(args))
        {
            return;
        }

        Renamed?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the <see cref="Disposed"/> event.
    /// </summary>
    private void RaiseDisposed()
    {
        Disposed?.Invoke(this, new EventArgs());
    }

    private bool ShouldRaiseEvent(FileChangedEventArgs args)
    {
        return ShouldRaiseEventImpl(args);
    }

    private bool ShouldRaiseEvent(FileOpenedEventArgs args)
    {
        return ShouldRaiseEventImpl(args);
    }

    /// <summary>
    /// Checks if the event should be raised for the given arguments. Default implementation
    /// checks if the <see cref="FileChangedEventArgs.FullPath"/> is contained in <see cref="Path"/>.
    /// </summary>
    /// <param name="args">Arguments for the event.</param>
    /// <returns>True if the event should be raised, false to ignore it.</returns>
    protected virtual bool ShouldRaiseEventImpl(FileChangedEventArgs args)
    {
        return args.FullPath.IsInDirectory(Path, true);
    }

    /// <summary>
    /// Checks if the event should be raised for the given arguments. Default implementation
    /// checks if the <see cref="FileOpenedEventArgs.OpenedPath"/> is contained in <see cref="Path"/>.
    /// </summary>
    /// <param name="args">Arguments for the event.</param>
    /// <returns>True if the event should be raised, false to ignore it.</returns>
    protected virtual bool ShouldRaiseEventImpl(FileOpenedEventArgs args)
    {
        return args.OpenedPath.IsInDirectory(Path, true);
    }

    /// <summary>
    /// Listens to events from another <see cref="IFileSystemWatcher"/> instance to forward them
    /// into this instance.
    /// </summary>
    /// <param name="watcher">Other instance to listen to.</param>
    protected void RegisterEvents(IFileSystemWatcher watcher)
    {
        if (watcher == null)
        {
            throw new ArgumentNullException(nameof(watcher));
        }

        watcher.Changed += OnChanged;
        watcher.Created += OnCreated;
        watcher.Deleted += OnDeleted;
        watcher.Error += OnError;
        watcher.Renamed += OnRenamed;
    }

    /// <summary>
    /// Stops listening to events from another <see cref="IFileSystemWatcher"/>.
    /// </summary>
    /// <param name="watcher">Instance to remove event handlers from.</param>
    protected void UnregisterEvents(IFileSystemWatcher watcher)
    {
        if (watcher == null)
        {
            throw new ArgumentNullException(nameof(watcher));
        }

        watcher.Changed -= OnChanged;
        watcher.Created -= OnCreated;
        watcher.Deleted -= OnDeleted;
        watcher.Error -= OnError;
        watcher.Renamed -= OnRenamed;
    }

    /// <summary>
    /// Attempts to convert paths from an existing event in another <see cref="IFileSystem"/> into
    /// this <see cref="FileSystem"/>. If this returns <c>null</c> the event will be discarded.
    /// </summary>
    /// <param name="pathFromEvent">Path from the other filesystem.</param>
    /// <returns>Path in this filesystem, or null if it cannot be converted.</returns>
    protected virtual UPath? TryConvertPath(UPath pathFromEvent)
    {
        return pathFromEvent;
    }

    private void OnChanged(object sender, FileChangedEventArgs args)
    {
        var newPath = TryConvertPath(args.FullPath);
        if (!newPath.HasValue)
        {
            return;
        }

        var newArgs = new FileChangedEventArgs
        {
            FileSystem = FileSystem,
            ChangeType = args.ChangeType,
            FullPath = newPath.Value
        };
        RaiseChanged(newArgs);
    }

    private void OnCreated(object sender, FileChangedEventArgs args)
    {
        var newPath = TryConvertPath(args.FullPath);
        if (!newPath.HasValue)
        {
            return;
        }

        var newArgs = new FileChangedEventArgs
        {
            FileSystem = FileSystem,
            ChangeType = args.ChangeType,
            FullPath = newPath.Value
        };
        RaiseCreated(newArgs);
    }

    private void OnDeleted(object sender, FileChangedEventArgs args)
    {
        var newPath = TryConvertPath(args.FullPath);
        if (!newPath.HasValue)
        {
            return;
        }

        var newArgs = new FileChangedEventArgs
        {
            FileSystem = FileSystem,
            ChangeType = args.ChangeType,
            FullPath = newPath.Value
        };
        RaiseDeleted(newArgs);
    }

    private void OnError(object sender, FileSystemErrorEventArgs args)
    {
        RaiseError(args);
    }

    private void OnRenamed(object sender, FileRenamedEventArgs args)
    {
        var newPath = TryConvertPath(args.FullPath);
        if (!newPath.HasValue)
        {
            return;
        }

        var newOldPath = TryConvertPath(args.OldFullPath);
        if (!newOldPath.HasValue)
        {
            return;
        }

        var newArgs = new FileRenamedEventArgs
        {
            FileSystem = FileSystem,
            ChangeType = args.ChangeType,
            FullPath = newPath.Value,
            OldFullPath = newOldPath.Value
        };
        RaiseRenamed(newArgs);
    }
}