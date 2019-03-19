using System;
using System.Collections.Generic;
using System.IO;

namespace Kontract.Interfaces.FileSystem
{
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    public interface IFileSystem : IDisposable
    {
        bool CanCreateDirectories { get; }
        bool CanCreateFiles { get; }
        bool CanDeleteDirectories { get; }
        bool CanDeleteFiles { get; }

        string RootDirectory { get; }

        IEnumerable<string> EnumerateFiles(bool relative = false);
        IEnumerable<string> EnumerateDirectories(bool relative = false);

        IFileSystem GetDirectory(string path);

        bool FileExists(string filename);

        Stream OpenFile(string filename);
        Stream CreateFile(string filename);
        void DeleteFile(string filename);
    }
}
