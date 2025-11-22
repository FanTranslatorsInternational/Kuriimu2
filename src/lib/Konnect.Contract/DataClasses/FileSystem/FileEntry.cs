namespace Konnect.Contract.DataClasses.FileSystem;

/// <summary>
/// Description of a single file in a file system.
/// </summary>
public class FileEntry
{
    /// <summary>
    /// The absolute path to the file.
    /// </summary>
    /// <remarks>May not necessarily be an absolute path on the disk, but can be rooted to the file system it originates from.</remarks>
    public required UPath Path { get; init; }

    /// <summary>
    /// The size of the file in bytes.
    /// </summary>
    /// <remarks>May be the decompressed size of the file, depending on the underlying information available to the file description.</remarks>
    public required long Size { get; init; }
}