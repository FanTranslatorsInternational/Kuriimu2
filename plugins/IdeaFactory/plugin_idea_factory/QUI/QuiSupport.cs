using System.Collections.Generic;
using Kontract.Interfaces.Text;

namespace plugin_idea_factory.QUI
{
    /// <summary>
    /// The entry type.
    /// </summary>
    public enum QuiEntryType
    {
        EmptyLine,
        Function,
        EndFunction,
        Name,
        Message,
        Comment
    }

    /// <summary>
    /// A QUI Entry that stores the command and its type.
    /// </summary>
    public class QuiTextEntry : TextEntry
    {
        /// <summary>
        /// The command for this entry.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The command type of this QUI entry.
        /// </summary>
        public QuiEntryType Type { get; set; }

        /// <summary>
        /// Stores extra content parts that are not strings
        /// </summary>
        public List<string> Extras { get; } = new List<string>();

        /// <summary>
        /// Stores the comment at the end of the line.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Dump the content to string.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Content;
    }
}
