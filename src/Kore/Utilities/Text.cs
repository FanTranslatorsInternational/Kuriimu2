using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;
using Kore.Files;
using Kore.Files.Models;

namespace Kore.Utilities
{
    public static class Text
    {
        /// <summary>
        /// Exports an open file to a given adapter format.
        /// </summary>
        /// <param name="sourceAdapter">The adapter to be exported from.</param>
        /// <param name="exportAdapter">The adapter to be exported to.</param>
        /// <param name="outputFileName">The target filename to save to.</param>
        public static void ExportFile(ITextAdapter sourceAdapter, ITextAdapter exportAdapter, string outputFileName)
        {
            var newAdapter = (ITextAdapter)Activator.CreateInstance(exportAdapter.GetType());

            if (newAdapter is ICreateFiles creator && newAdapter is IAddEntries add)
            {
                // Create 
                creator.Create();

                // Create the new entries
                foreach (var entry in sourceAdapter.Entries)
                {
                    var newEntry = add.NewEntry();
                    newEntry.Name = entry.Name;
                    newEntry.EditedText = entry.EditedText;
                    newEntry.OriginalText = entry.OriginalText.Length == 0 ? entry.EditedText : entry.OriginalText;
                    newEntry.MaxLength = entry.MaxLength;
                    add.AddEntry(newEntry);
                }

                // Save the file
                // TODO: Inject file system
                (newAdapter as ISaveFiles)?.Save(new StreamInfo(File.Create(outputFileName), outputFileName), null);
            }
        }

        /// <summary>
        /// Imports the text from any supported text file into the adapter provided.
        /// </summary>
        /// <param name="fileManager">Instance of fileManager that will be used to load the input file.</param>
        /// <param name="adapter">The adapter that will be imported into.</param>
        /// <param name="inputFileName">The input file to be imported from.</param>
        public static bool ImportFile(this FileManager fileManager, ITextAdapter adapter, string inputFileName)
        {
            var result = false;

            //TODO
            var kfi = fileManager.LoadFile(new KoreLoadInfo(File.Open(inputFileName, FileMode.Open), inputFileName) { TrackFile = false });
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
