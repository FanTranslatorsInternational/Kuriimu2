﻿// Copyright(c) 2017-2019, Alexandre Mutel
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Streams;
using Kontract.Models.FileSystem;
using Kore.Models.FileSystem;

namespace Kore.Implementation.FileSystem
{
    /// <summary>
    /// Provides an in-memory <see cref="IFileSystem"/> compatible with the way a real <see cref="PhysicalFileSystem"/> is working.
    /// </summary>
    public class MemoryFileSystem : FileSystem
    {
        // The locking strategy is based on https://www.kernel.org/doc/Documentation/filesystems/directory-locking

        private readonly DirectoryNode _rootDirectory;
        private readonly FileSystemNodeReadWriteLock _globalLock;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryFileSystem"/> class.
        /// </summary>
        public MemoryFileSystem(IStreamManager streamManager) :
            base(streamManager)
        {
            _rootDirectory = new DirectoryNode(this);
            _globalLock = new FileSystemNodeReadWriteLock();
        }

        /// <summary>
        /// Constructor used for deep cloning.
        /// </summary>
        /// <param name="copyFrom">The <see cref="MemoryFileSystem"/> to clone from.</param>
        /// <param name="streamManager">The <see cref="IStreamManager"/> for this file system.</param>
        protected MemoryFileSystem(MemoryFileSystem copyFrom, IStreamManager streamManager, IList<Watcher.FileSystemWatcher> watchers) :
            base(streamManager)
        {
            if (copyFrom == null) throw new ArgumentNullException(nameof(copyFrom));
            Debug.Assert(copyFrom._globalLock.IsLocked);
            _rootDirectory = (DirectoryNode)copyFrom._rootDirectory.Clone(null, null);
            _globalLock = new FileSystemNodeReadWriteLock();

            foreach (var watcher in watchers)
                GetOrCreateDispatcher().Add(watcher);
        }

        /// <inheritdoc />
        public override IFileSystem Clone(IStreamManager streamManager)
        {
            EnterFileSystemExclusive();
            try
            {
                return CloneImpl(streamManager);
            }
            finally
            {
                ExitFileSystemExclusive();
            }
        }

        protected virtual MemoryFileSystem CloneImpl(IStreamManager streamManager)
        {
            return new MemoryFileSystem(this, streamManager, GetOrCreateDispatcher().Get());
        }

        // ----------------------------------------------
        // Directory API
        // ----------------------------------------------

        /// <inheritdoc />
        public override bool CanCreateDirectories => true;

        /// <inheritdoc />
        public override bool CanDeleteDirectories => true;

        /// <inheritdoc />
        public override bool CanMoveDirectories => true;

        /// <inheritdoc />
        protected override void CreateDirectoryImpl(UPath path)
        {
            EnterFileSystemShared();
            try
            {
                CreateDirectoryNode(path);
                GetOrCreateDispatcher().RaiseCreated(path);
            }
            finally
            {
                ExitFileSystemShared();
            }
        }

        /// <inheritdoc />
        protected override bool DirectoryExistsImpl(UPath path)
        {
            if (path == UPath.Root)
            {
                return true;
            }

            EnterFileSystemShared();
            try
            {
                // NodeCheck doesn't take a lock, on the return node
                // but allows us to check if it is a directory or a file
                var result = EnterFindNode(path, FindNodeFlags.NodeCheck);
                try
                {
                    return result.Node is DirectoryNode;
                }
                finally
                {
                    ExitFindNode(result);
                }
            }
            finally
            {
                ExitFileSystemShared();
            }
        }

        /// <inheritdoc />
        protected override void MoveDirectoryImpl(UPath srcPath, UPath destPath)
        {
            MoveFileOrDirectory(srcPath, destPath, true);
        }

        /// <inheritdoc />
        protected override void DeleteDirectoryImpl(UPath path, bool isRecursive)
        {
            EnterFileSystemShared();
            try
            {
                var result = EnterFindNode(path, FindNodeFlags.KeepParentNodeExclusive | FindNodeFlags.NodeExclusive);

                var deleteRootDirectory = false;
                try
                {
                    AssertDirectory(result.Node, path);

                    if (result.Node.IsReadOnly)
                    {
                        throw new IOException($"Access to the path `{path}` is denied");
                    }

                    using (var locks = new ListFileSystemNodes(this))
                    {
                        TryLockExclusive(result.Node, locks, isRecursive, path);

                        // Check that files are not readonly
                        foreach (var lockFile in locks)
                        {
                            var node = lockFile.Value;

                            if (node.IsReadOnly)
                            {
                                throw new UnauthorizedAccessException($"Access to path `{path}` is denied.");
                            }
                        }

                        // We remove all elements
                        for (var i = locks.Count - 1; i >= 0; i--)
                        {
                            var lockFile = locks[i];
                            locks.RemoveAt(i);
                            lockFile.Value.DetachFromParent();
                            lockFile.Value.Dispose();

                            ExitExclusive(lockFile.Value);
                        }
                    }
                    deleteRootDirectory = true;
                }
                finally
                {
                    if (deleteRootDirectory)
                    {
                        result.Node.DetachFromParent();
                        result.Node.Dispose();
                    }

                    GetOrCreateDispatcher().RaiseDeleted(path);

                    ExitFindNode(result);
                }
            }
            finally
            {
                ExitFileSystemShared();
            }
        }

        // ----------------------------------------------
        // File API
        // ----------------------------------------------

        /// <inheritdoc />
        public override bool CanCreateFiles => true;

        /// <inheritdoc />
        public override bool CanCopyFiles => true;

        /// <inheritdoc />
        public override bool CanMoveFiles => true;

        /// <inheritdoc />
        public override bool CanReplaceFiles => true;

        /// <inheritdoc />
        public override bool CanDeleteFiles => true;

        /// <inheritdoc />
        protected override void CopyFileImpl(UPath srcPath, UPath destPath, bool overwrite)
        {
            EnterFileSystemShared();
            try
            {
                var srcResult = EnterFindNode(srcPath, FindNodeFlags.NodeShared);
                try
                {
                    // The source file must exist
                    var srcNode = srcResult.Node;
                    if (srcNode is DirectoryNode)
                    {
                        throw new UnauthorizedAccessException($"Cannot copy file. The path `{srcPath}` is a directory");
                    }
                    if (srcNode == null)
                    {
                        throw FileSystemExceptionHelper.NewFileNotFoundException(srcPath);
                    }

                    var destResult = EnterFindNode(destPath, FindNodeFlags.KeepParentNodeExclusive | FindNodeFlags.NodeExclusive);
                    var destFileName = destResult.Name;
                    var destDirectory = destResult.Directory;
                    var destNode = destResult.Node;
                    try
                    {
                        // The dest file may exist
                        if (destDirectory == null)
                        {
                            throw FileSystemExceptionHelper.NewDirectoryNotFoundException(destPath);
                        }

                        if (destNode is DirectoryNode)
                        {
                            throw new IOException($"The target file `{destPath}` is a directory, not a file.");
                        }

                        // If the destination is empty, we need to create it
                        if (destNode == null)
                        {
                            // Constructor copies and attaches to directory for us
                            var newFileNode = new FileNode(this, destDirectory, destFileName, (FileNode)srcNode);
                        }
                        else if (overwrite)
                        {
                            if (destNode.IsReadOnly)
                            {
                                throw new UnauthorizedAccessException($"Access to path `{destPath}` is denied.");
                            }
                            var destFileNode = (FileNode)destNode;
                            destFileNode.Content.CopyFrom(((FileNode)srcNode).Content);

                            GetOrCreateDispatcher().RaiseChanged(srcPath);
                            GetOrCreateDispatcher().RaiseCreated(destPath);
                        }
                        else
                        {
                            throw new IOException($"The destination file path `{destPath}` already exist and overwrite is false");
                        }
                    }
                    finally
                    {
                        if (destNode != null)
                        {
                            ExitExclusive(destNode);
                        }

                        if (destDirectory != null)
                        {
                            ExitExclusive(destDirectory);
                        }
                    }
                }
                finally
                {
                    ExitFindNode(srcResult);
                }
            }
            finally
            {
                ExitFileSystemShared();
            }
        }

        /// <inheritdoc />
        protected override void ReplaceFileImpl(UPath srcPath, UPath destPath, UPath destBackupPath, bool ignoreMetadataErrors)
        {
            // Get the directories of src/dest/backup
            var parentSrcPath = srcPath.GetDirectory();
            var parentDestPath = destPath.GetDirectory();
            var parentDestBackupPath = destBackupPath.IsNull ? new UPath() : destBackupPath.GetDirectory();

            // Simple case: src/dest/backup in the same folder
            var isSameFolder = parentSrcPath == parentDestPath && (destBackupPath.IsNull || (parentDestBackupPath == parentSrcPath));
            // Else at least one folder is different. This is a rename semantic (as per the locking guidelines)

            var paths = new List<KeyValuePair<UPath, int>>
            {
                new KeyValuePair<UPath, int>(srcPath, 0),
                new KeyValuePair<UPath, int>(destPath, 1)
            };

            if (!destBackupPath.IsNull)
            {
                paths.Add(new KeyValuePair<UPath, int>(destBackupPath, 2));
            }
            paths.Sort((p1, p2) => string.Compare(p1.Key.FullName, p2.Key.FullName, StringComparison.Ordinal));

            // We need to take the lock on the folders in the correct order to avoid deadlocks
            // So we sort the srcPath and destPath in alphabetical order
            // (if srcPath is a subFolder of destPath, we will lock first destPath parent Folder, and then srcFolder)

            if (isSameFolder)
            {
                EnterFileSystemShared();
            }
            else
            {
                EnterFileSystemExclusive();
            }

            try
            {
                var results = new NodeResult[destBackupPath.IsNull ? 2 : 3];
                try
                {
                    foreach (var pathPair in paths)
                    {
                        var flags = FindNodeFlags.KeepParentNodeExclusive;
                        if (pathPair.Value != 2)
                        {
                            flags |= FindNodeFlags.NodeExclusive;
                        }
                        else
                        {
                            flags |= FindNodeFlags.NodeShared;
                        }
                        results[pathPair.Value] = EnterFindNode(pathPair.Key, flags, results);
                    }

                    var srcResult = results[0];
                    var destResult = results[1];

                    AssertFile(srcResult.Node, srcPath);
                    AssertFile(destResult.Node, destPath);

                    if (!destBackupPath.IsNull)
                    {
                        var backupResult = results[2];
                        AssertDirectory(backupResult.Directory, destPath);

                        if (backupResult.Node != null)
                        {
                            AssertFile(backupResult.Node, destBackupPath);
                            backupResult.Node.DetachFromParent();
                            backupResult.Node.Dispose();
                        }

                        destResult.Node.DetachFromParent();
                        destResult.Node.AttachToParent(backupResult.Directory, backupResult.Name);
                    }
                    else
                    {
                        destResult.Node.DetachFromParent();
                        destResult.Node.Dispose();
                    }

                    srcResult.Node.DetachFromParent();
                    GetOrCreateDispatcher().RaiseDeleted(srcPath);

                    srcResult.Node.AttachToParent(destResult.Directory, destResult.Name);
                    GetOrCreateDispatcher().RaiseChanged(destPath);
                }
                finally
                {
                    for (var i = results.Length - 1; i >= 0; i--)
                    {
                        ExitFindNode(results[i]);
                    }
                }
            }
            finally
            {
                if (isSameFolder)
                {
                    ExitFileSystemShared();
                }
                else
                {
                    ExitFileSystemExclusive();
                }
            }
        }

        /// <inheritdoc />
        protected override long GetFileLengthImpl(UPath path)
        {
            EnterFileSystemShared();
            try
            {
                return ((FileNode)FindNodeSafe(path, true)).Content.Length;
            }
            finally
            {
                ExitFileSystemShared();
            }
        }

        /// <inheritdoc />
        protected override FileEntry GetFileEntryImpl(UPath path)
        {
            UPath GetAbsolutePath(FileSystemNode node) =>
                node == null ? string.Empty : GetAbsolutePath(node.Parent) / node.Name;

            EnterFileSystemShared();
            try
            {
                var node = (FileNode)FindNodeSafe(path, true);
                return new FileEntry(GetAbsolutePath(node), node.Content.Length);
            }
            finally
            {
                ExitFileSystemShared();
            }
        }

        /// <inheritdoc />
        protected override bool FileExistsImpl(UPath path)
        {
            EnterFileSystemShared();
            try
            {
                // NodeCheck doesn't take a lock, on the return node
                // but allows us to check if it is a directory or a file
                var result = EnterFindNode(path, FindNodeFlags.NodeCheck);
                ExitFindNode(result);
                return result.Node is FileNode;
            }
            finally
            {
                ExitFileSystemShared();
            }
        }

        /// <inheritdoc />
        protected override void MoveFileImpl(UPath srcPath, UPath destPath)
        {
            MoveFileOrDirectory(srcPath, destPath, false);
        }

        /// <inheritdoc />
        protected override void DeleteFileImpl(UPath path)
        {
            EnterFileSystemShared();
            try
            {
                var result = EnterFindNode(path, FindNodeFlags.KeepParentNodeExclusive | FindNodeFlags.NodeExclusive);
                try
                {
                    var srcNode = result.Node;
                    if (srcNode == null)
                    {
                        // If the file to be deleted does not exist, no exception is thrown.
                        return;
                    }
                    if (srcNode is DirectoryNode || srcNode.IsReadOnly)
                    {
                        throw new UnauthorizedAccessException($"Access to path `{path}` is denied.");
                    }

                    srcNode.DetachFromParent();
                    srcNode.Dispose();

                    GetOrCreateDispatcher().RaiseDeleted(path);
                }
                finally
                {
                    ExitFindNode(result);
                }
            }
            finally
            {
                ExitFileSystemShared();
            }
        }

        /// <inheritdoc />
        protected override Stream OpenFileImpl(UPath path, FileMode mode, FileAccess access, FileShare share)
        {
            if (mode == FileMode.Append && (access & FileAccess.Read) != 0)
            {
                throw new ArgumentException("Combining FileMode: Append with FileAccess: Read is invalid.", nameof(access));
            }

            var isReading = (access & FileAccess.Read) != 0;
            var isWriting = (access & FileAccess.Write) != 0;
            var isExclusive = share == FileShare.None;

            EnterFileSystemShared();
            DirectoryNode parentDirectory = null;
            FileNode fileNodeToRelease = null;
            try
            {
                var result = EnterFindNode(path, (isExclusive ? FindNodeFlags.NodeExclusive : FindNodeFlags.NodeShared) | FindNodeFlags.KeepParentNodeExclusive, share);
                if (result.Directory == null)
                {
                    ExitFindNode(result);
                    throw FileSystemExceptionHelper.NewDirectoryNotFoundException(path);
                }

                if (result.Node is DirectoryNode || (isWriting && result.Node != null && result.Node.IsReadOnly))
                {
                    ExitFindNode(result);
                    throw new UnauthorizedAccessException($"Access to the path `{path}` is denied.");
                }

                var filename = result.Name;
                parentDirectory = result.Directory;
                var srcNode = result.Node;

                var fileNode = (FileNode)srcNode;

                // Append: Opens the file if it exists and seeks to the end of the file, or creates a new file. 
                //         This requires FileIOPermissionAccess.Append permission. FileMode.Append can be used only in 
                //         conjunction with FileAccess.Write. Trying to seek to a position before the end of the file 
                //         throws an IOException exception, and any attempt to read fails and throws a 
                //         NotSupportedException exception.
                //
                //
                // CreateNew: Specifies that the operating system should create a new file.This requires 
                //            FileIOPermissionAccess.Write permission. If the file already exists, an IOException 
                //            exception is thrown.
                //
                // Open: Specifies that the operating system should open an existing file. The ability to open 
                //       the file is dependent on the value specified by the FileAccess enumeration. 
                //       A System.IO.FileNotFoundException exception is thrown if the file does not exist.
                //
                // OpenOrCreate: Specifies that the operating system should open a file if it exists; 
                //               otherwise, a new file should be created. If the file is opened with 
                //               FileAccess.Read, FileIOPermissionAccess.Read permission is required. 
                //               If the file access is FileAccess.Write, FileIOPermissionAccess.Write permission 
                //               is required. If the file is opened with FileAccess.ReadWrite, both 
                //               FileIOPermissionAccess.Read and FileIOPermissionAccess.Write permissions 
                //               are required. 
                //
                // Truncate: Specifies that the operating system should open an existing file. 
                //           When the file is opened, it should be truncated so that its size is zero bytes. 
                //           This requires FileIOPermissionAccess.Write permission. Attempts to read from a file 
                //           opened with FileMode.Truncate cause an ArgumentException exception.

                // Create: Specifies that the operating system should create a new file.If the file already exists, 
                //         it will be overwritten.This requires FileIOPermissionAccess.Write permission. 
                //         FileMode.Create is equivalent to requesting that if the file does not exist, use CreateNew; 
                //         otherwise, use Truncate. If the file already exists but is a hidden file, 
                //         an UnauthorizedAccessException exception is thrown.

                var shouldTruncate = false;
                var shouldAppend = false;

                if (mode == FileMode.Create)
                {
                    if (fileNode != null)
                    {
                        mode = FileMode.Open;
                        shouldTruncate = true;
                    }
                    else
                    {
                        mode = FileMode.CreateNew;
                    }
                }

                if (mode == FileMode.OpenOrCreate)
                {
                    mode = fileNode != null ? FileMode.Open : FileMode.CreateNew;
                }

                if (mode == FileMode.Append)
                {
                    if (fileNode != null)
                    {
                        mode = FileMode.Open;
                        shouldAppend = true;
                    }
                    else
                    {
                        mode = FileMode.CreateNew;
                    }
                }

                if (mode == FileMode.Truncate)
                {
                    if (fileNode != null)
                    {
                        mode = FileMode.Open;
                        shouldTruncate = true;
                    }
                    else
                    {
                        throw FileSystemExceptionHelper.NewFileNotFoundException(path);
                    }
                }

                // Here we should only have Open or CreateNew
                Debug.Assert(mode == FileMode.Open || mode == FileMode.CreateNew);

                if (mode == FileMode.CreateNew)
                {
                    // This is not completely accurate to throw an exception (as we have been called with an option to OpenOrCreate)
                    // But we assume that between the beginning of the method and here, the filesystem is not changing, and 
                    // if it is, it is an unfortunate concurrency
                    if (fileNode != null)
                    {
                        fileNodeToRelease = fileNode;
                        throw FileSystemExceptionHelper.NewDestinationFileExistException(path);
                    }

                    fileNode = new FileNode(this, parentDirectory, filename, null);
                    GetOrCreateDispatcher().RaiseCreated(path);
                    GetOrCreateDispatcher().RaiseOpened(path);

                    if (isExclusive)
                    {
                        EnterExclusive(fileNode, path);
                    }
                    else
                    {
                        EnterShared(fileNode, path, share);
                    }
                }
                else
                {
                    if (fileNode == null)
                    {
                        throw FileSystemExceptionHelper.NewFileNotFoundException(path);
                    }

                    ExitExclusive(parentDirectory);
                    parentDirectory = null;
                }

                // TODO: Add checks between mode and access

                // Create and register a memory file stream
                var stream = new MemoryFileStream(this, fileNode, isReading, isWriting, isExclusive);
                StreamManager.Register(stream);

                if (shouldAppend)
                {
                    stream.Position = stream.Length;
                }
                else if (shouldTruncate)
                {
                    stream.SetLength(0);
                }

                return stream;
            }
            finally
            {
                if (fileNodeToRelease != null)
                {
                    if (isExclusive)
                    {
                        ExitExclusive(fileNodeToRelease);
                    }
                    else
                    {
                        ExitShared(fileNodeToRelease);
                    }
                }
                if (parentDirectory != null)
                {
                    ExitExclusive(parentDirectory);
                }
                ExitFileSystemShared();
            }
        }

        /// <inheritdoc />
        protected override Task<Stream> OpenFileAsyncImpl(UPath path, FileMode mode, FileAccess access, FileShare share)
        {
            return Task.FromResult(OpenFile(path, mode, access, share));
        }

        /// <inheritdoc />
        protected override void SetFileDataImpl(UPath savePath, Stream saveData)
        {
            // 1. Create file
            var createdFile = OpenFile(savePath, FileMode.Create, FileAccess.Write);

            // 2. Write new content
            var bkPos = saveData.Position;
            saveData.Position = 0;
            saveData.CopyTo(createdFile);
            saveData.Position = bkPos;

            createdFile.Close();
        }

        /// <inheritdoc />
        protected override ulong GetTotalSizeImpl(UPath directory)
        {
            throw new NotImplementedException();
        }

        // ----------------------------------------------
        // Search API
        // ----------------------------------------------

        /// <inheritdoc />
        protected override IEnumerable<UPath> EnumeratePathsImpl(UPath path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            var search = SearchPattern.Parse(ref path, ref searchPattern);

            var foldersToProcess = new List<UPath> { path };

            var entries = new SortedSet<UPath>(UPath.DefaultComparerIgnoreCase);
            while (foldersToProcess.Count > 0)
            {
                var directoryPath = foldersToProcess[0];
                foldersToProcess.RemoveAt(0);
                var dirIndex = 0;
                entries.Clear();

                // This is important that here we don't lock the FileSystemShared
                // or the visited folder while returning a yield otherwise the finally
                // may never be executed if the caller of this method decide to not
                // Dispose the IEnumerable (because the generated IEnumerable
                // doesn't have a finalizer calling Dispose)
                // This is why the yield is performed outside this block
                EnterFileSystemShared();
                try
                {
                    var result = EnterFindNode(directoryPath, FindNodeFlags.NodeShared);
                    try
                    {
                        if (directoryPath == path)
                        {
                            // The first folder must be a directory, if it is not, throw an error
                            AssertDirectory(result.Node, directoryPath);
                        }
                        else
                        {
                            // Might happen during the time a DirectoryNode is enqueued into foldersToProcess
                            // and the time we are going to actually visit it, it might have been
                            // removed in the meantime, so we make sure here that we have a folder
                            // and we don't throw an error if it is not
                            if (!(result.Node is DirectoryNode))
                            {
                                continue;
                            }
                        }

                        var directory = (DirectoryNode)result.Node;
                        foreach (var nodePair in directory.Children)
                        {
                            if (nodePair.Value is FileNode && searchTarget == SearchTarget.Directory)
                            {
                                continue;
                            }

                            var isEntryMatching = search.Match(nodePair.Key);

                            var canFollowFolder = searchOption == SearchOption.AllDirectories && nodePair.Value is DirectoryNode;

                            var addEntry = (nodePair.Value is FileNode && searchTarget != SearchTarget.Directory && isEntryMatching)
                                           || (nodePair.Value is DirectoryNode && searchTarget != SearchTarget.File && isEntryMatching);

                            var fullPath = directoryPath / nodePair.Key;

                            if (canFollowFolder)
                            {
                                foldersToProcess.Insert(dirIndex++, fullPath);
                            }

                            if (addEntry)
                            {
                                entries.Add(fullPath);
                            }
                        }
                    }
                    finally
                    {
                        ExitFindNode(result);
                    }
                }
                finally
                {
                    ExitFileSystemShared();
                }

                // We return all the elements of visited directory in one shot, outside the previous lock block
                foreach (var entry in entries)
                {
                    yield return entry;
                }
            }
        }

        // ----------------------------------------------
        // Watch API
        // ----------------------------------------------

        /// <inheritdoc />
        protected override bool CanWatchImpl(UPath path)
        {
            return DirectoryExists(path);
        }

        /// <inheritdoc />
        protected override IFileSystemWatcher WatchImpl(UPath path)
        {
            var watcher = new Watcher.FileSystemWatcher(this, path);
            watcher.Disposed += Watcher_Disposed;

            GetOrCreateDispatcher().Add(watcher);

            return watcher;
        }

        private void Watcher_Disposed(object sender, EventArgs e)
        {
            GetOrCreateDispatcher().Remove((Watcher.FileSystemWatcher)sender);
        }

        // ----------------------------------------------
        // Path API
        // ----------------------------------------------

        /// <inheritdoc />
        protected override string ConvertPathToInternalImpl(UPath path)
        {
            return path.FullName;
        }

        /// <inheritdoc />
        protected override UPath ConvertPathFromInternalImpl(string innerPath)
        {
            return new UPath(innerPath);
        }

        // ----------------------------------------------
        // Internals
        // ----------------------------------------------

        private void MoveFileOrDirectory(UPath srcPath, UPath destPath, bool expectDirectory)
        {
            var parentSrcPath = srcPath.GetDirectory();
            var parentDestPath = destPath.GetDirectory();

            void AssertNoDestination(FileSystemNode node)
            {
                if (expectDirectory)
                {
                    if (node is FileNode || node != null)
                    {
                        throw FileSystemExceptionHelper.NewDestinationFileExistException(destPath);
                    }
                }
                else
                {
                    if (node is DirectoryNode || node != null)
                    {
                        throw FileSystemExceptionHelper.NewDestinationDirectoryExistException(destPath);
                    }
                }
            }

            // Same directory move
            var isSameFolder = parentSrcPath == parentDestPath;
            // Check that Destination folder is not a subfolder of source directory
            if (!isSameFolder && expectDirectory)
            {
                var checkParentDestDirectory = destPath.GetDirectory();
                while (checkParentDestDirectory != null)
                {
                    if (checkParentDestDirectory == srcPath)
                    {
                        throw new IOException($"Cannot move the source directory `{srcPath}` to a a sub-folder of itself `{destPath}`");
                    }

                    checkParentDestDirectory = checkParentDestDirectory.GetDirectory();
                }
            }

            // We need to take the lock on the folders in the correct order to avoid deadlocks
            // So we sort the srcPath and destPath in alphabetical order
            // (if srcPath is a subFolder of destPath, we will lock first destPath parent Folder, and then srcFolder)

            var isLockInverted = !isSameFolder && string.Compare(srcPath.FullName, destPath.FullName, StringComparison.Ordinal) > 0;

            if (isSameFolder)
            {
                EnterFileSystemShared();
            }
            else
            {
                EnterFileSystemExclusive();
            }
            try
            {
                var srcResult = new NodeResult();
                var destResult = new NodeResult();
                try
                {
                    if (isLockInverted)
                    {
                        destResult = EnterFindNode(destPath, FindNodeFlags.KeepParentNodeExclusive | FindNodeFlags.NodeShared);
                        srcResult = EnterFindNode(srcPath, FindNodeFlags.KeepParentNodeExclusive | FindNodeFlags.NodeExclusive, destResult);
                    }
                    else
                    {
                        srcResult = EnterFindNode(srcPath, FindNodeFlags.KeepParentNodeExclusive | FindNodeFlags.NodeExclusive);
                        destResult = EnterFindNode(destPath, FindNodeFlags.KeepParentNodeExclusive | FindNodeFlags.NodeShared, srcResult);
                    }

                    if (expectDirectory)
                    {
                        AssertDirectory(srcResult.Node, srcPath);
                    }
                    else
                    {
                        AssertFile(srcResult.Node, srcPath);
                    }
                    AssertDirectory(destResult.Directory, destPath);

                    AssertNoDestination(destResult.Node);

                    srcResult.Node.DetachFromParent();
                    srcResult.Node.AttachToParent(destResult.Directory, destResult.Name);

                    GetOrCreateDispatcher().RaiseRenamed(destPath, srcPath);
                }
                finally
                {
                    if (isLockInverted)
                    {
                        ExitFindNode(srcResult);
                        ExitFindNode(destResult);
                    }
                    else
                    {
                        ExitFindNode(destResult);
                        ExitFindNode(srcResult);
                    }
                }
            }
            finally
            {
                if (isSameFolder)
                {
                    ExitFileSystemShared();
                }
                else
                {
                    ExitFileSystemExclusive();
                }
            }
        }

        private void AssertDirectory(FileSystemNode node, UPath srcPath)
        {
            if (node is FileNode)
            {
                throw new IOException($"The source directory `{srcPath}` is a file");
            }
            if (node == null)
            {
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(srcPath);
            }
        }

        private void AssertFile(FileSystemNode node, UPath srcPath)
        {
            if (node == null)
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(srcPath);
            }
        }

        private FileSystemNode TryFindNodeSafe(UPath path)
        {
            EnterFileSystemShared();
            try
            {
                var result = EnterFindNode(path, FindNodeFlags.NodeShared);
                try
                {
                    var node = result.Node;
                    return node;
                }
                finally
                {
                    ExitFindNode(result);
                }
            }
            finally
            {
                ExitFileSystemShared();
            }
        }

        private FileSystemNode FindNodeSafe(UPath path, bool expectFileOnly)
        {
            var node = TryFindNodeSafe(path);

            if (node == null)
            {
                if (expectFileOnly)
                {
                    throw FileSystemExceptionHelper.NewFileNotFoundException(path);
                }
                throw new IOException($"The file or directory `{path}` was not found");
            }

            if (node is DirectoryNode)
            {
                if (expectFileOnly)
                {
                    throw FileSystemExceptionHelper.NewFileNotFoundException(path);
                }
            }

            return node;
        }

        private void CreateDirectoryNode(UPath path)
        {
            ExitFindNode(EnterFindNode(path, FindNodeFlags.CreatePathIfNotExist | FindNodeFlags.NodeShared));
        }

        private struct NodeResult
        {
            public NodeResult(DirectoryNode directory, FileSystemNode node, string name, FindNodeFlags flags)
            {
                Directory = directory;
                Node = node;
                Name = name;
                Flags = flags;
            }

            public readonly DirectoryNode Directory;

            public readonly FileSystemNode Node;

            public readonly string Name;

            public readonly FindNodeFlags Flags;
        }

        [Flags]
        private enum FindNodeFlags
        {
            CreatePathIfNotExist = 1 << 1,

            NodeCheck = 1 << 2,

            NodeShared = 1 << 3,

            NodeExclusive = 1 << 4,

            KeepParentNodeExclusive = 1 << 5,

            KeepParentNodeShared = 1 << 6,
        }

        private void ExitFindNode(NodeResult nodeResult)
        {
            var flags = nodeResult.Flags;

            // Unlock first the node
            if (nodeResult.Node != null)
            {
                if ((flags & FindNodeFlags.NodeExclusive) != 0)
                {
                    ExitExclusive(nodeResult.Node);
                }
                else if ((flags & FindNodeFlags.NodeShared) != 0)
                {
                    ExitShared(nodeResult.Node);
                }
            }

            if (nodeResult.Directory == null)
            {
                return;
            }

            // Unlock the parent directory if necessary
            if ((flags & FindNodeFlags.KeepParentNodeExclusive) != 0)
            {
                ExitExclusive(nodeResult.Directory);
            }
            else if ((flags & FindNodeFlags.KeepParentNodeShared) != 0)
            {
                ExitShared(nodeResult.Directory);
            }
        }


        private NodeResult EnterFindNode(UPath path, FindNodeFlags flags, params NodeResult[] existingNodes)
        {
            return EnterFindNode(path, flags, null, existingNodes);
        }

        private NodeResult EnterFindNode(UPath path, FindNodeFlags flags, FileShare? share, params NodeResult[] existingNodes)
        {
            // TODO: Split the flags between parent and node to make the code more clear
            var result = new NodeResult();

            // This method should be always called with at least one of these
            Debug.Assert((flags & (FindNodeFlags.NodeExclusive | FindNodeFlags.NodeShared | FindNodeFlags.NodeCheck)) != 0);

            var sharePath = share ?? FileShare.Read;
            var isLockOnRootAlreadyTaken = IsNodeAlreadyLocked(_rootDirectory, existingNodes);

            // Even if it is not valid, the EnterFindNode may be called with a root directory
            // So we handle it as a special case here
            if (path == UPath.Root)
            {
                if (!isLockOnRootAlreadyTaken)
                {
                    if ((flags & FindNodeFlags.NodeExclusive) != 0)
                    {
                        EnterExclusive(_rootDirectory, path);
                    }
                    else if ((flags & FindNodeFlags.NodeShared) != 0)
                    {
                        EnterShared(_rootDirectory, path, sharePath);
                    }
                }
                else
                {
                    // If the lock was already taken, we make sure that NodeResult
                    // will not try to release it
                    flags &= ~(FindNodeFlags.NodeExclusive | FindNodeFlags.NodeShared);
                }
                result = new NodeResult(null, _rootDirectory, null, flags);
                return result;
            }

            var isRequiringExclusiveLockForParent = (flags & (FindNodeFlags.CreatePathIfNotExist | FindNodeFlags.KeepParentNodeExclusive)) != 0;

            var parentNode = _rootDirectory;
            var names = path.Split();

            // Walking down the nodes in locking order:
            // /a/b/c.txt
            //
            // Lock /
            // Lock /a
            // Unlock /
            // Lock /a/b
            // Unlock /a
            // Lock /a/b/c.txt

            // Start by locking the parent directory (only if it is not already locked)
            var isParentLockTaken = false;
            if (!isLockOnRootAlreadyTaken)
            {
                EnterExclusiveOrSharedDirectoryOrBlock(_rootDirectory, path, isRequiringExclusiveLockForParent);
                isParentLockTaken = true;
            }

            for (var i = 0; i < names.Count && parentNode != null; i++)
            {
                var name = names[i];
                var isLast = i + 1 == names.Count;

                DirectoryNode nextParent = null;
                var isNextParentLockTaken = false;
                try
                {
                    FileSystemNode subNode;
                    if (!parentNode.Children.TryGetValue(name, out subNode))
                    {
                        if ((flags & FindNodeFlags.CreatePathIfNotExist) != 0)
                        {
                            subNode = new DirectoryNode(this, parentNode, name);
                        }
                    }
                    else
                    {
                        // If we are trying to create a directory and one of the node on the way is a file
                        // this is an error
                        if ((flags & FindNodeFlags.CreatePathIfNotExist) != 0 && subNode is FileNode)
                        {
                            throw new IOException($"Cannot create directory `{path}` on an existing file");
                        }
                    }

                    // Special case of the last entry
                    if (isLast)
                    {
                        // If the lock was not taken by the parent, modify the flags 
                        // so that Exit(NodeResult) will not try to release the lock on the parent
                        if (!isParentLockTaken)
                        {
                            flags &= ~(FindNodeFlags.KeepParentNodeExclusive | FindNodeFlags.KeepParentNodeShared);
                        }

                        result = new NodeResult(parentNode, subNode, name, flags);

                        // The last subnode may be null but we still want to return a valid parent
                        // otherwise, lock the final node if necessary
                        if (subNode != null)
                        {
                            if ((flags & FindNodeFlags.NodeExclusive) != 0)
                            {
                                EnterExclusive(subNode, path);
                            }
                            else if ((flags & FindNodeFlags.NodeShared) != 0)
                            {
                                EnterShared(subNode, path, sharePath);
                            }
                        }

                        // After we have taken the lock, and we need to keep a lock on the parent, make sure
                        // that the finally {} below will not unlock the parent
                        // This is important to perform this here, as the previous EnterExclusive/EnterShared
                        // could have failed (e.g trying to lock exclusive on a file already locked)
                        // and thus, we would have to release the lock of the parent in finally
                        if ((flags & (FindNodeFlags.KeepParentNodeExclusive | FindNodeFlags.KeepParentNodeShared)) != 0)
                        {
                            parentNode = null;
                            break;
                        }
                    }
                    else
                    {
                        // Going down the directory, 
                        nextParent = subNode as DirectoryNode;
                        if (nextParent != null && !IsNodeAlreadyLocked(nextParent, existingNodes))
                        {
                            EnterExclusiveOrSharedDirectoryOrBlock(nextParent, path, isRequiringExclusiveLockForParent);
                            isNextParentLockTaken = true;
                        }
                    }
                }
                finally
                {
                    // We unlock the parent only if it was taken
                    if (isParentLockTaken && parentNode != null)
                    {
                        ExitExclusiveOrShared(parentNode, isRequiringExclusiveLockForParent);
                    }
                }

                parentNode = nextParent;
                isParentLockTaken = isNextParentLockTaken;
            }

            return result;
        }

        private static bool IsNodeAlreadyLocked(DirectoryNode directoryNode, NodeResult[] existingNodes)
        {
            foreach (var existingNode in existingNodes)
            {
                if (existingNode.Directory == directoryNode || existingNode.Node == directoryNode)
                {
                    return true;
                }
            }
            return false;
        }

        // ----------------------------------------------
        // Locks internals
        // ----------------------------------------------

        private void EnterFileSystemShared()
        {
            _globalLock.EnterShared(UPath.Root);
        }

        private void ExitFileSystemShared()
        {
            _globalLock.ExitShared();
        }

        private void EnterFileSystemExclusive()
        {
            _globalLock.EnterExclusive();
        }

        private void ExitFileSystemExclusive()
        {
            _globalLock.ExitExclusive();
        }

        private void EnterSharedDirectoryOrBlock(DirectoryNode node, UPath context)
        {
            EnterShared(node, context, true, FileShare.Read);
        }

        private void EnterExclusiveOrSharedDirectoryOrBlock(DirectoryNode node, UPath context, bool isExclusive)
        {
            if (isExclusive)
            {
                EnterExclusiveDirectoryOrBlock(node, context);
            }
            else
            {
                EnterSharedDirectoryOrBlock(node, context);
            }
        }

        private void EnterExclusiveDirectoryOrBlock(DirectoryNode node, UPath context)
        {
            EnterExclusive(node, context, true);
        }

        private void EnterExclusive(FileSystemNode node, UPath context)
        {
            EnterExclusive(node, context, node is DirectoryNode);
        }

        private void EnterShared(FileSystemNode node, UPath context, FileShare share)
        {
            EnterShared(node, context, node is DirectoryNode, share);
        }

        private void EnterShared(FileSystemNode node, UPath context, bool block, FileShare share)
        {
            if (block)
            {
                node.EnterShared(share, context);
            }
            else if (!node.TryEnterShared(share))
            {
                var pathType = node is FileNode ? "file" : "directory";
                throw new IOException($"The {pathType} `{context}` is already used for writing by another thread.");
            }
        }

        private void ExitShared(FileSystemNode node)
        {
            node.ExitShared();
        }

        private void EnterExclusive(FileSystemNode node, UPath context, bool block)
        {
            if (block)
            {
                node.EnterExclusive();
            }
            else if (!node.TryEnterExclusive())
            {
                var pathType = node is FileNode ? "file" : "directory";
                throw new IOException($"The {pathType} `{context}` is already locked.");
            }
        }

        private void ExitExclusiveOrShared(FileSystemNode node, bool isExclusive)
        {
            if (isExclusive)
            {
                node.ExitExclusive();
            }
            else
            {
                node.ExitShared();
            }
        }

        private void ExitExclusive(FileSystemNode node)
        {
            node.ExitExclusive();
        }

        private void TryLockExclusive(FileSystemNode node, ListFileSystemNodes locks, bool recursive, UPath context)
        {
            if (locks == null) throw new ArgumentNullException(nameof(locks));

            if (node is DirectoryNode directory)
            {
                if (recursive)
                {
                    foreach (var child in directory.Children)
                    {
                        EnterExclusive(child.Value, context);

                        var path = context / child.Key;
                        locks.Add(child);

                        TryLockExclusive(child.Value, locks, true, path);
                    }
                }
                else
                {
                    if (directory.Children.Count > 0)
                    {
                        throw new IOException($"The directory `{context}` is not empty");
                    }
                }
            }
        }

        private abstract class FileSystemNode : FileSystemNodeReadWriteLock
        {
            private readonly MemoryFileSystem _fileSystem;

            protected FileSystemNode(MemoryFileSystem fileSystem, DirectoryNode parentNode, string name, FileSystemNode copyNode)
            {
                Debug.Assert(fileSystem != null);
                Debug.Assert((parentNode == null) == string.IsNullOrEmpty(name));

                _fileSystem = fileSystem;

                if (parentNode != null && !string.IsNullOrEmpty(name))
                {
                    Debug.Assert(parentNode.IsLocked);

                    parentNode.Children.Add(name, this);
                    Parent = parentNode;
                    Name = name;
                }

                if (copyNode != null && copyNode.Attributes != 0)
                {
                    Attributes = copyNode.Attributes;
                }
                CreationTime = DateTime.Now;
                LastWriteTime = copyNode?.LastWriteTime ?? CreationTime;
                LastAccessTime = copyNode?.LastAccessTime ?? CreationTime;
            }

            public DirectoryNode Parent { get; private set; }

            public string Name { get; private set; }

            public FileAttributes Attributes { get; set; }

            public DateTime CreationTime { get; set; }

            public DateTime LastWriteTime { get; set; }

            public DateTime LastAccessTime { get; set; }

            public bool IsDisposed { get; set; }

            public bool IsReadOnly => (Attributes & FileAttributes.ReadOnly) != 0;

            public void DetachFromParent()
            {
                Debug.Assert(IsLocked);
                var parent = Parent;
                Debug.Assert(parent.IsLocked);

                parent.Children.Remove(Name);
                Parent = null;
                Name = null;
            }

            public void AttachToParent(DirectoryNode parentNode, string name)
            {
                if (parentNode == null) throw new ArgumentNullException(nameof(parentNode));
                if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
                Debug.Assert(parentNode.IsLocked);
                Debug.Assert(IsLocked);
                Debug.Assert(Parent == null);

                Parent = parentNode;
                Parent.Children.Add(name, this);
                Name = name;
            }

            public void Dispose()
            {
                Debug.Assert(IsLocked);
                // In order to issue a Dispose, we need to have control on this node
                IsDisposed = true;
            }

            public virtual FileSystemNode Clone(DirectoryNode newParent, string newName)
            {
                Debug.Assert((newParent == null) == string.IsNullOrEmpty(newName));

                var clone = (FileSystemNode)Clone();
                clone.Parent = newParent;
                clone.Name = newName;
                return clone;
            }
        }

        private class ListFileSystemNodes : List<KeyValuePair<string, FileSystemNode>>, IDisposable
        {
            private readonly MemoryFileSystem _fs;

            public ListFileSystemNodes(MemoryFileSystem fs)
            {
                Debug.Assert(fs != null);
                _fs = fs;
            }

            public void Dispose()
            {
                for (var i = this.Count - 1; i >= 0; i--)
                {
                    var entry = this[i];
                    _fs.ExitExclusive(entry.Value);
                }
                Clear();
            }
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "(),nq}")]
        [DebuggerTypeProxy(typeof(DebuggerProxyInternal))]
        private class DirectoryNode : FileSystemNode
        {
            private Dictionary<string, FileSystemNode> _children;

            public DirectoryNode(MemoryFileSystem fileSystem) : base(fileSystem, null, null, null)
            {
                _children = new Dictionary<string, FileSystemNode>();
            }

            public DirectoryNode(MemoryFileSystem fileSystem, DirectoryNode parent, string name) : base(fileSystem, parent, name, null)
            {
                Debug.Assert(parent != null);
                _children = new Dictionary<string, FileSystemNode>();
            }

            public Dictionary<string, FileSystemNode> Children
            {
                get
                {
                    Debug.Assert(IsLocked);
                    return _children;
                }
            }

            public override FileSystemNode Clone(DirectoryNode newParent, string newName)
            {
                var dir = (DirectoryNode)base.Clone(newParent, newName);
                dir._children = new Dictionary<string, FileSystemNode>();
                foreach (var name in _children.Keys)
                {
                    dir._children[name] = _children[name].Clone(dir, name);
                }
                return dir;
            }

            public override string DebuggerDisplay()
            {
                return Name == null ? $"Count = {_children.Count}{base.DebuggerDisplay()}" : $"Folder: {Name}, Count = {_children.Count}{base.DebuggerDisplay()}";
            }

            private sealed class DebuggerProxyInternal
            {
                private readonly DirectoryNode _directoryNode;

                public DebuggerProxyInternal(DirectoryNode directoryNode)
                {
                    _directoryNode = directoryNode;
                }

                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public FileSystemNode[] Items => _directoryNode._children.Values.ToArray();
            }
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "(),nq}")]
        private class FileNode : FileSystemNode
        {
            public FileNode(MemoryFileSystem fileSystem, DirectoryNode parentNode, string name, FileNode copyNode)
                : base(fileSystem, parentNode, name, copyNode)
            {
                if (copyNode != null)
                {
                    Content = new FileContent(this, copyNode.Content);
                }
                else
                {
                    Attributes = FileAttributes.Archive;
                    Content = new FileContent(this);
                }
            }

            public FileContent Content { get; private set; }


            public override FileSystemNode Clone(DirectoryNode newParent, string newName)
            {
                var copy = (FileNode)base.Clone(newParent, newName);
                copy.Content = new FileContent(copy, Content);
                return copy;
            }

            public override string DebuggerDisplay()
            {
                return $"File: {Name}, {Content.DebuggerDisplay()}{base.DebuggerDisplay()}";
            }
        }

        private class FileContent
        {
            private readonly FileNode _fileNode;
            private readonly MemoryStream _stream;

            public FileContent(FileNode fileNode)
            {
                Debug.Assert(fileNode != null);

                _fileNode = fileNode;
                _stream = new MemoryStream();
            }

            public FileContent(FileNode fileNode, FileContent copy)
            {
                Debug.Assert(fileNode != null);

                _fileNode = fileNode;
                var length = copy.Length;
                _stream = new MemoryStream(length <= int.MaxValue ? (int)length : int.MaxValue);
                CopyFrom(copy);
            }

            public byte[] ToArray()
            {
                lock (this)
                {
                    return _stream.ToArray();
                }
            }

            public void CopyFrom(FileContent copy)
            {
                lock (this)
                {
                    var length = copy.Length;
                    var buffer = copy.ToArray();
                    _stream.Position = 0;
                    _stream.Write(buffer, 0, buffer.Length);
                    _stream.Position = 0;
                    _stream.SetLength(length);
                }
            }

            public int Read(long position, byte[] buffer, int offset, int count)
            {
                lock (this)
                {
                    _stream.Position = position;
                    return _stream.Read(buffer, offset, count);
                }
            }

            public void Write(long position, byte[] buffer, int offset, int count)
            {
                lock (this)
                {
                    _stream.Position = position;
                    _stream.Write(buffer, offset, count);
                }
            }

            public void SetPosition(long position)
            {
                lock (this)
                {
                    _stream.Position = position;
                }
            }

            public long Length
            {
                get
                {
                    lock (this)
                    {
                        return _stream.Length;
                    }
                }
                set
                {
                    lock (this)
                    {
                        _stream.SetLength(value);
                    }
                }
            }

            public string DebuggerDisplay() => $"Size = {_stream.Length}";
        }

        private sealed class MemoryFileStream : Stream
        {
            private readonly MemoryFileSystem _fs;
            private readonly FileNode _fileNode;
            private readonly bool _canRead;
            private readonly bool _canWrite;
            private readonly bool _isExclusive;
            private int _isDisposed;
            private long _position;

            public MemoryFileStream(MemoryFileSystem fs, FileNode fileNode, bool canRead, bool canWrite, bool isExclusive)
            {
                Debug.Assert(fs != null);
                Debug.Assert(fileNode != null);
                Debug.Assert(fileNode.IsLocked);
                _fs = fs;
                _fileNode = fileNode;
                _canWrite = canWrite;
                _canRead = canRead;
                _isExclusive = isExclusive;
                _position = 0;
            }

            public override bool CanRead => _isDisposed == 0 && _canRead;

            public override bool CanSeek => _isDisposed == 0;

            public override bool CanWrite => _isDisposed == 0 && _canWrite;

            public override long Length
            {
                get
                {
                    CheckNotDisposed();
                    return _fileNode.Content.Length;
                }
            }

            public override long Position
            {
                get
                {
                    CheckNotDisposed();
                    return _position;
                }

                set
                {
                    CheckNotDisposed();
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException("The position cannot be negative");
                    }
                    _position = value;
                    _fileNode.Content.SetPosition(_position);
                }
            }

            ~MemoryFileStream()
            {
                Dispose(false);
            }

            protected override void Dispose(bool disposing)
            {
                if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
                {
                    return;
                }

                if (_isExclusive)
                {
                    _fs.ExitExclusive(_fileNode);
                }
                else
                {
                    _fs.ExitShared(_fileNode);
                }

                base.Dispose(disposing);
            }

            public override void Flush()
            {
                CheckNotDisposed();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                CheckNotDisposed();
                var readCount = _fileNode.Content.Read(_position, buffer, offset, count);
                _position += readCount;
                _fileNode.LastAccessTime = DateTime.Now;
                return readCount;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                CheckNotDisposed();
                var newPosition = offset;

                switch (origin)
                {
                    case SeekOrigin.Current:
                        newPosition += _position;
                        break;

                    case SeekOrigin.End:
                        newPosition += _fileNode.Content.Length;
                        break;
                }

                if (newPosition < 0)
                {
                    throw new IOException("An attempt was made to move the file pointer before the beginning of the file");
                }

                return _position = newPosition;
            }

            public override void SetLength(long value)
            {
                CheckNotDisposed();
                _fileNode.Content.Length = value;

                var time = DateTime.Now;
                _fileNode.LastAccessTime = time;
                _fileNode.LastWriteTime = time;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                CheckNotDisposed();
                _fileNode.Content.Write(_position, buffer, offset, count);
                _position += count;

                var time = DateTime.Now;
                _fileNode.LastAccessTime = time;
                _fileNode.LastWriteTime = time;
            }


            private void CheckNotDisposed()
            {
                if (_isDisposed > 0)
                {
                    throw new ObjectDisposedException("Cannot access a closed file.");
                }
            }
        }

        /// <summary>
        /// Internal class used to synchronize shared-exclusive access to a <see cref="FileSystemNode"/>
        /// </summary>
        private class FileSystemNodeReadWriteLock
        {
            // _sharedCount  < 0 => This is an exclusive lock (_sharedCount == -1)
            // _sharedCount == 0 => No lock
            // _sharedCount  > 0 => This is a shared lock
            private int _sharedCount;

            private FileShare? _shared;

            internal bool IsLocked => _sharedCount != 0;

            public void EnterShared(UPath context)
            {
                EnterShared(FileShare.Read, context);
            }

            protected FileSystemNodeReadWriteLock Clone()
            {
                var locker = (FileSystemNodeReadWriteLock)MemberwiseClone();
                // Erase any locks
                locker._sharedCount = 0;
                locker._shared = null;
                return locker;
            }

            public void EnterShared(FileShare share, UPath context)
            {
                Monitor.Enter(this);
                try
                {
                    while (_sharedCount < 0)
                    {
                        Monitor.Wait(this);
                    }

                    if (_shared.HasValue)
                    {
                        var currentShare = _shared.Value;
                        // The previous share must be a superset of the shared being asked
                        if ((share & currentShare) != share)
                        {
                            throw new UnauthorizedAccessException($"Cannot access shared resource path `{context}` with shared access`{share}` while current is `{currentShare}`");
                        }
                    }
                    else
                    {
                        _shared = share;
                    }

                    _sharedCount++;
                    Monitor.PulseAll(this);
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }

            public void ExitShared()
            {
                Monitor.Enter(this);
                try
                {
                    Debug.Assert(_sharedCount > 0);
                    _sharedCount--;
                    if (_sharedCount == 0)
                    {
                        _shared = null;
                    }
                    Monitor.PulseAll(this);
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }

            public void EnterExclusive()
            {
                Monitor.Enter(this);
                try
                {
                    while (_sharedCount != 0)
                    {
                        Monitor.Wait(this);
                    }
                    _sharedCount = -1;
                    Monitor.PulseAll(this);
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }

            public bool TryEnterShared(FileShare share)
            {
                Monitor.Enter(this);
                try
                {
                    if (_sharedCount < 0)
                    {
                        return false;
                    }

                    if (_shared.HasValue)
                    {
                        var currentShare = _shared.Value;
                        if ((share & currentShare) != share)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        _shared = share;
                    }
                    _sharedCount++;
                    Monitor.PulseAll(this);
                }
                finally
                {
                    Monitor.Exit(this);
                }
                return true;
            }

            public bool TryEnterExclusive()
            {
                Monitor.Enter(this);
                try
                {
                    if (_sharedCount != 0)
                    {
                        return false;
                    }
                    _sharedCount = -1;
                    Monitor.PulseAll(this);
                }
                finally
                {
                    Monitor.Exit(this);
                }
                return true;
            }
            public void ExitExclusive()
            {
                Monitor.Enter(this);
                try
                {
                    Debug.Assert(_sharedCount < 0);
                    _sharedCount = 0;
                    Monitor.PulseAll(this);
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }

            public virtual string DebuggerDisplay()
            {
                return _sharedCount < 0 ? " (exclusive lock)" : _sharedCount > 0 ? $" (shared lock: {_sharedCount})" : string.Empty;
            }
        }
    }
}