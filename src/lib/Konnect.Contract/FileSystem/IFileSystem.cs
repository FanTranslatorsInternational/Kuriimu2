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
// - Add boolean properties to enable/disable certain file system functions
// - Add SetFileData method mainly for leveraging into the AfiFileSystem
// - Add OpenFileAsync method mainly for leveraging into the AfiFileSystem
// - Add Clone method to assign a new stream manager in our runtime
// - Remove the filesystem watcher API
// - Remove the metadata attribute API

using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Enums.FileSystem;
using Konnect.Contract.Management.Streams;

namespace Konnect.Contract.FileSystem;

/// <summary>
/// Interface of a FileSystem.
/// </summary>
public interface IFileSystem : IDisposable
{
    #region Directory API

    /// <summary>
    /// Determines if the file system can create directories.
    /// </summary>
    bool CanCreateDirectories { get; }

    /// <summary>
    /// Determines if the file system can delete directories.
    /// </summary>
    bool CanDeleteDirectories { get; }

    /// <summary>
    /// Determines if the file system can move directories.
    /// </summary>
    bool CanMoveDirectories { get; }

    /// <summary>
    /// Determines whether the given path refers to an existing directory on disk.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns><c>true</c> if the given path refers to an existing directory on disk, <c>false</c> otherwise.</returns>
    bool DirectoryExists(UPath path);

    /// <summary>
    /// Creates all directories and subdirectories in the specified path unless they already exist.
    /// </summary>
    /// <param name="path">The directory to create.</param>
    void CreateDirectory(UPath path);

    /// <summary>
    /// Moves a directory and its contents to a new location.
    /// </summary>
    /// <param name="srcPath">The path of the directory to move.</param>
    /// <param name="destPath">The path to the new location for <paramref name="srcPath"/></param>
    void MoveDirectory(UPath srcPath, UPath destPath);

    /// <summary>
    /// Deletes the specified directory and, if indicated, any subdirectories and files in the directory. 
    /// </summary>
    /// <param name="path">The path of the directory to remove.</param>
    /// <param name="isRecursive"><c>true</c> to remove directories, subdirectories, and files in path; otherwise, <c>false</c>.</param>
    void DeleteDirectory(UPath path, bool isRecursive);

    #endregion

    #region File API

    /// <summary>
    /// Determines if the file system can create files.
    /// </summary>
    bool CanCreateFiles { get; }

    /// <summary>
    /// Determines if the file system can copy files.
    /// </summary>
    bool CanCopyFiles { get; }

    /// <summary>
    /// Determines if the file system can replace files.
    /// </summary>
    bool CanReplaceFiles { get; }

    /// <summary>
    /// Determines if the file system can move files.
    /// </summary>
    bool CanMoveFiles { get; }

    /// <summary>
    /// Determines if the file system can delete files.
    /// </summary>
    bool CanDeleteFiles { get; }

    /// <summary>
    /// Copies an existing file to a new file. Overwriting a file of the same name is allowed.
    /// </summary>
    /// <param name="srcPath">The path of the file to copy.</param>
    /// <param name="destPath">The path of the destination file. This cannot be a directory.</param>
    /// <param name="overwrite"><c>true</c> if the destination file can be overwritten; otherwise, <c>false</c>.</param>
    void CopyFile(UPath srcPath, UPath destPath, bool overwrite);

    /// <summary>
    /// Replaces the contents of a specified file with the contents of another file, deleting the original file, and creating a backup of the replaced file and optionally ignores merge errors.
    /// </summary>
    /// <param name="srcPath">The path of a file that replaces the file specified by <paramref name="destPath"/>.</param>
    /// <param name="destPath">The path of the file being replaced.</param>
    /// <param name="destBackupPath">The path of the backup file (maybe null, in that case, it doesn't create any backup)</param>
    /// <param name="ignoreMetadataErrors"><c>true</c> to ignore merge errors (such as attributes and access control lists (ACLs)) from the replaced file to the replacement file; otherwise, <c>false</c>.</param>
    void ReplaceFile(UPath srcPath, UPath destPath, UPath destBackupPath, bool ignoreMetadataErrors);

    /// <summary>
    /// Gets the size, in bytes, of a file.
    /// </summary>
    /// <param name="path">The path of a file.</param>
    /// <returns>The size, in bytes, of the file</returns>
    long GetFileLength(UPath path);

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns><c>true</c> if the caller has the required permissions and path contains the name of an existing file; 
    /// otherwise, <c>false</c>. This method also returns false if path is null, an invalid path, or a zero-length string. 
    /// If the caller does not have sufficient permissions to read the specified file, 
    /// no exception is thrown and the method returns false regardless of the existence of path.</returns>
    bool FileExists(UPath path);

    /// <summary>
    /// Moves a specified file to a new location, providing the option to specify a new file name.
    /// </summary>
    /// <param name="srcPath">The path of the file to move.</param>
    /// <param name="destPath">The new path and name for the file.</param>
    void MoveFile(UPath srcPath, UPath destPath);

    /// <summary>
    /// Deletes the specified file. 
    /// </summary>
    /// <param name="path">The path of the file to be deleted.</param>
    void DeleteFile(UPath path);

    /// <summary>
    /// Opens a file <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
    /// </summary>
    /// <param name="path">The path to the file to open.</param>
    /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
    /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
    /// <returns>A file <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</returns>
    Stream OpenFile(UPath path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read);

    /// <summary>
    /// Opens a file <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
    /// </summary>
    /// <param name="path">The path to the file to open.</param>
    /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
    /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
    /// <returns>A file <see cref="Stream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</returns>
    Task<Stream> OpenFileAsync(UPath path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read);

    /// <summary>
    /// Sets the data to a specified file and overwrites the original.
    /// </summary>
    /// <param name="savePath">The path of the file to overwrite.</param>
    /// <param name="saveData">The data to overwrite the file with.</param>
    void SetFileData(UPath savePath, Stream saveData);

    #endregion

    #region Search API

    /// <summary>
    /// Enumerates file names that match a search pattern in a specified path, without searching subdirectories.
    /// </summary>
    /// <param name="path">The path to the directory to search in.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
    /// <returns>An enumerable collection of file names in the given restrictions.</returns>
    public IEnumerable<UPath> EnumerateFiles(UPath path, string searchPattern = "*");

    /// <summary>
    /// Enumerates file names that match a search pattern in a specified path, searching subdirectories.
    /// </summary>
    /// <param name="path">The path to the directory to search in.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
    /// <returns>An enumerable collection of file names in the given restrictions.</returns>
    public IEnumerable<UPath> EnumerateAllFiles(UPath path, string searchPattern = "*");

    /// <summary>
    /// Enumerates directory names that match a search pattern in a specified path, without searching subdirectories.
    /// </summary>
    /// <param name="path">The path to the directory to search in.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
    /// <returns>An enumerable collection of file names in the given restrictions.</returns>
    public IEnumerable<UPath> EnumerateDirectories(UPath path, string searchPattern = "*");

    /// <summary>
    /// Enumerates directory names that match a search pattern in a specified path, searching subdirectories.
    /// </summary>
    /// <param name="path">The path to the directory to search in.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
    /// <returns>An enumerable collection of file names in the given restrictions.</returns>
    public IEnumerable<UPath> EnumerateAllDirectories(UPath path, string searchPattern = "*");

    /// <summary>
    /// Returns an enumerable collection of file names and/or directory names that match a search pattern in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="path">The path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <param name="searchTarget">The search target either <see cref="SearchTarget.Both"/> or only <see cref="Directory"/> or <see cref="SearchTarget.File"/>.</param>
    /// <returns>An enumerable collection of file-system paths in the directory specified by path and that match the specified search pattern, option and target.</returns>
    IEnumerable<UPath> EnumeratePaths(UPath path, string searchPattern = "*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly, SearchTarget searchTarget = SearchTarget.Both);

    #endregion

    #region Watch API

    /// <summary>
    /// Checks if the file system and <paramref name="path"/> can be watched with <see cref="Watch"/>.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the the path can be watched on this file system.</returns>
    bool CanWatch(UPath path);

    /// <summary>
    /// Returns an <see cref="IFileSystemWatcher"/> instance that can be used to watch for changes to files and directories in the given path. The instance must be
    /// configured before events are raised.
    /// </summary>
    /// <param name="path">The path to watch for changes.</param>
    /// <returns>An <see cref="IFileSystemWatcher"/> instance that watches the given path.</returns>
    IFileSystemWatcher Watch(UPath path);

    #endregion

    #region Path API

    /// <summary>
    /// Converts the specified path to the underlying path used by this <see cref="IFileSystem"/>. In case of a PhysicalFileSystem, it 
    /// would represent the actual path on the disk.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The converted system path according to the specified path.</returns>
    string ConvertPathToInternal(UPath path);

    /// <summary>
    /// Converts the specified system path to a <see cref="IFileSystem"/> path.
    /// </summary>
    /// <param name="systemPath">The system path.</param>
    /// <returns>The converted path according to the system path.</returns>
    UPath ConvertPathFromInternal(string systemPath);

    #endregion

    #region Clone API

    /// <summary>
    /// Clones this instance with a new <see cref="IStreamManager"/>.
    /// </summary>
    /// <param name="streamManager">The new stream manager to assign.</param>
    /// <returns>The cloned file system.</returns>
    IFileSystem Clone(IStreamManager streamManager);

    #endregion

    #region Metadata API

    /// <summary>
    /// Gets the total size of this file system.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The total size.</returns>
    ulong GetTotalSize(UPath path);

    /// <summary>
    /// Gets an object describing the file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The <see cref="FileEntry"/> for the file.</returns>
    FileEntry GetFileEntry(UPath path);

    #endregion
}