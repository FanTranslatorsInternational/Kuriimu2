namespace Kontract.Attribute
{
    /// <summary>
    /// This attribute is used to define general information about a plugin.
    /// </summary>
    public class PluginInfo : System.Attribute
    {
        // TODO: Determine how to handle plugin selection while using MEF and when two plugins have the same ID.
        /// <summary>
        /// This is the unique GUID of the plugin.
        /// "82FAE4B0-9734-4802-A3C6-1594EE8C6EEA"
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// This is the short form description of the plugin.
        /// "Kuriimu Text Archive"
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// This is the short name of the plugin.
        /// Often it's the file magic.
        /// "KUP"
        /// </summary>
        public string ShortName { get; }

        /// <summary>
        /// This is the list of author names.
        /// "IcySon55, NeoBeo, onepiecefreak"
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// This is the long form description of the plugin.
        /// "This is the KUP text adapter for Kuriimu."
        /// </summary>
        public string About { get; }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new PluginInfo with the provided values.
        /// </summary>
        /// <param name="id">GUID</param>
        /// <param name="name">The plugin name.</param>
        /// <param name="shortName">The plugin short name.</param>
        /// <param name="author">The plugin author(s).</param>
        /// <param name="about">The plugin description.</param>
        public PluginInfo(string id, string name = "", string shortName = "", string author = "", string about = "")
        {
            ID = id;
            Name = name;
            ShortName = shortName;
            Author = author;
            About = about;
        }
    }
}
