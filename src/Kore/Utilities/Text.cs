using System.Linq;
using Kontract.Interfaces;
using Kore.SamplePlugins;

namespace Kore.Utilities
{
    public static class Text
    {
        /// <summary>
        /// Exports an open file to a KUP file.
        /// </summary>
        /// <param name="adapter">The adapter to be exported from.</param>
        /// <param name="outputFileName">The target filename to save to.</param>
        public static void ExportKup(ITextAdapter adapter, string outputFileName)
        {
            var kup = new KupAdapter();
            kup.Create();
            kup.Entries = adapter.Entries.Select(entry => new TextEntry
            {
                Name = entry.Name,
                EditedText = entry.EditedText,
                OriginalText = entry.OriginalText.Length == 0 ? entry.EditedText : entry.OriginalText,
                MaxLength = entry.MaxLength
            });
            kup.Save(outputFileName);
        }

        /// <summary>
        /// Imports the text from any supported text file into the adapter provided.
        /// </summary>
        /// <param name="kore">An instance of kore that will be used to load the input file.</param>
        /// <param name="adapter">The adapter that will be imported into.</param>
        /// <param name="inputFileName">The input file to be imported from.</param>
        public static bool ImportFile(this Kore kore, ITextAdapter adapter, string inputFileName)
        {
            var result = false;

            var kfi = kore.LoadFile(inputFileName, true);
            if (!(kfi.Adapter is ITextAdapter inAdapter)) return false;

            foreach (var inEntry in inAdapter.Entries)
            {
                var entry = adapter.Entries.FirstOrDefault(e => e.Name == inEntry.Name);
                if (entry == null || entry.EditedText == inEntry.EditedText) continue;
                entry.EditedText = inEntry.EditedText;
                result = true;
            }

            return result;
        }
    }
}
