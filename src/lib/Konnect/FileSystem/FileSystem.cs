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
// - See main changes in the IFileSystem interface

using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Enums.FileSystem;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Streams;
using Konnect.Extensions;
using Konnect.FileSystem.Events;

namespace Konnect.FileSystem;

/// <summary>
/// Abstract class for a <see cref="IFileSystem"/>. Provides default arguments safety checking and redirecting to safe implementation.
/// Implements also the <see cref="IDisposable"/> pattern.
/// </summary>
public abstract class FileSystem : IFileSystem
{
    private readonly object _dispatcherLock;
    private FileSystemEventDispatcher<Watcher.FileSystemWatcher> _dispatcher;

    protected IStreamManager StreamManager { get; }

    /// <summary>
    /// The default file time if the file described in a path parameter does not exist.
    /// The default file time is 12:00 midnight, January 1, 1601 A.D. (C.E.) Coordinated Universal Time (UTC), adjusted to local time.
    /// </summary>
    public static readonly DateTime DefaultFileTime = new DateTime(1601, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();

    /// <summary>
    /// Creates a new instance of <see cref="FileSystem"/>.
    /// </summary>
    /// <param name="streamManager">The stream manager to scope streams in.</param>
    public FileSystem(IStreamManager streamManager)
    {
        _dispatcherLock = new object();

        StreamManager = streamManager;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="FileSystem"/> class.
    /// </summary>
    ~FileSystem()
    {
        DisposeInternal(false);
    }

    /// <inheritdoc />
    public abstract IFileSystem Clone(IStreamManager streamManager);

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeInternal(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// <c>true</c> if this instance if being disposed.
    /// </summary>
    protected bool IsDisposing { get; private set; }

    /// <summary>
    /// <c>true</c> if this instance if being disposed.
    /// </summary>
    protected bool IsDisposed { get; private set; }

    #region Directory API

    /// <inheritdoc />
    public abstract bool CanCreateDirectories { get; }

    /// <inheritdoc />
    public abstract bool CanDeleteDirectories { get; }

    /// <inheritdoc />
    public abstract bool CanMoveDirectories { get; }

    /// <inheritdoc />
    public void CreateDirectory(UPath path)
    {
        AssertNotDisposed();
        AssertTrue(CanCreateDirectories, nameof(CreateDirectory));
        if (path == UPath.Root)
        {
            throw new UnauthorizedAccessException("Cannot create root directory `/`");
        }
        CreateDirectoryImpl(ValidatePath(path));
    }

    /// <summary>
    /// Implementation for <see cref="CreateDirectory"/>, paths is guaranteed to be absolute and not the root path `/`
    /// and validated through <see cref="ValidatePath"/>.
    /// Creates all directories and subdirectories in the specified path unless they already exist.
    /// </summary>
    /// <param name="path">The directory to create.</param>
    protected abstract void CreateDirectoryImpl(UPath path);

    /// <inheritdoc />
    public bool DirectoryExists(UPath path)
    {
        AssertNotDisposed();

        // With FileExists, case where a null path is allowed
        if (path.IsNull)
        {
            return false;
        }

        return DirectoryExistsImpl(ValidatePath(path));
    }

    /// <summary>
    /// Implementation for <see cref="DirectoryExists"/>, paths is guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Determines whether the given path refers to an existing directory on disk.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns><c>true</c> if the given path refers to an existing directory on disk, <c>false</c> otherwise.</returns>
    protected abstract bool DirectoryExistsImpl(UPath path);

    /// <inheritdoc />
    public void MoveDirectory(UPath srcPath, UPath destPath)
    {
        AssertNotDisposed();
        AssertTrue(CanMoveDirectories, nameof(MoveDirectory));
        if (srcPath == UPath.Root)
        {
            throw new UnauthorizedAccessException("Cannot move from the source root directory `/`");
        }
        if (destPath == UPath.Root)
        {
            throw new UnauthorizedAccessException("Cannot move to the root directory `/`");
        }

        if (srcPath == destPath)
        {
            throw new IOException($"The source and destination path are the same `{srcPath}`");
        }

        MoveDirectoryImpl(ValidatePath(srcPath, nameof(srcPath)), ValidatePath(destPath, nameof(destPath)));
    }

    /// <summary>
    /// Implementation for <see cref="MoveDirectory"/>, <paramref name="srcPath"/> and <paramref name="destPath"/>
    /// are guaranteed to be absolute, not equal and different from root `/`, and validated through <see cref="ValidatePath"/>.
    /// Moves a directory and its contents to a new location.
    /// </summary>
    /// <param name="srcPath">The path of the directory to move.</param>
    /// <param name="destPath">The path to the new location for <paramref name="srcPath"/></param>
    protected abstract void MoveDirectoryImpl(UPath srcPath, UPath destPath);

    /// <inheritdoc />
    public void DeleteDirectory(UPath path, bool isRecursive)
    {
        AssertNotDisposed();
        AssertTrue(CanDeleteDirectories, nameof(DeleteDirectory));
        if (path == UPath.Root)
        {
            throw new UnauthorizedAccessException("Cannot delete root directory `/`");
        }

        DeleteDirectoryImpl(ValidatePath(path), isRecursive);
    }

    /// <summary>
    /// Implementation for <see cref="DeleteDirectory"/>, <paramref name="path"/> is guaranteed to be absolute and different from root path `/` and validated through <see cref="ValidatePath"/>.
    /// Deletes the specified directory and, if indicated, any subdirectories and files in the directory. 
    /// </summary>
    /// <param name="path">The path of the directory to remove.</param>
    /// <param name="isRecursive"><c>true</c> to remove directories, subdirectories, and files in path; otherwise, <c>false</c>.</param>
    protected abstract void DeleteDirectoryImpl(UPath path, bool isRecursive);

    #endregion

    #region File API

    /// <inheritdoc />
    public abstract bool CanCreateFiles { get; }

    /// <inheritdoc />
    public abstract bool CanCopyFiles { get; }

    /// <inheritdoc />
    public abstract bool CanReplaceFiles { get; }

    /// <inheritdoc />
    public abstract bool CanMoveFiles { get; }

    /// <inheritdoc />
    public abstract bool CanDeleteFiles { get; }

    /// <inheritdoc />
    public void CopyFile(UPath srcPath, UPath destPath, bool overwrite)
    {
        AssertNotDisposed();
        AssertTrue(CanCopyFiles, nameof(CopyFile));
        CopyFileImpl(ValidatePath(srcPath, nameof(srcPath)), ValidatePath(destPath, nameof(destPath)), overwrite);
    }

    /// <summary>
    /// Implementation for <see cref="CopyFile"/>, <paramref name="srcPath"/> and <paramref name="destPath"/>
    /// are guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Copies an existing file to a new file. Overwriting a file of the same name is allowed.
    /// </summary>
    /// <param name="srcPath">The path of the file to copy.</param>
    /// <param name="destPath">The path of the destination file. This cannot be a directory.</param>
    /// <param name="overwrite"><c>true</c> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    protected abstract void CopyFileImpl(UPath srcPath, UPath destPath, bool overwrite);

    /// <inheritdoc />
    public void ReplaceFile(UPath srcPath, UPath destPath, UPath destBackupPath, bool ignoreMetadataErrors)
    {
        AssertNotDisposed();
        AssertTrue(CanReplaceFiles, nameof(ReplaceFile));
        srcPath = ValidatePath(srcPath, nameof(srcPath));
        destPath = ValidatePath(destPath, nameof(destPath));
        destBackupPath = ValidatePath(destBackupPath, nameof(destBackupPath), true);

        if (!FileExistsImpl(srcPath))
        {
            throw new FileNotFoundException($"Could not find file `{srcPath}`.");
        }

        if (!FileExistsImpl(destPath))
        {
            throw new FileNotFoundException($"Could not find file `{destPath}`.");
        }

        if (destBackupPath == srcPath)
        {
            throw new IOException($"The source and backup cannot have the same path `{srcPath}`");
        }

        ReplaceFileImpl(srcPath, destPath, destBackupPath, ignoreMetadataErrors);
    }

    /// <summary>
    /// Implementation for <see cref="ReplaceFile"/>, <paramref name="srcPath"/>, <paramref name="destPath"/> and <paramref name="destBackupPath"/>
    /// are guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Replaces the contents of a specified file with the contents of another file, deleting the original file, and creating a backup of the replaced file and optionally ignores merge errors.
    /// </summary>
    /// <param name="srcPath">The path of a file that replaces the file specified by <paramref name="destPath"/>.</param>
    /// <param name="destPath">The path of the file being replaced.</param>
    /// <param name="destBackupPath">The path of the backup file (maybe null, in that case, it doesn't create any backup)</param>
    /// <param name="ignoreMetadataErrors"><c>true</c> to ignore merge errors (such as attributes and access control lists (ACLs)) from the replaced file to the replacement file; otherwise, <c>false</c>.</param>
    protected abstract void ReplaceFileImpl(UPath srcPath, UPath destPath, UPath destBackupPath, bool ignoreMetadataErrors);

    /// <inheritdoc />
    public long GetFileLength(UPath path)
    {
        AssertNotDisposed();
        return GetFileLengthImpl(ValidatePath(path));
    }

    /// <summary>
    /// Implementation for <see cref="GetFileLength"/>, <paramref name="path"/> is guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Gets the size, in bytes, of a file.
    /// </summary>
    /// <param name="path">The path of a file.</param>
    /// <returns>The size, in bytes, of the file</returns>
    protected abstract long GetFileLengthImpl(UPath path);

    /// <inheritdoc />
    public bool FileExists(UPath path)
    {
        AssertNotDisposed();

        // Only case where a null path is allowed
        if (path.IsNull)
        {
            return false;
        }

        return FileExistsImpl(ValidatePath(path));
    }

    /// <summary>
    /// Implementation for <see cref="FileExists"/>, <paramref name="path"/> is guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns><c>true</c> if the caller has the required permissions and path contains the name of an existing file; 
    /// otherwise, <c>false</c>. This method also returns false if path is null, an invalid path, or a zero-length string. 
    /// If the caller does not have sufficient permissions to read the specified file, 
    /// no exception is thrown and the method returns false regardless of the existence of path.</returns>
    protected abstract bool FileExistsImpl(UPath path);

    /// <inheritdoc />
    public void MoveFile(UPath srcPath, UPath destPath)
    {
        AssertNotDisposed();
        AssertTrue(CanMoveFiles, nameof(CanMoveFiles));
        MoveFileImpl(ValidatePath(srcPath, nameof(srcPath)), ValidatePath(destPath, nameof(destPath)));
    }

    /// <summary>
    /// Implementation for <see cref="CopyFile"/>, <paramref name="srcPath"/> and <paramref name="destPath"/>
    /// are guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Moves a specified file to a new location, providing the option to specify a new file name.
    /// </summary>
    /// <param name="srcPath">The path of the file to move.</param>
    /// <param name="destPath">The new path and name for the file.</param>
    protected abstract void MoveFileImpl(UPath srcPath, UPath destPath);

    /// <inheritdoc />
    public void DeleteFile(UPath path)
    {
        AssertNotDisposed();
        AssertTrue(CanDeleteFiles, nameof(DeleteFile));
        DeleteFileImpl(ValidatePath(path));
    }

    /// <summary>
    /// Implementation for <see cref="FileExists"/>, <paramref name="path"/> is guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Deletes the specified file. 
    /// </summary>
    /// <param name="path">The path of the file to be deleted.</param>
    protected abstract void DeleteFileImpl(UPath path);

    /// <inheritdoc />
    public Stream OpenFile(UPath path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.None)
    {
        AssertNotDisposed();
        if (mode == FileMode.Create || mode == FileMode.CreateNew || mode == FileMode.OpenOrCreate)
            AssertTrue(CanCreateFiles, "CreateFile");
        return OpenFileImpl(ValidatePath(path), mode, access, share);
    }

    /// <summary>
    /// Implementation for <see cref="OpenFile"/>, <paramref name="path"/> is guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Opens a file <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
    /// </summary>
    /// <param name="path">The path to the file to open.</param>
    /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
    /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
    /// <returns>A file <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</returns>
    protected abstract Stream OpenFileImpl(UPath path, FileMode mode, FileAccess access, FileShare share);

    /// <inheritdoc />
    public Task<Stream> OpenFileAsync(UPath path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.None)
    {
        AssertNotDisposed();
        if (mode == FileMode.Create || mode == FileMode.CreateNew || mode == FileMode.OpenOrCreate)
            AssertTrue(CanCreateFiles, "CreateFile");
        return OpenFileAsyncImpl(ValidatePath(path), mode, access, share);
    }

    /// <summary>
    /// Implementation for <see cref="OpenFileAsync"/>, <paramref name="path"/> is guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Opens a file <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
    /// </summary>
    /// <param name="path">The path to the file to open.</param>
    /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
    /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
    /// <returns>A file <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</returns>
    protected abstract Task<Stream> OpenFileAsyncImpl(UPath path, FileMode mode, FileAccess access, FileShare share);

    /// <inheritdoc />
    public void SetFileData(UPath savePath, Stream saveData)
    {
        AssertNotDisposed();
        SetFileDataImpl(ValidatePath(savePath), saveData);
    }

    /// <summary>
    /// Implementation for <see cref="SetFileData"/>, <paramref name="savePath"/> is guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// </summary>
    /// <param name="savePath">The path to of the file to set.</param>
    /// <param name="saveData">The data to set to the file.</param>
    protected abstract void SetFileDataImpl(UPath savePath, Stream saveData);

    #endregion

    #region Metadata API

    /// <inheritdoc />
    public ulong GetTotalSize(UPath path)
    {
        AssertNotDisposed();
        return GetTotalSizeImpl(ValidatePath(path));
    }

    /// <summary>
    /// Implementation for <see cref="GetTotalSize"/>.
    /// </summary>
    /// <returns>The total size of this file system.</returns>
    protected abstract ulong GetTotalSizeImpl(UPath directory);

    /// <inheritdoc />
    public FileEntry GetFileEntry(UPath path)
    {
        AssertNotDisposed();
        return GetFileEntryImpl(ValidatePath(path));
    }

    /// <summary>
    /// Implementation for <see cref="GetFileEntry"/>.
    /// </summary>
    /// <param name="path">Path of the file to describe.</param>
    /// <returns>The file entry for the given path.</returns>
    protected abstract FileEntry GetFileEntryImpl(UPath path);

    #endregion

    #region Search API

    /// <summary>
    /// Enumerates file names that match a search pattern in a specified path, without searching subdirectories.
    /// </summary>
    /// <param name="path">The path to the directory to search in.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
    /// <returns>An enumerable collection of file names in the given restrictions.</returns>
    public IEnumerable<UPath> EnumerateFiles(UPath path, string searchPattern = "*") =>
        EnumeratePaths(path, searchPattern, SearchOption.TopDirectoryOnly, SearchTarget.File);

    /// <summary>
    /// Enumerates file names that match a search pattern in a specified path, searching subdirectories.
    /// </summary>
    /// <param name="path">The path to the directory to search in.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
    /// <returns>An enumerable collection of file names in the given restrictions.</returns>
    public IEnumerable<UPath> EnumerateAllFiles(UPath path, string searchPattern = "*") =>
        EnumeratePaths(path, searchPattern, SearchOption.AllDirectories, SearchTarget.File);

    /// <summary>
    /// Enumerates directory names that match a search pattern in a specified path, without searching subdirectories.
    /// </summary>
    /// <param name="path">The path to the directory to search in.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
    /// <returns>An enumerable collection of file names in the given restrictions.</returns>
    public IEnumerable<UPath> EnumerateDirectories(UPath path, string searchPattern = "*") =>
        EnumeratePaths(path, searchPattern, SearchOption.TopDirectoryOnly, SearchTarget.Directory);

    /// <summary>
    /// Enumerates directory names that match a search pattern in a specified path, searching subdirectories.
    /// </summary>
    /// <param name="path">The path to the directory to search in.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
    /// <returns>An enumerable collection of file names in the given restrictions.</returns>
    public IEnumerable<UPath> EnumerateAllDirectories(UPath path, string searchPattern = "*") =>
        EnumeratePaths(path, searchPattern, SearchOption.AllDirectories, SearchTarget.Directory);

    /// <inheritdoc />
    public IEnumerable<UPath> EnumeratePaths(UPath path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SearchTarget searchTarget = SearchTarget.Both)
    {
        AssertNotDisposed();
        if (searchPattern == null) throw new ArgumentNullException(nameof(searchPattern));
        return EnumeratePathsImpl(ValidatePath(path), searchPattern, searchOption, searchTarget);
    }

    /// <summary>
    /// Implementation for <see cref="EnumeratePaths"/>, <paramref name="path"/> is guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Returns an enumerable collection of file names and/or directory names that match a search pattern in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <param name="searchTarget">The search target either <see cref="SearchTarget.Both"/> or only <see cref="SearchTarget.Directory"/> or <see cref="SearchTarget.File"/>.</param>
    /// <returns>An enumerable collection of file-system paths in the directory specified by path and that match the specified search pattern, option and target.</returns>
    protected abstract IEnumerable<UPath> EnumeratePathsImpl(UPath path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget);

    #endregion

    #region Watch API

    /// <inheritdoc />
    public bool CanWatch(UPath path)
    {
        AssertNotDisposed();
        return CanWatchImpl(ValidatePath(path));
    }

    /// <summary>
    /// Implementation for <see cref="CanWatch"/>, <paramref name="path"/> is guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Checks if the file system and <paramref name="path"/> can be watched with <see cref="Watch"/>.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the the path can be watched on this file system.</returns>
    protected virtual bool CanWatchImpl(UPath path)
    {
        return true;
    }

    /// <inheritdoc />
    public IFileSystemWatcher Watch(UPath path)
    {
        AssertNotDisposed();

        var validatedPath = ValidatePath(path);

        if (!CanWatchImpl(validatedPath))
        {
            throw new NotSupportedException($"The file system or path `{validatedPath}` does not support watching");
        }

        return WatchImpl(validatedPath);
    }

    /// <summary>
    /// Implementation for <see cref="Watch"/>, <paramref name="path"/> is guaranteed to be absolute and valudated through <see cref="ValidatePath"/>.
    /// Returns an <see cref="IFileSystemWatcher"/> instance that can be used to watch for changes to files and directories in the given path. The instance must be
    /// configured before events are raised.
    /// </summary>
    /// <param name="path">The path to watch for changes.</param>
    /// <returns>An <see cref="IFileSystemWatcher"/> instance that watches the given path.</returns>
    protected abstract IFileSystemWatcher WatchImpl(UPath path);

    /// <summary>
    /// Get or create the <see cref="FileSystemEventDispatcher{T}"/> for this instance.
    /// </summary>
    /// <returns>The <see cref="FileSystemEventDispatcher{T}"/> for this instance.</returns>
    protected FileSystemEventDispatcher<Watcher.FileSystemWatcher> GetOrCreateDispatcher()
    {
        lock (_dispatcherLock)
        {
            return _dispatcher ??= new FileSystemEventDispatcher<Watcher.FileSystemWatcher>(this);
        }
    }

    #endregion

    #region Path API

    /// <inheritdoc />
    public string ConvertPathToInternal(UPath path)
    {
        AssertNotDisposed();
        return ConvertPathToInternalImpl(ValidatePath(path));
    }

    /// <summary>
    /// Implementation for <see cref="ConvertPathToInternal"/>, <paramref name="path"/> is guaranteed to be absolute and validated through <see cref="ValidatePath"/>.
    /// Converts the specified path to the underlying path used by this <see cref="IFileSystem"/>. In case of a <see cref="PhysicalFileSystem"/>, it 
    /// would represent the actual path on the disk.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The converted system path according to the specified path.</returns>
    protected abstract string ConvertPathToInternalImpl(UPath path);

    /// <inheritdoc />
    public UPath ConvertPathFromInternal(string systemPath)
    {
        AssertNotDisposed();
        if (systemPath == null) throw new ArgumentNullException(nameof(systemPath));
        return ValidatePath(ConvertPathFromInternalImpl(systemPath));
    }
    /// <summary>
    /// Implementation for <see cref="ConvertPathToInternal"/>, <paramref name="innerPath"/> is guaranteed to be not null and return path to be validated through <see cref="ValidatePath"/>.
    /// Converts the specified system path to a <see cref="IFileSystem"/> path.
    /// </summary>
    /// <param name="innerPath">The system path.</param>
    /// <returns>The converted path according to the system path.</returns>
    protected abstract UPath ConvertPathFromInternalImpl(string innerPath);

    /// <summary>
    /// User overridable implementation for <see cref="ValidatePath"/> to validate the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="name">The name.</param>
    /// <returns>The path validated</returns>
    /// <exception cref="System.NotSupportedException">The path cannot contain the `:` character</exception>
    protected virtual UPath ValidatePathImpl(UPath path, string name = "path")
    {
        if (path.FullName.IndexOf(':') >= 0)
        {
            throw new NotSupportedException($"The path `{path}` cannot contain the `:` character");
        }
        return path;
    }

    /// <summary>
    /// Validates the specified path (Check that it is absolute by default)
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="name">The name.</param>
    /// <param name="allowNull">if set to <c>true</c> the path is allowed to be null. <c>false</c> otherwise.</param>  
    /// <returns>The path validated</returns>
    protected UPath ValidatePath(UPath path, string name = "path", bool allowNull = false)
    {
        if (allowNull && path.IsNull)
        {
            return path;
        }
        path.AssertAbsolute(name);

        return ValidatePathImpl(path, name);
    }

    #endregion

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    private void AssertNotDisposed()
    {
        if (IsDisposing || IsDisposed)
        {
            throw new ObjectDisposedException($"This instance `{GetType()}` is already disposed.");
        }
    }

    private void AssertTrue(bool condition, string conditionName)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"`{conditionName}` is not allowed.");
        }
    }

    private void DisposeInternal(bool disposing)
    {
        AssertNotDisposed();
        IsDisposing = true;
        Dispose(disposing);
        IsDisposed = true;
    }
}