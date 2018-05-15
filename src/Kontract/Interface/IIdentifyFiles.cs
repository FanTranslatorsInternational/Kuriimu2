namespace Kontract.Interface
{
    /// <summary>
    /// This interface allows a plugin to participate in automatic file identification.
    /// Plugins with this interface take priority over those without.
    /// </summary>
    public interface IIdentifyFiles
    {
        /// <summary>
        /// Determines if the given file is supported by the plugin.
        /// </summary>
        /// <param name="filename">The file to be identified.</param>
        /// <returns>True or False</returns>
        bool Identify(string filename);
    }
}
