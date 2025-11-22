namespace Konnect.Contract.DataClasses.FileSystem;

/// <summary>
/// A class combining a stream and a name to represent an in-memory file.
/// </summary>
public class StreamFile
{
    /// <summary>
    /// The stream containing the file data.
    /// </summary>
    public required Stream Stream { get; init; }

    /// <summary>
    /// The path of the file to represent.
    /// </summary>
    public required UPath Path { get; init; }
}