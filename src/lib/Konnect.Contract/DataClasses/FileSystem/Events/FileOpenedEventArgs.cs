namespace Konnect.Contract.DataClasses.FileSystem.Events;

/// <summary>
/// Represents a file opening.
/// </summary>
public class FileOpenedEventArgs : EventArgs
{
    /// <summary>
    /// Absolute path to the opened file.
    /// </summary>
    public required UPath OpenedPath { get; init; }
}