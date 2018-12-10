using System.IO;

namespace Kontract.Interfaces.Common
{
    /// <summary>
    /// This interface allows a plugin to save streams.
    /// </summary>
    public interface ISaveStreams
    {
        /// <summary>
        /// Allows a plugin to save streams.
        /// </summary>
        /// <param name="output">The stream to be saved.</param>
        /// <param name="versionIndex">The version index that the user selected.</param>
        void Save(Stream output, int versionIndex = 0);
    }
}
