namespace Kontract.Models.Plugins.Entry
{
    /// <summary>
    /// The type of file a plugin can handle.
    /// </summary>
    public enum PluginType
    {
        /// <summary>
        /// Defines the type of file as an archive.
        /// </summary>
        Archive,

        /// <summary>
        /// Defines the type of file as images.
        /// </summary>
        Image,

        /// <summary>
        /// Defines the type of file as text.
        /// </summary>
        Text,

        /// <summary>
        /// Defines the type of file as font.
        /// </summary>
        Font,

        /// <summary>
        /// Defines the type of file as raw hex data.
        /// May only be used in internal code.
        /// </summary>
        Raw = int.MaxValue
    }
}
