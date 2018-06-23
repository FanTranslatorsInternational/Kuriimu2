using System;

namespace Kontract.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// This interface allows a plugin to load files.
    /// </summary>
    public interface ILoadFiles : IDisposable
    {
        /// <summary>
        /// Loads the given file and populates the entry list.
        /// </summary>
        /// <param name="filename">The file to be loaded.</param>
        void Load(string filename);
    }
}
