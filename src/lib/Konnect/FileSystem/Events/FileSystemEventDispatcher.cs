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

using System.Collections.Concurrent;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.FileSystem.Events;
using Konnect.Contract.FileSystem;

namespace Konnect.FileSystem.Events;

/// <summary>
/// Stores <see cref="Watcher.FileSystemWatcher"/> instances to dispatch events to. Events are
/// called on a separate thread.
/// </summary>
/// <typeparam name="T">The <see cref="Watcher.FileSystemWatcher"/> type to store.</typeparam>
public class FileSystemEventDispatcher<T> : IDisposable
    where T : Watcher.FileSystemWatcher
{
    private readonly Thread _dispatchThread;
    private readonly BlockingCollection<Action> _dispatchQueue;
    private readonly CancellationTokenSource _dispatchCts;
    private readonly List<T> _watchers;

    public FileSystemEventDispatcher(IFileSystem fileSystem)
    {
        FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _dispatchThread = new Thread(DispatchWorker)
        {
            Name = "FileSystem Event Dispatch",
            IsBackground = true
        };

        _dispatchQueue = new BlockingCollection<Action>(16);
        _dispatchCts = new CancellationTokenSource();
        _watchers = new List<T>();

        _dispatchThread.Start();
    }

    public IFileSystem FileSystem { get; }

    ~FileSystemEventDispatcher()
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
        _dispatchCts?.Cancel();
        _dispatchThread?.Join();

        if (!disposing)
        {
            return;
        }

        _dispatchQueue.CompleteAdding();

        lock (_watchers)
        {
            foreach (var watcher in _watchers)
            {
                watcher.Dispose();
            }

            _watchers.Clear();
        }

        _dispatchQueue.Dispose();
    }

    /// <summary>
    /// Adds a <see cref="Watcher.FileSystemWatcher"/> instance to dispatch events to.
    /// </summary>
    /// <param name="watcher">Instance to add.</param>
    public void Add(T watcher)
    {
        lock (_watchers)
        {
            _watchers.Add(watcher);
        }
    }

    /// <summary>
    /// Returns all watchers in this dispatcher.
    /// </summary>
    /// <returns>The watchers of this dispatcher.</returns>
    public IList<T> Get()
    {
        lock (_watchers)
        {
            return _watchers.ToArray();
        }
    }

    /// <summary>
    /// Removes a <see cref="Watcher.FileSystemWatcher"/> instance to stop dispatching events.
    /// </summary>
    /// <param name="watcher">Instance to remove.</param>
    public void Remove(T watcher)
    {
        lock (_watchers)
        {
            _watchers.Remove(watcher);
        }
    }

    /// <summary>
    /// Raise the <see cref="IFileSystemWatcher.Opened"/> event on watchers.
    /// </summary>
    /// <param name="path">Absolute path to the opened file.</param>
    public void RaiseOpened(UPath path)
    {
        var args = new FileOpenedEventArgs
        {
            OpenedPath = path
        };
        Dispatch(args, (w, a) => w.RaiseOpened(a));
    }

    /// <summary>
    /// Raise the <see cref="IFileSystemWatcher.Changed"/> event on watchers.
    /// </summary>
    /// <param name="path">Absolute path to the changed file or directory.</param>
    public void RaiseChanged(UPath path)
    {
        var args = new FileChangedEventArgs
        {
            FileSystem = FileSystem,
            ChangeType = WatcherChangeTypes.Changed,
            FullPath = path
        };
        Dispatch(args, (w, a) => w.RaiseChanged(a));
    }

    /// <summary>
    /// Raise the <see cref="IFileSystemWatcher.Created"/> event on watchers.
    /// </summary>
    /// <param name="path">Absolute path to the new file or directory.</param>
    public void RaiseCreated(UPath path)
    {
        var args = new FileChangedEventArgs
        {
            FileSystem = FileSystem,
            ChangeType = WatcherChangeTypes.Created,
            FullPath = path
        };
        Dispatch(args, (w, a) => w.RaiseCreated(a));
    }

    /// <summary>
    /// Raise the <see cref="IFileSystemWatcher.Deleted"/> event on watchers.
    /// </summary>
    /// <param name="path">Absolute path to the changed file or directory.</param>
    public void RaiseDeleted(UPath path)
    {
        var args = new FileChangedEventArgs
        {
            FileSystem = FileSystem,
            ChangeType = WatcherChangeTypes.Deleted,
            FullPath = path
        };
        Dispatch(args, (w, a) => w.RaiseDeleted(a));
    }

    /// <summary>
    /// Raise the <see cref="IFileSystemWatcher.Renamed"/> event on watchers.
    /// </summary>
    /// <param name="newPath">Absolute path to the new file or directory.</param>
    /// <param name="oldPath">Absolute path to the old file or directory.</param>
    public void RaiseRenamed(UPath newPath, UPath oldPath)
    {
        var args = new FileRenamedEventArgs
        {
            FileSystem = FileSystem,
            ChangeType = WatcherChangeTypes.Renamed,
            FullPath = newPath,
            OldFullPath = oldPath
        };
        Dispatch(args, (w, a) => w.RaiseRenamed(a));
    }

    /// <summary>
    /// Raise the <see cref="IFileSystemWatcher.Error"/> event on watchers.
    /// </summary>
    /// <param name="exception">Exception that occurred.</param>
    public void RaiseError(Exception exception)
    {
        var args = new FileSystemErrorEventArgs
        {
            Exception = exception
        };
        Dispatch(args, (w, a) => w.RaiseError(a), false);
    }

    private void Dispatch<TArgs>(TArgs eventArgs, Action<T, TArgs> handler, bool captureError = true)
        where TArgs : EventArgs
    {
        List<T> watchersSnapshot;
        lock (_watchers)
        {
            if (_watchers.Count == 0)
            {
                return;
            }

            watchersSnapshot = _watchers.ToList(); // TODO: reduce allocations
        }

        // The events should be called on a separate thread because the filesystem code
        // could be holding locks that must be released.
        _dispatchQueue.Add(() =>
        {
            foreach (var watcher in watchersSnapshot)
            {
                try
                {
                    handler(watcher, eventArgs);
                }
                catch (Exception e) when (captureError)
                {
                    RaiseError(e);
                }
            }
        });
    }

    // Worker runs on dedicated thread to call events
    private void DispatchWorker()
    {
        var ct = _dispatchCts.Token;

        try
        {
            foreach (var action in _dispatchQueue.GetConsumingEnumerable(ct))
            {
                action();
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }
}