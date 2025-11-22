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

namespace Konnect.FileSystem;

/// <summary>
/// Provides an abstract base <see cref="IFileSystem"/> for composing a filesystem with another FileSystem. 
/// This implementation delegates by default its implementation to the filesystem passed to the constructor.
/// </summary>
public abstract class ComposeFileSystem : IFileSystem
{
    protected bool Owned { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComposeFileSystem"/> class.
    /// </summary>
    /// <param name="fileSystem">The delegated file system (can be null).</param>
    /// <param name="owned">True if <paramref name="fileSystem"/> should be disposed when this instance is disposed.</param>
    protected ComposeFileSystem(IFileSystem fileSystem, bool owned = true)
    {
        NextFileSystem = fileSystem;
        Owned = owned;
    }

    public void Dispose()
    {
        if (Owned)
        {
            NextFileSystem?.Dispose();
        }
    }

    /// <summary>
    /// Gets the next delegated file system (may be null).
    /// </summary>
    protected IFileSystem NextFileSystem { get; }

    /// <summary>
    /// Gets the next delegated file system or throws an error if it is null.
    /// </summary>
    protected IFileSystem NextFileSystemSafe
    {
        get
        {
            if (NextFileSystem == null)
            {
                throw new InvalidOperationException("The delegate filesystem for this instance is null.");
            }
            return NextFileSystem;
        }
    }

    /// <inheritdoc />
    public virtual IFileSystem Clone(IStreamManager streamManager)
    {
        return NextFileSystemSafe.Clone(streamManager);
    }

    // ----------------------------------------------
    // Directory API
    // ----------------------------------------------

    /// <inheritdoc />
    public bool CanCreateDirectories => NextFileSystemSafe.CanCreateDirectories;

    /// <inheritdoc />
    public bool CanMoveDirectories => NextFileSystemSafe.CanMoveDirectories;

    /// <inheritdoc />
    public bool CanDeleteDirectories => NextFileSystemSafe.CanDeleteDirectories;

    /// <inheritdoc />
    public void CreateDirectory(UPath path)
    {
        NextFileSystemSafe.CreateDirectory(ConvertPathToDelegate(path));
    }

    /// <inheritdoc />
    public bool DirectoryExists(UPath path)
    {
        return NextFileSystemSafe.DirectoryExists(ConvertPathToDelegate(path));
    }

    /// <inheritdoc />
    public void MoveDirectory(UPath srcPath, UPath destPath)
    {
        NextFileSystemSafe.MoveDirectory(ConvertPathToDelegate(srcPath), ConvertPathToDelegate(destPath));
    }

    /// <inheritdoc />
    public void DeleteDirectory(UPath path, bool isRecursive)
    {
        NextFileSystemSafe.DeleteDirectory(ConvertPathToDelegate(path), isRecursive);
    }

    // ----------------------------------------------
    // File API
    // ----------------------------------------------

    /// <inheritdoc />
    public bool CanCreateFiles => NextFileSystemSafe.CanCreateFiles;

    /// <inheritdoc />
    public bool CanCopyFiles => NextFileSystemSafe.CanCopyFiles;

    /// <inheritdoc />
    public bool CanMoveFiles => NextFileSystemSafe.CanMoveFiles;

    /// <inheritdoc />
    public bool CanReplaceFiles => NextFileSystemSafe.CanReplaceFiles;

    /// <inheritdoc />
    public bool CanDeleteFiles => NextFileSystemSafe.CanDeleteFiles;

    /// <inheritdoc />
    public void CopyFile(UPath srcPath, UPath destPath, bool overwrite)
    {
        NextFileSystemSafe.CopyFile(ConvertPathToDelegate(srcPath), ConvertPathToDelegate(destPath), overwrite);
    }

    /// <inheritdoc />
    public void ReplaceFile(UPath srcPath, UPath destPath, UPath destBackupPath,
        bool ignoreMetadataErrors)
    {
        NextFileSystemSafe.ReplaceFile(ConvertPathToDelegate(srcPath), ConvertPathToDelegate(destPath), destBackupPath.IsNull ? destBackupPath : ConvertPathToDelegate(destBackupPath), ignoreMetadataErrors);
    }

    /// <inheritdoc />
    public long GetFileLength(UPath path)
    {
        return NextFileSystemSafe.GetFileLength(ConvertPathToDelegate(path));
    }

    /// <inheritdoc />
    public bool FileExists(UPath path)
    {
        return NextFileSystemSafe.FileExists(ConvertPathToDelegate(path));
    }

    /// <inheritdoc />
    public void MoveFile(UPath srcPath, UPath destPath)
    {
        NextFileSystemSafe.MoveFile(ConvertPathToDelegate(srcPath), ConvertPathToDelegate(destPath));
    }

    /// <inheritdoc />
    public void DeleteFile(UPath path)
    {
        NextFileSystemSafe.DeleteFile(ConvertPathToDelegate(path));
    }

    /// <inheritdoc />
    public Stream OpenFile(UPath path, FileMode mode, FileAccess access, FileShare share = FileShare.None)
    {
        return NextFileSystemSafe.OpenFile(ConvertPathToDelegate(path), mode, access, share);
    }

    /// <inheritdoc />
    public Task<Stream> OpenFileAsync(UPath path, FileMode mode, FileAccess access, FileShare share = FileShare.None)
    {
        return NextFileSystemSafe.OpenFileAsync(ConvertPathToDelegate(path), mode, access, share);
    }

    /// <inheritdoc />
    public void SetFileData(UPath savePath, Stream saveData)
    {
        NextFileSystemSafe.SetFileData(ConvertPathToDelegate(savePath), saveData);
    }

    // ----------------------------------------------
    // Metadata API
    // ----------------------------------------------

    /// <inheritdoc />
    public ulong GetTotalSize(UPath path)
    {
        return NextFileSystemSafe.GetTotalSize(ConvertPathToDelegate(path));
    }

    /// <inheritdoc />
    public FileEntry GetFileEntry(UPath path)
    {
        return NextFileSystemSafe.GetFileEntry(ConvertPathToDelegate(path));
    }

    // ----------------------------------------------
    // Search API
    // ----------------------------------------------

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
    public IEnumerable<UPath> EnumeratePaths(UPath path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
    {
        foreach (var subPath in NextFileSystemSafe.EnumeratePaths(ConvertPathToDelegate(path), searchPattern, searchOption, searchTarget))
        {
            yield return ConvertPathFromDelegate(subPath);
        }
    }

    // ----------------------------------------------
    // -watch API
    // ----------------------------------------------

    /// <inheritdoc />
    public bool CanWatch(UPath path)
    {
        return NextFileSystemSafe.CanWatch(ConvertPathToDelegate(path));
    }

    /// <inheritdoc />
    public IFileSystemWatcher Watch(UPath path)
    {
        return NextFileSystemSafe.Watch(ConvertPathToDelegate(path));
    }

    // ----------------------------------------------
    // Path API
    // ----------------------------------------------

    /// <inheritdoc />
    public string ConvertPathToInternal(UPath path)
    {
        return NextFileSystemSafe.ConvertPathToInternal(ConvertPathToDelegate(path));
    }

    /// <inheritdoc />
    public UPath ConvertPathFromInternal(string innerPath)
    {
        return ConvertPathFromDelegate(NextFileSystemSafe.ConvertPathFromInternal(innerPath));
    }

    /// <summary>
    /// Converts the specified path to the path supported by the underlying <see cref="NextFileSystem"/>
    /// </summary>
    /// <param name="path">The path exposed by this filesystem</param>
    /// <returns>A new path translated to the delegate path</returns>
    protected abstract UPath ConvertPathToDelegate(UPath path);

    /// <summary>
    /// Converts the specified delegate path to the path exposed by this filesystem.
    /// </summary>
    /// <param name="path">The path used by the underlying <see cref="NextFileSystem"/></param>
    /// <returns>A new path translated to this filesystem</returns>
    protected abstract UPath ConvertPathFromDelegate(UPath path);
}