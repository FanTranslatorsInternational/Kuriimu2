using System;
using System.Runtime.Serialization;
using Kontract.Interfaces.Plugins.Loaders;

namespace Kore.Exceptions
{
    /// <summary>
    /// An exception thrown by <see cref="IPluginLoader"/>s, if loaded plugins create inconsistencies.
    /// </summary>
    public class PluginInconsistencyException : Exception
    {
        public string[] PluginNames { get; }

        /// <summary>
        /// Creates a new instance of <see cref="PluginInconsistencyException"/>.
        /// </summary>
        /// <param name="pluginNames">A list of all plugins that create inconsistencies.</param>
        public PluginInconsistencyException(params string[] pluginNames) : base(CreateInconsistencyList(pluginNames))
        {
            PluginNames = pluginNames;
        }

        /// <summary>
        /// Serializes a message containing all inconsistent plugins.
        /// </summary>
        /// <param name="pluginNames">Names of the plugins.</param>
        /// <returns>A serialized message.</returns>
        private static string CreateInconsistencyList(string[] pluginNames)
        {
            if (pluginNames == null || pluginNames.Length <= 0)
                return "Some plugins create inconsistencies.";

            return $"The following plugins are inconsistent: {string.Join(",", pluginNames)}";
        }

        /// <inheritdoc cref="ISerializable.GetObjectData(SerializationInfo,StreamingContext)"/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(PluginNames), PluginNames);
            base.GetObjectData(info, context);
        }
    }
}
