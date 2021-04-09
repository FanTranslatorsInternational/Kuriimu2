using System;
using System.Collections.Generic;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kontract.Interfaces.Managers
{
    public class ManualSelectionEventArgs : EventArgs
    {
        public string Message { get; }
        public IReadOnlyList<IFilePlugin> FilePlugins { get; }

        public IFilePlugin Result { get; set; }

        public ManualSelectionEventArgs(string message, IReadOnlyList<IFilePlugin> filePlugins)
        {
            Message = message;
            FilePlugins = filePlugins;
        }
    }
}
