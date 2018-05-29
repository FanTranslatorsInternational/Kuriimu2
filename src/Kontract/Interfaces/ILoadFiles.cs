namespace Kontract.Interfaces
{
    /// <summary>
    /// This interface allows a plugin to load files.
    /// </summary>
    public interface ILoadFiles
    {
        /// <summary>
        /// Loads the given file and populates the entry list.
        /// </summary>
        /// <param name="filename">The file to be loaded.</param>
        void Load(string filename);
    }
}
