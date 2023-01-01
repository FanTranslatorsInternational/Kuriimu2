namespace Kontract.Models.Plugins.Entry
{
    /// <summary>
    /// Offers additional information to the plugin.
    /// </summary>
    public sealed class PluginMetadata
    {
        /// <summary>
        /// The name of the plugin or its supported format(s).
        /// Often its file magic.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The short form description of the plugin.
        /// "A Kuriimu plugin."
        /// </summary>
        public string ShortDescription { get; }

        /// <summary>
        /// The long form description of the plugin.
        /// "A Kuriimu plugin to support a certain file format with meta information."
        /// </summary>
        public string LongDescription { get; }

        /// <summary>
        /// The author/developer of this plugin.
        /// Any possible notation the author is known as can be used.
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// The website at which either the plugin or the author can be found and contacted.
        /// </summary>
        public string Website { get; }

        /// <summary>
        /// Creates a new instance of plugin metadata.
        /// </summary>
        /// <param name="name">Name of the plugin.</param>
        /// <param name="author">Author of the plugin.</param>
        /// <param name="shortDescription">Short term description.</param>
        /// <param name="longDescription">Long term description.</param>
        /// <param name="website">Website to find and contact the author.</param>
        public PluginMetadata(string name, string author, string shortDescription = "", string longDescription = "", string website = "")
        {
            ContractAssertions.IsNotNull(name, nameof(name));
            ContractAssertions.IsNotNull(author, nameof(author));

            Name = name;
            Author = author;

            ShortDescription = shortDescription;
            LongDescription = longDescription;
            Website = website;
        }
    }
}
