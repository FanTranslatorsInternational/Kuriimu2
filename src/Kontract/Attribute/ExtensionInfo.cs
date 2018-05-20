namespace Kontract.Attribute
{
    /// <summary>
    /// This attribute is used to define the extension(s) that a plugin works with.
    /// </summary>
    public class PluginExtensionInfo : System.Attribute
    {
        /// <summary>
        /// This is a semi-colon delimited list of file extensions supported by the plugin.
        /// "*.ext1;*.ext2"
        /// </summary>
        public string Extension { get; }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new ExtensionInfo with the provided extension(s).
        /// </summary>
        /// <param name="extension"></param>
        public PluginExtensionInfo(string extension)
        {
            Extension = extension;
        }
    }
}
