using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Text
{
    /// <summary>
    /// This interface allows the text adapter to rename entries through the UI making use of the NameList.
    /// </summary>
    public interface IRenameEntries
    {
        /// <summary>
        /// Renames an entry and allows the plugin to perform any required renaming steps.
        /// </summary>
        /// <param name="entry">The entry being renamed.</param>
        /// <param name="name">The new name to be assigned.</param>
        /// <returns>True if the entry was renamed, False otherwise.</returns>
        bool RenameEntry(TextEntry entry, string name);
    }
}
