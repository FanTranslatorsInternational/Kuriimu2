namespace Konnect.Contract.Management.Streams;

/// <summary>
/// Exposes methods to create temporary streams.
/// </summary>
public interface ITemporaryStreamManager
{
    /// <summary>
    /// Creates a temporary stream on the disk.
    /// </summary>
    /// <returns>The temporary stream.</returns>
    Stream CreateTemporaryStream();
}