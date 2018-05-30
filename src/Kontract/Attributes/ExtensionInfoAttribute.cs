using System;

namespace Kontract.Attributes
{
    /// <inheritdoc />
    /// <summary>
    /// This attribute is used to define the extension(s) that a plugin works with.
    /// </summary>
    public class PluginExtensionInfoAttribute : Attribute
    {
        /// <summary>
        /// This is a semi-colon delimited list of file extensions supported by the plugin.
        /// "*.ext1;*.ext2"
        /// </summary>
        public string Extension { get; }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new ExtensionInfoAttribute with the provided extension(s).
        /// </summary>
        /// <param name="extension"></param>
        public PluginExtensionInfoAttribute(string extension)
        {
            Extension = extension;
        }
    }
}
