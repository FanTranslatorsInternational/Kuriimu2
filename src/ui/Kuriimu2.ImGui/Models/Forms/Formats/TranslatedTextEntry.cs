using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace Kuriimu2.ImGui.Models.Forms.Formats
{
    class TranslatedTextEntry
    {
        public TranslatedTextEntryPage? Page { get; init; }

        public required TextEntry Entry { get; init; }

        public required byte[] OriginalTextData { get; set; }
    }
}
