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
        private const string MatchMessage = "^\\s*\\(message (.+) (.+?)(?: |$)";
        private const string MatchSingleMessage = "^\\s*\\(message (.+) (.+?) (.+?)\\)$";

        /// <summary>
        /// Stores all of the entries.
        /// </summary>
        private readonly List<QuiTextEntry> _allEntries;

        /// <summary>
        /// Stores the editable text entries.
        /// </summary>
        public List<QuiTextEntry> Entries
        {
            get => _allEntries.Where(e => e.Type == QuiEntryType.Name || e.Type == QuiEntryType.Message).ToList();
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

                // Content
                const string matchText = "^\\s*\"(.+)\"";
                const string matchEndText = "^\\s*\"(.+)\"\\)";
                const string matchComment = "^\\s*\".+\"\\)(;.+)$";

                do
                {
                    var line = sr.ReadLine() ?? string.Empty;
                    var type = IdentifyLine(line);

                    switch (type)
                    {
                        case QuiEntryType.EmptyLine:
                        case QuiEntryType.Comment:
                        case QuiEntryType.EndFunction:
                            _allEntries.Add(new QuiTextEntry { Content = line, Type = type });
                            break;
                        case QuiEntryType.Function:
                            // Now the fun begins
                            if (Regex.IsMatch(line, MatchMessage))
                            {
                                // Name
                                var name = Regex.Match(line, MatchMessage).Groups[1].Value;
                                if (name != "nil")
                                    _allEntries.Add(new QuiTextEntry
                                    {
                                        Content = line,
                                        Type = QuiEntryType.Name,
                                        Name = $"Name{index.ToString()}",
                                        EditedText = name.Trim('"'),
                                        OriginalText = name.Trim('"')
                                    });

                                // Single line message
                                if (Regex.IsMatch(line, MatchSingleMessage))
                                {
                                    var singleMessage = Regex.Match(line, MatchSingleMessage).Groups[3].Value;
                                    _allEntries.Add(new QuiTextEntry
                                    {
                                        Content = line,
                                        Type = QuiEntryType.Message,
                                        Name = $"Message{index.ToString()}",
                                        EditedText = singleMessage.Trim('"'),
                                        OriginalText = singleMessage.Trim('"')
                                    });
                                    index++;
                                }
                                else
                                {
                                    // Multi-line message
                                    var lines = new List<string>();

                                    while (!sr.EndOfStream)
                                    {
                                        var inline = sr.ReadLine() ?? string.Empty;
                                        lines.Add(inline);

                                        if (Regex.IsMatch(inline, matchEndText))
                                            break;
                                    }

                                    var text = string.Empty;
                                    foreach (var inline in lines)
                                    {
                                        // Is this line a comment?
                                        if (IdentifyLine(inline) == QuiEntryType.Comment)
                                        {
                                            _allEntries.Add(new QuiTextEntry
                                            {
                                                Content = inline,
                                                Type = QuiEntryType.Comment
                                            });
                                            continue;
                                        }

                                        text += Regex.Match(inline, matchText).Groups[1].Value.Replace("\\n", "\r\n");
                                    }
                                    var comment = Regex.Match(lines.Last(), matchComment).Groups[1].Value;

                                    _allEntries.Add(new QuiTextEntry
                                    {
                                        Content = line,
                                        Comment = comment,
                                        Type = QuiEntryType.Message,
                                        Name = $"Message{index.ToString()}",
                                        EditedText = text,
                                        OriginalText = text
                                    });
                                    index++;
                                }
                            }
                            else
                                _allEntries.Add(new QuiTextEntry { Content = line, Type = type });

                            break;
                    }
                } while (!sr.EndOfStream);

                //var final = new QuiTextEntry { Content = string.Empty };
                //IdentifyLine(final);
                //_allEntries.Add(final);
            }
        }

        /// <summary>
        /// Identifies the content type.
        /// </summary>
        /// <param name="entry"></param>
        private QuiEntryType IdentifyLine(string content)
        {
            if (content.Length == 0)
                return QuiEntryType.EmptyLine;
            if (Regex.IsMatch(content, @"^\s*\("))
                return QuiEntryType.Function;
            if (Regex.IsMatch(content, @"^\s*\)"))
                return QuiEntryType.EndFunction;
            if (Regex.IsMatch(content, @"^\s*\;"))
                return QuiEntryType.Comment;

            return QuiEntryType.EmptyLine;
        }

        /// <summary>
        /// Saves out the QUI format.
        /// </summary>
        /// <param name="output">Stream to save to.</param>
        public void Save(Stream output)
        {
            using (var sw = new StreamWriter(output, Encoding.UTF8))
            {
                var hasName = false;

                foreach (var entry in _allEntries)
                {
                    var tabs = Regex.Match(entry.Content, @"(\s+)").Groups[1].Value;

                    switch (entry.Type)
                    {
                        case QuiEntryType.Function:
                        case QuiEntryType.EndFunction:
                        case QuiEntryType.Comment:
                        case QuiEntryType.EmptyLine:
                            sw.WriteLine(entry.Content);
                            break;
                        case QuiEntryType.Name:
                            var name = entry.EditedText.StartsWith("(") && entry.EditedText.EndsWith(")") || entry.EditedText.StartsWith("'") ? entry.EditedText : $"\"{entry.EditedText}\"";
                            var other = Regex.Match(entry.Content, MatchMessage).Groups[2].Value;
                            var nameMessage = $"{tabs}(message {name} {other}";

                            if (Regex.IsMatch(entry.Content, MatchSingleMessage))
                                sw.Write(nameMessage);
                            else
                                sw.WriteLine(nameMessage);

                            hasName = true;
                            break;
                        case QuiEntryType.Message:
                            {
                                var lines = entry.EditedText.Split('\r');

                                if (!hasName)
                                {
                                    var match = Regex.Match(entry.Content, MatchMessage);
                                    var message = $"{tabs}(message {match.Groups[1].Value} {match.Groups[2].Value}";

                                    if (Regex.IsMatch(entry.Content, MatchSingleMessage))
                                        sw.Write(message);
                                    else
                                        sw.WriteLine(message);
                                }

                                if (lines.Length == 1)
                                {
                                    var inText = lines[0];
                                    var text = inText.StartsWith("(") && inText.EndsWith(")") && entry.Type == QuiEntryType.Name || inText.StartsWith("(str-append") ? inText : $"\"{inText}\"";

                                    if (Regex.IsMatch(entry.Content, MatchSingleMessage))
                                        sw.WriteLine(" " + text.Replace("\n", string.Empty) + ")" + entry.Comment);
                                    else
                                        sw.WriteLine(tabs + "\t\t" + text.Replace("\n", string.Empty) + ")" + entry.Comment);
                                }
                                else
                                {
                                    for (var i = 0; i < lines.Length; i++)
                                    {
                                        var line = lines[i];
                                        sw.Write(tabs + "\t\t\"" + line.Replace("\n", string.Empty));
                                        if (i != lines.Length - 1)
                                            sw.WriteLine("\\n\"");
                                        else
                                            sw.WriteLine("\")" + entry.Comment);
                                    }
                                }

                                hasName = false;
                            }
                            break;
                    }
                }
            }
        }
    }
}
