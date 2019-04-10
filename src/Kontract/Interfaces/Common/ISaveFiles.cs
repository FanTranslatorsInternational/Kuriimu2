namespace Kontract.Interfaces.Common
{
    /// <summary>
    /// This interface allows a plugin to save files.
    /// </summary>
    public interface ISaveFiles
    {
        /// <summary>
        /// Allows a plugin to save files.
        /// </summary>
        /// <param name="output">The file to be saved.</param>
        /// <param name="versionIndex">The version index that the user selected.</param>
        void Save(StreamInfo output, int versionIndex = 0);
    }
}
