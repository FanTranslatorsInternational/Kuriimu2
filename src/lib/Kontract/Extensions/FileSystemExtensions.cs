using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract.Interfaces.FileSystem;
using Kontract.Models.FileSystem;

namespace Kontract.Extensions
{
    public static class FileSystemExtensions
    {
        /// <summary>
        /// Enumerates file names that match a search pattern in a specified path, without searching subdirectories.
        /// </summary>
        /// <param name="fileSystem">The file-system to search in.</param>
        /// <param name="path">The path to the directory to search in.</param>
        /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
        /// <returns>An enumerable collection of file names in the given restrictions.</returns>
        public static IEnumerable<UPath> EnumerateFiles(this IFileSystem fileSystem, UPath path, string searchPattern = "*") =>
            fileSystem.EnumeratePaths(path, searchPattern, SearchOption.TopDirectoryOnly, SearchTarget.File);

        /// <summary>
        /// Enumerates file names that match a search pattern in a specified path, searching subdirectories.
        /// </summary>
        /// <param name="fileSystem">The file-system to search in.</param>
        /// <param name="path">The path to the directory to search in.</param>
        /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
        /// <returns>An enumerable collection of file names in the given restrictions.</returns>
        public static IEnumerable<UPath> EnumerateAllFiles(this IFileSystem fileSystem, UPath path, string searchPattern = "*") =>
            fileSystem.EnumeratePaths(path, searchPattern, SearchOption.AllDirectories, SearchTarget.File);

        /// <summary>
        /// Enumerates file entries that match a search pattern in a specified path, searching subdirectories.
        /// </summary>
        /// <param name="fileSystem">The file-system to search in.</param>
        /// <param name="path">The path to the directory to search in.</param>
        /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
        /// <returns>An enumerable collection of file entries in the given restrictions.</returns>
        public static IEnumerable<FileEntry> EnumerateAllFileEntries(this IFileSystem fileSystem, UPath path, string searchPattern = "*") =>
            fileSystem.EnumerateAllFiles(path, searchPattern).Select(fileSystem.GetFileEntry);

        /// <summary>
        /// Enumerates directory names that match a search pattern in a specified path, without searching subdirectories.
        /// </summary>
        /// <param name="fileSystem">The file-system to search in.</param>
        /// <param name="path">The path to the directory to search in.</param>
        /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
        /// <returns>An enumerable collection of file names in the given restrictions.</returns>
        public static IEnumerable<UPath> EnumerateDirectories(this IFileSystem fileSystem, UPath path, string searchPattern = "*") =>
            fileSystem.EnumeratePaths(path, searchPattern, SearchOption.TopDirectoryOnly, SearchTarget.Directory);

        /// <summary>
        /// Enumerates directory names that match a search pattern in a specified path, searching subdirectories.
        /// </summary>
        /// <param name="fileSystem">The file-system to search in.</param>
        /// <param name="path">The path to the directory to search in.</param>
        /// <param name="searchPattern">The search string to match against file-system entries in path. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.</param>
        /// <returns>An enumerable collection of file names in the given restrictions.</returns>
        public static IEnumerable<UPath> EnumerateAllDirectories(this IFileSystem fileSystem, UPath path, string searchPattern = "*") =>
            fileSystem.EnumeratePaths(path, searchPattern, SearchOption.AllDirectories, SearchTarget.Directory);
    }
}
