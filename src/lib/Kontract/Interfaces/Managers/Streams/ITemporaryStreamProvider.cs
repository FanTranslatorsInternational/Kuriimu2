using System.IO;

namespace Kontract.Interfaces.Managers.Streams
{
    /// <summary>
    /// Exposes methods to create temporary streams.
    /// </summary>
    public interface ITemporaryStreamProvider
    {
        /// <summary>
        /// Creates a temporary stream on the disk.
        /// </summary>
        /// <returns>The temporary stream.</returns>
        Stream CreateTemporaryStream();
    }
}
