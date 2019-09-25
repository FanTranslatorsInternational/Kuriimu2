using System;
using Kontract.Interfaces.Common;

namespace Kontract.Attributes
{
    /// <summary>
    /// This attribute is used to define the extensions that a plugin works with.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginExtensionInfoAttribute : Attribute, IPluginMetadata
    {
        // TODO: Make this a list of extensions, not a concatinated string
        /// <summary>
        /// This is a semi-colon delimited list of file extensions supported by the plugin.
        /// "*.ext1;*.ext2"
        /// </summary>
        public string Extension { get; }

        // TODO: Make this a params input
        /// <summary>
        /// Initializes a new <see cref="PluginExtensionInfoAttribute"/> with the provided extensions.
        /// </summary>
        /// <param name="extension">Provided extensions for a plugin.</param>
        public PluginExtensionInfoAttribute(string extension)
        {
            Extension = extension;
        }
    }
}
