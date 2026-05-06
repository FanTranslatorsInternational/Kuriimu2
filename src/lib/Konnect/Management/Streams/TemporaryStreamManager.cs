using Konnect.Contract.Management.Streams;
using Konnect.Streams;

namespace Konnect.Management.Streams;

/// <summary>
/// Creates temporary streams on disk.
/// </summary>
internal class TemporaryStreamManager(string temporaryDirectory, IStreamManager streamManager) : ITemporaryStreamManager
{
    /// <inheritdoc />
    public Stream CreateTemporaryStream()
    {
        EnsureTemporaryDirectory(temporaryDirectory);

        var fileName = GetTemporaryName();
        var file = File.Create(Path.Combine(temporaryDirectory, fileName));

        var temporaryStream = new TemporaryStream(file);
        streamManager.Register(temporaryStream);

        return temporaryStream;
    }

    /// <summary>
    /// Ensure that the given directory exists on disk.
    /// </summary>
    /// <param name="tempDirectory">The directory to ensure.</param>
    private static void EnsureTemporaryDirectory(string tempDirectory)
    {
        if (!Directory.Exists(tempDirectory))
            Directory.CreateDirectory(tempDirectory);
    }

    /// <summary>
    /// Retrieve a temporary name.
    /// </summary>
    /// <returns>The temporary name.</returns>
    private static string GetTemporaryName()
    {
        return Guid.NewGuid().ToString("D");
    }
}