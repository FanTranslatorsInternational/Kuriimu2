using System.Collections.Generic;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kontract.Interfaces.Plugins.State.Text
{
    /// <summary>
    /// This is the text adapter interface for creating text format plugins.
    /// </summary>
    public interface ITextAdapter : IFilePlugin
    {
        /// <summary>
        /// The list of entries provided by the text adapter to the UI.
        /// </summary>
        IEnumerable<TextEntry> Entries { get; }

        /// <summary>
        /// A regular expression that new names must match.
        /// Use @".*" to accept any character.
        /// </summary>
        string NameFilter { get; }

        /// <summary>
        /// The maximum length that entry names can store.
        /// Longer names will be truncated.
        /// 0 for unlimited.
        /// </summary>
        int NameMaxLength { get; }

        // TODO: Determine if this member is necessary at all.
        /// <summary>
        /// The line endings used by the file format.
        /// </summary>
        string LineEndings { get; set; }
    }
}
