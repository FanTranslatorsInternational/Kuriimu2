using Kontract.Interfaces;
using System;
using System.ComponentModel.Composition;

namespace Kontract.Attributes
{
    /// <inheritdoc />
    /// <summary>
    /// This attribute is used to define general information about a plugin.
    /// </summary>
    public class PluginInfoAttribute : Attribute, IPluginMetadata
    {
        // TODO: Determine how to handle plugin selection when two plugins have the same ID.
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
        /// The plugin website.
        /// "https://github.com/FanTranslatorsInternational/Kuriimu2"
        /// </summary>
        public string WebSite { get; }

        /// <summary>
        /// This is the long form description of the plugin.
        /// "This is the KUP text adapter for Kuriimu."
        /// </summary>
        public string About { get; }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new PluginInfoAttribute with the provided values.
        /// </summary>
        /// <param name="id">GUID</param>
        /// <param name="name">The plugin name.</param>
        /// <param name="shortName">The plugin short name.</param>
        /// <param name="author">The plugin author(s).</param>
        /// <param name="webSite">The plugin website.</param>
        /// <param name="about">The plugin description.</param>
        public PluginInfoAttribute(string id, string name = "", string shortName = "", string author = "", string webSite = "", string about = "")
        {
            ID = id;
            Name = name;
            ShortName = shortName;
            Author = author;
            WebSite = webSite;
            About = about;
        }
    }
}
