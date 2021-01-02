using System;
using Kontract.Interfaces.Plugins.State;

namespace Kuriimu2.EtoForms.Exceptions
{
    class UnknownPluginStateException : Exception
    {
        private const string UnknownPluginState = "Unknown plugin state type '{0}'.";

        public UnknownPluginStateException(IPluginState pluginState) : base(GetMessage(pluginState))
        {
        }

        private static string GetMessage(IPluginState pluginState)
        {
            return string.Format(UnknownPluginState, pluginState.GetType().Name);
        }
    }
}
