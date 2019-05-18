using System;
using System.Collections.Generic;
using Kontract.Interfaces.Common;

namespace Kore.Files.Models
{
    /// <summary>
    /// Allows the UI to display a list of blind plugins and to return one selected by the user.
    /// </summary>
    public class IdentificationFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Adapters with no identification method.
        /// </summary>
        public IList<ILoadFiles> BlindAdapters { get; }

        /// <summary>
        /// Name of the file to load.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The adapter selected from the blind adapter.
        /// </summary>
        public ILoadFiles SelectedAdapter { get; set; }

        public IdentificationFailedEventArgs(string fileName, IList<ILoadFiles> blindAdapters)
        {
            FileName = fileName;
            BlindAdapters = blindAdapters;
        }
    }
}
