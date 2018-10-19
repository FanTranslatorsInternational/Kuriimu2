using System;
using System.Collections.Generic;
using System.IO;

namespace Kontract.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// This interface allows a plugin to load streams.
    /// </summary>
    public interface ILoadStreams : IDisposable
    {
        ///// <summary>
        ///// Returns a list of filenames to be loaded.
        ///// </summary>
        ///// <param name="input">The initial stream being loaded.</param>
        ///// <param name="filename">The initial filename being loaded.</param>
        ///// <returns></returns>
        //IList<string> GetFilenames(Stream input, string filename);

        /// <summary>
        /// Loads the given stream(s).
        /// </summary>
        /// <param name="inputs">The stream(s) to be loaded.</param>
        void Load(params Stream[] inputs);
    }
}
