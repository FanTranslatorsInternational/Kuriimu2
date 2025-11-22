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

using System.Text;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Enums.FileSystem;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Streams;
using Konnect.Extensions;
using Konnect.FileSystem.Watcher;

namespace Konnect.FileSystem;

/// <summary>
/// Provides a <see cref="IFileSystem"/> for the physical filesystem.
/// </summary>
public class PhysicalFileSystem : FileSystem
{
    private const string DrivePrefixOnWindows = "/mnt/";
    private static readonly UPath PathDrivePrefixOnWindows = new UPath(DrivePrefixOnWindows);
#if NETSTANDARD
        private static readonly bool IsOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
    private static readonly bool IsOnWindows = CheckIsOnWindows();

    private static bool CheckIsOnWindows()
    {
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Xbox:
            case PlatformID.Win32NT:
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.WinCE:
                return true;
        }
        return false;
    }
#endif

    /// <inheritdoc />
    public PhysicalFileSystem(IStreamManager streamManager) :
        base(streamManager)
    {
    }

    private PhysicalFileSystem(IStreamManager streamManager, IList<Watcher.FileSystemWatcher> watchers) :
        base(streamManager)
    {
        foreach (var watcher in watchers)
            GetOrCreateDispatcher().Add(watcher);
    }

    /// <inheritdoc />
    public override IFileSystem Clone(IStreamManager streamManager)
    {
        return new PhysicalFileSystem(streamManager, GetOrCreateDispatcher().Get());
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
        if (IsWithinSpecialDirectory(path))
        {
            throw new UnauthorizedAccessException($"Cannot create a directory in the path `{path}`");
        }

        Directory.CreateDirectory(ConvertPathToInternal(path));
    }

    /// <inheritdoc />
    protected override bool DirectoryExistsImpl(UPath path)
    {
        return IsWithinSpecialDirectory(path) ? SpecialDirectoryExists(path) : Directory.Exists(ConvertPathToInternal(path));
    }

    /// <inheritdoc />
    protected override void MoveDirectoryImpl(UPath srcPath, UPath destPath)
    {
        if (IsOnWindows)
        {
            if (IsWithinSpecialDirectory(srcPath))
            {
                if (!SpecialDirectoryExists(srcPath))
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path `{srcPath}`.");
                }

                throw new UnauthorizedAccessException($"Cannot move the special directory `{srcPath}`");
            }

            if (IsWithinSpecialDirectory(destPath))
            {
                if (!SpecialDirectoryExists(destPath))
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path `{destPath}`.");
                }
                throw new UnauthorizedAccessException($"Cannot move to the special directory `{destPath}`");
            }
        }

        var systemSrcPath = ConvertPathToInternal(srcPath);
        var systemDestPath = ConvertPathToInternal(destPath);

        // If the source path is a file
        var fileInfo = new FileInfo(systemSrcPath);
        if (fileInfo.Exists)
        {
            throw new IOException($"The source `{srcPath}` is not a directory");
        }

        Directory.Move(systemSrcPath, systemDestPath);
    }

    /// <inheritdoc />
    protected override void DeleteDirectoryImpl(UPath path, bool isRecursive)
    {
        if (IsWithinSpecialDirectory(path))
        {
            if (!SpecialDirectoryExists(path))
            {
                throw new DirectoryNotFoundException($"Could not find a part of the path `{path}`.");
            }
            throw new UnauthorizedAccessException($"Cannot delete directory `{path}`");
        }

        Directory.Delete(ConvertPathToInternal(path), isRecursive);
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
        if (IsWithinSpecialDirectory(srcPath))
        {
            throw new UnauthorizedAccessException($"The access to `{srcPath}` is denied");
        }
        if (IsWithinSpecialDirectory(destPath))
        {
            throw new UnauthorizedAccessException($"The access to `{destPath}` is denied");
        }

        File.Copy(ConvertPathToInternal(srcPath), ConvertPathToInternal(destPath), overwrite);
    }

    /// <inheritdoc />
    protected override void ReplaceFileImpl(UPath srcPath, UPath destPath, UPath destBackupPath, bool ignoreMetadataErrors)
    {
        if (IsWithinSpecialDirectory(srcPath))
        {
            throw new UnauthorizedAccessException($"The access to `{srcPath}` is denied");
        }
        if (IsWithinSpecialDirectory(destPath))
        {
            throw new UnauthorizedAccessException($"The access to `{destPath}` is denied");
        }
        if (!destBackupPath.IsNull && IsWithinSpecialDirectory(destBackupPath))
        {
            throw new UnauthorizedAccessException($"The access to `{destBackupPath}` is denied");
        }

        if (!destBackupPath.IsNull)
        {
            CopyFileImpl(destPath, destBackupPath, true);
        }

        CopyFileImpl(srcPath, destPath, true);

        DeleteFileImpl(srcPath);

        // TODO: Add atomic version using File.Replace coming with .NET Standard 2.0
    }

    /// <inheritdoc />
    protected override long GetFileLengthImpl(UPath path)
    {
        if (IsWithinSpecialDirectory(path))
        {
            throw new UnauthorizedAccessException($"The access to `{path}` is denied");
        }

        return new FileInfo(ConvertPathToInternal(path)).Length;
    }

    /// <inheritdoc />
    protected override bool FileExistsImpl(UPath path)
    {
        return !IsWithinSpecialDirectory(path) && File.Exists(ConvertPathToInternal(path));
    }

    /// <inheritdoc />
    protected override void MoveFileImpl(UPath srcPath, UPath destPath)
    {
        if (IsWithinSpecialDirectory(srcPath))
        {
            throw new UnauthorizedAccessException($"The access to `{srcPath}` is denied");
        }

        if (IsWithinSpecialDirectory(destPath))
        {
            throw new UnauthorizedAccessException($"The access to `{destPath}` is denied");
        }

        File.Move(ConvertPathToInternal(srcPath), ConvertPathToInternal(destPath));
    }

    /// <inheritdoc />
    protected override void DeleteFileImpl(UPath path)
    {
        if (IsWithinSpecialDirectory(path))
        {
            throw new UnauthorizedAccessException($"The access to `{path}` is denied");
        }

        File.Delete(ConvertPathToInternal(path));
    }

    /// <inheritdoc />
    protected override Stream OpenFileImpl(UPath path, FileMode mode, FileAccess access,
        FileShare share)
    {
        if (IsWithinSpecialDirectory(path))
        {
            throw new UnauthorizedAccessException($"The access to `{path}` is denied");
        }

        // Create directory if not existing
        var directory = path.GetDirectory();
        if (!DirectoryExists(directory))
            CreateDirectory(directory);

        // Open file
        Stream file;
        if (mode == FileMode.Create || mode == FileMode.CreateNew)
            file = File.Open(ConvertPathToInternal(path), mode);
        else
            file = File.Open(ConvertPathToInternal(path), mode, access, share);
        StreamManager.Register(file);

        GetOrCreateDispatcher().RaiseOpened(path);

        return file;
    }

    /// <inheritdoc />
    protected override Task<Stream> OpenFileAsyncImpl(UPath path, FileMode mode, FileAccess access,
        FileShare share)
    {
        if (IsWithinSpecialDirectory(path))
        {
            throw new UnauthorizedAccessException($"The access to `{path}` is denied");
        }

        string convertedPath = ConvertPathToInternal(path);
        if (mode is FileMode.Create or FileMode.CreateNew or FileMode.OpenOrCreate)
            CreateDirectoryImpl(path.GetDirectory());

        Stream file = File.Open(convertedPath, mode, access, share);
        StreamManager.Register(file);

        GetOrCreateDispatcher().RaiseOpened(path);

        return Task.FromResult((Stream)file);
    }

    /// <inheritdoc />
    protected override void SetFileDataImpl(UPath savePath, Stream saveData)
    {
        // 1. Create file at destination
        var createdFile = File.Create(ConvertPathToInternal(savePath));

        // 2. Copy all content of the file data to the destination file
        var bkPos = saveData.Position;
        saveData.Position = 0;
        saveData.CopyTo(createdFile);
        saveData.Position = bkPos;

        createdFile.Close();
    }

    // ----------------------------------------------
    // Metadata API
    // ----------------------------------------------

    /// <inheritdoc />
    protected override ulong GetTotalSizeImpl(UPath directory)
    {
        return GetDirectorySize(new DirectoryInfo(ConvertPathToInternalImpl(directory)));
    }

    /// <inheritdoc />
    protected override FileEntry GetFileEntryImpl(UPath path)
    {
        var fileInfo = new FileInfo(ConvertPathToInternalImpl(path));
        return new FileEntry
        {
            Path = fileInfo.FullName,
            Size = fileInfo.Length
        };
    }

    // ----------------------------------------------
    // Search API
    // ----------------------------------------------

    /// <inheritdoc />
    protected override IEnumerable<UPath> EnumeratePathsImpl(UPath path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
    {
        // Special case for Windows as we need to provide list for:
        // - the root folder / (which should just return the /drive folder)
        // - the drive folders /drive/c, drive/e...etc.
        var search = SearchPattern.Parse(ref path, ref searchPattern);
        if (IsOnWindows)
        {
            if (IsWithinSpecialDirectory(path))
            {
                if (!SpecialDirectoryExists(path))
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path `{path}`.");
                }

                var searchForDirectory = searchTarget == SearchTarget.Both || searchTarget == SearchTarget.Directory;

                // Only sub folder "/drive/" on root folder /
                if (path == UPath.Root)
                {
                    if (!searchForDirectory)
                        yield break;

                    yield return PathDrivePrefixOnWindows;

                    if (searchOption != SearchOption.AllDirectories)
                        yield break;

                    foreach (var subPath in EnumeratePathsImpl(PathDrivePrefixOnWindows, searchPattern, searchOption, searchTarget))
                    {
                        yield return subPath;
                    }

                    yield break;
                }

                // When listing for /drive, return the list of drives available
                if (path == PathDrivePrefixOnWindows)
                {
                    var pathDrives = new List<UPath>();
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.Name.Length < 2 || drive.Name[1] != ':')
                        {
                            continue;
                        }

                        var pathDrive = PathDrivePrefixOnWindows / char.ToLowerInvariant(drive.Name[0]).ToString();

                        if (search.Match(pathDrive))
                        {
                            pathDrives.Add(pathDrive);

                            if (searchForDirectory)
                            {
                                yield return pathDrive;
                            }
                        }
                    }

                    if (searchOption == SearchOption.AllDirectories)
                    {
                        foreach (var pathDrive in pathDrives)
                        {
                            foreach (var subPath in EnumeratePathsImpl(pathDrive, searchPattern, searchOption, searchTarget))
                            {
                                yield return subPath;
                            }
                        }
                    }

                    yield break;
                }
            }
        }

        IEnumerable<string> results;
        switch (searchTarget)
        {
            case SearchTarget.File:
                results = Directory.EnumerateFiles(ConvertPathToInternal(path), searchPattern, searchOption);
                break;

            case SearchTarget.Directory:
                results = Directory.EnumerateDirectories(ConvertPathToInternal(path), searchPattern, searchOption);
                break;

            case SearchTarget.Both:
                results = Directory.EnumerateFileSystemEntries(ConvertPathToInternal(path), searchPattern, searchOption);
                break;

            default:
                yield break;
        }

        foreach (var subPath in results)
        {
            // Windows will truncate the search pattern's extension to three characters if the filesystem
            // has 8.3 paths enabled. This means searching for *.docx will list *.doc as well which is
            // not what we want. Check against the search pattern again to filter out those false results.
            if (!IsOnWindows || search.Match(Path.GetFileName(subPath)))
            {
                yield return ConvertPathFromInternal(subPath);
            }
        }
    }

    // ----------------------------------------------
    // Watch API
    // ----------------------------------------------

    /// <inheritdoc />
    protected override bool CanWatchImpl(UPath path)
    {
        if (IsWithinSpecialDirectory(path))
        {
            return SpecialDirectoryExists(path);
        }

        return DirectoryExists(path);
    }

    /// <inheritdoc />
    protected override IFileSystemWatcher WatchImpl(UPath path)
    {
        if (IsWithinSpecialDirectory(path))
        {
            throw new UnauthorizedAccessException($"The access to `{path}` is denied");
        }

        var watcher = new PhysicalFileSystemWatcher(this, path);
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
        var absolutePath = path.FullName;

        if (IsOnWindows)
        {
            if (!absolutePath.StartsWith(DrivePrefixOnWindows) ||
                absolutePath.Length == DrivePrefixOnWindows.Length ||
                !IsDriveLetter(absolutePath[DrivePrefixOnWindows.Length]))
                throw new ArgumentException($"A path on Windows must start by `{DrivePrefixOnWindows}` followed by the drive letter");

            var driveLetter = char.ToUpper(absolutePath[DrivePrefixOnWindows.Length]);
            if (absolutePath.Length != DrivePrefixOnWindows.Length + 1 &&
                absolutePath[DrivePrefixOnWindows.Length + 1] !=
                UPath.DirectorySeparator)
                throw new ArgumentException($"The driver letter `/{DrivePrefixOnWindows}{absolutePath[DrivePrefixOnWindows.Length]}` must be followed by a `/` or nothing in the path -> `{absolutePath}`");

            var builder = new StringBuilder();
            builder.Append(driveLetter).Append(":\\");
            if (absolutePath.Length > DrivePrefixOnWindows.Length + 1)
                builder.Append(absolutePath.Replace(UPath.DirectorySeparator, '\\').Substring(DrivePrefixOnWindows.Length + 2));

            var result = builder.ToString();
            builder.Length = 0;
            return result;
        }
        return absolutePath;
    }

    /// <inheritdoc />
    protected override UPath ConvertPathFromInternalImpl(string innerPath)
    {
        if (IsOnWindows)
        {
            // We currently don't support special Windows files (\\.\ \??\  DosDevices...etc.)
            if (innerPath.StartsWith(@"\\") || innerPath.StartsWith(@"\?"))
                throw new NotSupportedException($"Path starting with `\\\\` or `\\?` are not supported -> `{innerPath}` ");

            var absolutePath = Path.GetFullPath(innerPath);
            var driveIndex = absolutePath.IndexOf(":\\", StringComparison.Ordinal);
            if (driveIndex != 1)
                throw new ArgumentException($"Expecting a drive for the path `{absolutePath}`");

            var builder = new StringBuilder();
            builder.Append(DrivePrefixOnWindows).Append(char.ToLowerInvariant(absolutePath[0])).Append('/');
            if (absolutePath.Length > 2)
                builder.Append(absolutePath.Substring(2));

            var result = builder.ToString();
            builder.Length = 0;
            return new UPath(result);
        }
        return innerPath;
    }

    private static bool IsWithinSpecialDirectory(UPath path)
    {
        if (!IsOnWindows)
        {
            return false;
        }

        var parentDirectory = path.GetDirectory();
        return path == PathDrivePrefixOnWindows ||
               path == UPath.Root ||
               parentDirectory == PathDrivePrefixOnWindows ||
               parentDirectory == UPath.Root;
    }

    private static bool SpecialDirectoryExists(UPath path)
    {
        // /drive or / can be read
        if (path == PathDrivePrefixOnWindows || path == UPath.Root)
        {
            return true;
        }

        // If /xxx, invalid (parent folder is /)
        var parentDirectory = path.GetDirectory();
        if (parentDirectory == UPath.Root)
        {
            return false;
        }

        var dirName = path.GetName();
        // Else check that we have a valid drive path (e.g /drive/c)
        return parentDirectory == PathDrivePrefixOnWindows &&
               dirName.Length == 1 &&
               DriveInfo.GetDrives().Any(p => char.ToLowerInvariant(p.Name[0]) == char.ToLowerInvariant(dirName[0]));
    }

    private static bool IsDriveLetter(char c)
    {
        return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
    }

    /// <summary>
    /// Calculates the size of a given directory.
    /// </summary>
    /// <param name="directory">The directory to calculate from.</param>
    /// <returns>The size of the directory.</returns>
    private static ulong GetDirectorySize(DirectoryInfo directory)
    {
        ulong size = 0;

        // Add file sizes
        var fileInfos = directory.GetFiles();
        foreach (var fileInfo in fileInfos)
            size += (ulong)fileInfo.Length;

        // Add sub directory sizes
        var directoryInfos = directory.GetDirectories();
        foreach (var directoryInfo in directoryInfos)
            size += GetDirectorySize(directoryInfo);

        return size;
    }
}