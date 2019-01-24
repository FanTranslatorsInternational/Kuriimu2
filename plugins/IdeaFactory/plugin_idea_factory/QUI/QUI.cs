using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace plugin_idea_factory.QUI
{
    /// <summary>
    /// The base QUI format class.
    /// </summary>
    public class QUI
    {
        /// <summary>
        /// Stores all of the entries.
        /// </summary>
        private List<QuiTextEntry> _allEntries;

        /// <summary>
        /// Stores the editable text entries.
        /// </summary>
        public List<QuiTextEntry> Entries
        {
            get => _allEntries.Where(e => e.Type == QuiEntryType.Message).ToList();
        }

        /// <summary>
        /// Construct a new QUI instance from a parsed stream.
        /// </summary>
        /// <param name="input">A stream of the data.</param>
        public QUI(Stream input)
        {
            using (var sr = new StreamReader(input, Encoding.UTF8))
            {
                _allEntries = new List<QuiTextEntry>();
                var index = 1;

                while (!sr.EndOfStream)
                {
                    var entry = new QuiTextEntry();
                    var line = sr.ReadLine();

                    entry.Text = line;

                    IdentifyLine(entry);
                    _allEntries.Add(entry);

                    if (entry.Type == QuiEntryType.Function)
                    {
                        if (Regex.IsMatch(line, @"^\s*\(message"))
                        {
                            var matchText = "^\\s*\"(.+)\"";
                            var matchEnd = "^\\s*\"(.+)\"\\)";
                            var matchComment = "^\\s*\".+\"\\)(;.+)$";

                            entry.Type = QuiEntryType.Message;
                            entry.Notes = line.TrimStart('\t');

                            line = sr.ReadLine();
                            var text = string.Empty;
                            while (line.Length > 0 && !sr.EndOfStream)
                            {
                                if (Regex.IsMatch(line, matchEnd))
                                {
                                    text += Regex.Match(line, matchEnd).Groups[1].Value.Replace("\\n", "\r\n");
                                    entry.Comment = Regex.Match(line, matchComment).Groups[1].Value;
                                    break;
                                }
                                else if (Regex.IsMatch(line, matchText))
                                    text += Regex.Match(line, matchText).Groups[1].Value.Replace("\\n", "\r\n");
                                else
                                {
                                    var nextEntry = new QuiTextEntry { Text = line };
                                    IdentifyLine(nextEntry);
                                    _allEntries.Add(nextEntry);
                                }
                                line = sr.ReadLine();
                            }
                            entry.Name = index.ToString();
                            entry.EditedText = text;
                            entry.OriginalText = text;
                            index++;

                            if (line.Length == 0)
                            {
                                var nextEntry = new QuiTextEntry { Text = line };
                                IdentifyLine(nextEntry);
                                _allEntries.Add(nextEntry);
                            }
                        }
                    }
                }

                var final = new QuiTextEntry { Text = string.Empty };
                IdentifyLine(final);
                _allEntries.Add(final);
            }
        }

        /// <summary>
        /// Idnetifies the entry type.
        /// </summary>
        /// <param name="entry"></param>
        private void IdentifyLine(QuiTextEntry entry)
        {
            if (entry.Text.Length == 0)
                entry.Type = QuiEntryType.EmptyLine;
            else if (Regex.IsMatch(entry.Text, @"^\s*\("))
                entry.Type = QuiEntryType.Function;
            else if (Regex.IsMatch(entry.Text, @"^\s*\)"))
                entry.Type = QuiEntryType.EndFunction;
            else if (Regex.IsMatch(entry.Text, @"^\s*\;"))
                entry.Type = QuiEntryType.Comment;
        }

        /// <summary>
        /// Saves out the QUI format.
        /// </summary>
        /// <param name="output">Stream to save to.</param>
        public void Save(Stream output)
        {
            using (var sw = new StreamWriter(output, Encoding.UTF8))
            {
                foreach (var entry in _allEntries)
                {
                    switch (entry.Type)
                    {
                        case QuiEntryType.Function:
                        case QuiEntryType.EndFunction:
                        case QuiEntryType.Comment:
                        case QuiEntryType.EmptyLine:
                            if (entry != _allEntries.Last())
                                sw.WriteLine(entry.Text);
                            break;
                        case QuiEntryType.Message:
                            {
                                sw.WriteLine(entry.Text);
                                var tabs = Regex.Match(entry.Text, @"(\s+)").Groups[1].Value;
                                var lines = entry.EditedText.Split('\r');

                                for (int i = 0; i < lines.Length; i++)
                                {
                                    string line = lines[i];
                                    sw.Write(tabs + "\t\t\"" + line.Replace("\n", string.Empty));
                                    if (i != lines.Length - 1)
                                        sw.WriteLine("\\n\"");
                                    else
                                        sw.WriteLine("\")" + entry.Comment);
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}
