using System.Collections.Generic;
using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace Kuriimu2.ImGui.Models.Forms.Formats
{
    class TranslatedTextEntryPage
    {
        public required TextEntryPage Page { get; init; }

        public required string Name { get; set; }

        public required IList<TranslatedTextEntry> Entries { get; init; }
    }
}
