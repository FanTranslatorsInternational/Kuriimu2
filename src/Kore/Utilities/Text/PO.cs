using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kontract.Interfaces.Plugins.State.Text;
using Kontract.Models.Text;
using Kore.Utilities.Models;
using MoreLinq;

namespace Kore.Utilities.Text
{
    class PO
    {
        public IList<TextEntry> TextEntries { get; }

        public PO(IList<TextEntry> entries)
        {
            TextEntries = entries;
        }

        public void Save(Stream output)
        {
            using (var writer = new StreamWriter(output, Encoding.UTF8))
            {
                foreach (PoEntry entry in TextEntries)
                {
                    if (entry.Comments?.Any() ?? false)
                        entry.Comments.ForEach(x => writer.WriteLine($"# {x}"));
                    if (entry.ExtractedComments?.Any() ?? false)
                        entry.ExtractedComments.ForEach(x => writer.WriteLine($"#. {x}"));
                    if (entry.SourceReference?.Any() ?? false)
                        entry.SourceReference.ForEach(x => writer.WriteLine($"#: {x}"));
                    if (entry.Flags?.Any() ?? false)
                        entry.Flags.ForEach(x => writer.WriteLine($"#, {x}"));

                    if (!string.IsNullOrWhiteSpace(entry.Context))
                        writer.WriteLine($"msgctxt {entry.Context}");
                    if (!string.IsNullOrWhiteSpace(entry.OriginalText))
                        writer.WriteLine($"msgid {entry.OriginalText}");
                    if (!string.IsNullOrWhiteSpace(entry.EditedText))
                        writer.WriteLine($"msgstr {entry.EditedText}");

                    writer.WriteLine();
                    writer.WriteLine();
                }
            }
        }

        /// <summary>
        /// Load a PO file from a stream
        /// </summary>
        /// <param name="input">The input stream to parse.</param>
        /// <returns>The parsed PO file.</returns>
        public static PO FromFile(Stream input)
        {
            var result = new List<PoEntry>();

            var previousCategory = PoLineCategory.Blank;
            var currentPoEntry = new PoEntry();
            foreach (var line in EnumerateLines(new StreamReader(input)))
            {
                if (!CheckLine(line, previousCategory))
                    continue;

                if (previousCategory == PoLineCategory.MessageString && line.category != PoLineCategory.MessageString)
                {
                    result.Add(currentPoEntry);
                    currentPoEntry = new PoEntry();
                }

                switch (line.category)
                {
                    case PoLineCategory.ExtractedComment:
                        currentPoEntry.ExtractedComments.Add(line.content);
                        break;

                    case PoLineCategory.NormalComment:
                        currentPoEntry.Comments.Add(line.content);
                        break;

                    case PoLineCategory.SourceReference:
                        currentPoEntry.SourceReference.Add(line.content);
                        break;

                    case PoLineCategory.Flags:
                        currentPoEntry.Flags.Add(line.content);
                        break;

                    case PoLineCategory.MessageContext:
                        currentPoEntry.Context = line.content;
                        break;

                    case PoLineCategory.MessageId:
                        currentPoEntry.OriginalText = line.content;
                        break;

                    case PoLineCategory.MessageString:
                        currentPoEntry.EditedText = line.content;
                        break;

                    case PoLineCategory.String:
                        if (previousCategory == PoLineCategory.MessageContext)
                            currentPoEntry.Context += line.content;
                        else if (previousCategory == PoLineCategory.MessageId)
                            currentPoEntry.OriginalText += line.content;
                        else if (previousCategory == PoLineCategory.MessageString)
                            currentPoEntry.EditedText += line.content;
                        continue;

                    case PoLineCategory.Blank:
                        break;
                }

                previousCategory = line.category;
            }

            if (!result.Contains(currentPoEntry))
                result.Add(currentPoEntry);

            return new PO(result.Cast<TextEntry>().ToArray());
        }

        private static IEnumerable<(int lineNr, PoLineCategory category, string content)> EnumerateLines(StreamReader reader)
        {
            var lineNr = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNr++;

                if (string.IsNullOrWhiteSpace(line))
                    yield return (lineNr, PoLineCategory.Blank, null);
                else
                {
                    if (line.StartsWith("\""))
                        yield return (lineNr, PoLineCategory.String, line.Substring(1, line.Length - 3));
                    if (line.StartsWith("# "))
                        yield return (lineNr, PoLineCategory.NormalComment, line.Substring(2));
                    else if (line.StartsWith("#. "))
                        yield return (lineNr, PoLineCategory.ExtractedComment, line.Substring(3));
                    else if (line.StartsWith("#, "))
                        yield return (lineNr, PoLineCategory.Flags, line.Substring(3));
                    else if (line.StartsWith("#: "))
                        yield return (lineNr, PoLineCategory.SourceReference, line.Substring(3));
                    else if (line.StartsWith("msgctxt "))
                        yield return (lineNr, PoLineCategory.MessageContext, line.Substring(9, line.Length - 11));
                    else if (line.StartsWith("msgid "))
                        yield return (lineNr, PoLineCategory.MessageId, line.Substring(7, line.Length - 9));
                    else if (line.StartsWith("msgstr "))
                        yield return (lineNr, PoLineCategory.MessageString, line.Substring(8, line.Length - 10));
                }

                throw new InvalidOperationException($"Invalid syntax at line '{lineNr}'.");
            }
        }

        private static bool CheckLine((int lineNr, PoLineCategory category, string content) line, PoLineCategory previousCategory)
        {
            switch (line.category)
            {
                case PoLineCategory.ExtractedComment:
                case PoLineCategory.NormalComment:
                case PoLineCategory.SourceReference:
                case PoLineCategory.Flags:
                    if (previousCategory == PoLineCategory.MessageContext ||
                        previousCategory == PoLineCategory.MessageId ||
                        previousCategory == PoLineCategory.MessageString)
                        return false;
                    break;

                case PoLineCategory.MessageContext:
                    if (previousCategory == PoLineCategory.MessageId ||
                       previousCategory == PoLineCategory.MessageString)
                        throw new InvalidOperationException($"'msgctxt' has to come before 'msgid' and 'msgstr' at line {line.lineNr}.");
                    if (previousCategory == PoLineCategory.MessageContext)
                        throw new InvalidOperationException($"Multiple 'msgctxt' are not allowed at line {line.lineNr}.");
                    break;

                case PoLineCategory.MessageId:
                    if (previousCategory == PoLineCategory.MessageString)
                        throw new InvalidOperationException($"'msgid' has to come before 'msgstr' at line {line.lineNr}.");
                    if (previousCategory == PoLineCategory.MessageId)
                        throw new InvalidOperationException($"Multiple 'msgid' are not allowed at line {line.lineNr}.");
                    break;

                case PoLineCategory.MessageString:
                    if (previousCategory == PoLineCategory.MessageContext ||
                        previousCategory == PoLineCategory.Blank)
                        throw new InvalidOperationException($"'msgid' has to come after a 'msgid' at line {line.lineNr}.");
                    if (previousCategory == PoLineCategory.MessageString)
                        throw new InvalidOperationException($"Multiple 'msgstr' are not allowed at line {line.lineNr}.");
                    break;

                case PoLineCategory.String:
                    if (previousCategory == PoLineCategory.ExtractedComment ||
                        previousCategory == PoLineCategory.NormalComment ||
                        previousCategory == PoLineCategory.Flags ||
                        previousCategory == PoLineCategory.SourceReference ||
                        previousCategory == PoLineCategory.Blank)
                        throw new InvalidOperationException($"Splitted strings are only allowed for 'msgctxt', 'msgid', or 'msgstr' at line {line.lineNr}");
                    break;
            }

            return true;
        }
    }
}
