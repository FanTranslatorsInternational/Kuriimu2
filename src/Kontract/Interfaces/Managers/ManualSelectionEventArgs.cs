using System;
using System.Collections.Generic;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kontract.Interfaces.Managers
{
    public class ManualSelectionEventArgs : EventArgs
    {
        public IReadOnlyList<IFilePlugin> FilePlugins { get; }

        public IFilePlugin Result { get; set; }

        public ManualSelectionEventArgs(IReadOnlyList<IFilePlugin> filePlugins)
        {
            FilePlugins = filePlugins;
        }
    }
}
