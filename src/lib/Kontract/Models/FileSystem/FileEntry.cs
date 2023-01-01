namespace Kontract.Models.FileSystem
{
    /// <summary>
    /// Description of a single file in a file system.
    /// </summary>
    public class FileEntry
    {
        /// <summary>
        /// The absolute path to the file.
        /// </summary>
        /// <remarks>May not necessarily be an absolute path on the disk, but can be rooted to the file system it originates from.</remarks>
        public UPath Path { get; }

        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        /// <remarks>May be the decompressed size of the file, depending on the underlying information available to the file description.</remarks>
        public long Size { get; }

        /// <summary>
        /// Creates a new <see cref="FileEntry"/>.
        /// </summary>
        /// <param name="path">The absolute path to the file.</param>
        /// <param name="size">The size of the file in bytes.</param>
        public FileEntry(UPath path, long size)
        {
            Path = path;
            Size = size;
        }
    }
}
