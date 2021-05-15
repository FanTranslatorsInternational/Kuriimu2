using System;
using System.Collections.Generic;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kontract.Interfaces.Managers
{
    public class ManualSelectionEventArgs : EventArgs
    {
        public string Message { get; }
        public IReadOnlyList<IFilePlugin> FilePlugins { get; }
        public string FilterNote { get; }
        public IReadOnlyList<IFilePlugin> FilteredPlugins { get; }

        public IFilePlugin Result { get; set; }

        public ManualSelectionEventArgs(string message, IReadOnlyList<IFilePlugin> filePlugins, string filterNote, IReadOnlyList<IFilePlugin> filteredPlugins)
        {
            Message = message;
            FilterNote = filterNote;
            FilteredPlugins = filteredPlugins;
            FilePlugins = filePlugins;
        }
    }
}
