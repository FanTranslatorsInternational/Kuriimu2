using System;
using System.Linq;

namespace Kontract.Exceptions
{
    /// <summary>
    /// An exception thrown by <see cref="PluginLoader"/> if loaded plugins create inconsistencies.
    /// </summary>
    public class PluginInconsistencyException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="PluginInconsistencyException"/>.
        /// </summary>
        /// <param name="pluginNames">A list of all plugins that create inconsistencies.</param>
        public PluginInconsistencyException(params string[] pluginNames) : base(CreateInconsistentList(pluginNames))
        {

        }

        /// <summary>
        /// Serializes a message containing all inconsistent plugins.
        /// </summary>
        /// <param name="pluginNames">Names of the plugins.</param>
        /// <returns>A serialized message.</returns>
        private static string CreateInconsistentList(string[] pluginNames)
        {
            if (pluginNames == null || !pluginNames.Any())
                return "Some plugins create inconsistencies.";

            var result = $"The following plugins are inconsistent: {Environment.NewLine}{Environment.NewLine}";
            return result + string.Join(Environment.NewLine, pluginNames);
        }
    }
}
