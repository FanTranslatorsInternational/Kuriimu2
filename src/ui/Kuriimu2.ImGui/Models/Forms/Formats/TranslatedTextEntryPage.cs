using System.Collections.Generic;
using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace Kuriimu2.ImGui.Models.Forms.Formats
{
    class TranslatedTextEntryPage
    {
        public TextEntryPage Page { get; init; }

        public IList<TranslatedTextEntry> Entries { get; init; }
    }
}
