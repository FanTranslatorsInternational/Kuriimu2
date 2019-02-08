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
        private readonly List<QuiTextEntry> _allEntries = new List<QuiTextEntry>();

        /// <summary>
        /// Stores the editable text entries.
        /// </summary>
        public List<QuiTextEntry> Entries => _allEntries.Where(e => e.Type == QuiEntryType.Name || e.Type == QuiEntryType.Message).ToList();

        /// <summary>
        /// Construct a new QUI instance from a parsed stream.
        /// </summary>
        /// <param name="input">A stream of the data.</param>
        public QUI(Stream input)
        {
            using (var sr = new StreamReader(input, Encoding.UTF8))
            {
                var lines = new List<string>();

                while (!sr.EndOfStream)
                    lines.Add(sr.ReadLine());

                var index = 1;

                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var type = IdentifyLine(line);

                    switch (type)
                    {
                        case QuiEntryType.EmptyLine:
                        case QuiEntryType.Comment:
                        case QuiEntryType.EndFunction:
                            _allEntries.Add(new QuiTextEntry { Content = line, Type = type });
                            break;
                        case QuiEntryType.Function:
                            var isMessage = Regex.IsMatch(line, "^\\s*\\(message(?:-kw)?\\s+(.+)$");
                            var isKwMessage = Regex.IsMatch(line, "^\\s*\\(message-kw?\\s+(.+)$");

                            if (isMessage)
                            {
                                var lin = Regex.Match(line, "^\\s*\\(message(?:-kw)?\\s+(.+)$").Groups[1].Value;
                                var parts = new List<string>();
                                var content = line;
                                var messageContent = "";

                                var ended = false;
                                var functionCount = 1;
                                var subCount = 0;
                                var inString = false;
                                var position = 0;
                                var part = "";
                                var comment = "";

                                void NewPart(string str)
                                {
                                    parts.Add(str);
                                    part = "";
                                }

                                void NextLine()
                                {
                                    i++;
                                    position = 0;
                                    line = lines[i];
                                    type = IdentifyLine(line);
                                    messageContent += line + "\r\n";

                                    if (type == QuiEntryType.Comment || type == QuiEntryType.EmptyLine)
                                    {
                                        _allEntries.Add(new QuiTextEntry { Content = line, Type = type });
                                        if (i < lines.Count)
                                            NextLine();
                                    }

                                    lin = Regex.Match(line, "^\\s*(.+)$").Groups[1].Value;
                                }

                                do
                                {
                                    // Move to next line when the current one doesn't close the function
                                    if (position > lin.Length - 1 && functionCount > 0)
                                    {
                                        if (part.Trim() != string.Empty)
                                            NewPart(part);
                                        NextLine();
                                    }

                                    // Next character
                                    var chr = lin[position];

                                    switch (chr)
                                    {
                                        case '"': // Flip in and out of string status
                                            inString = !inString;
                                            part += chr;
                                            break;
                                        case '(': // Increase function and sub count
                                            if (!inString)
                                            {
                                                functionCount++;
                                                subCount++;
                                            }
                                            part += chr;
                                            break;
                                        case ')': // Decrease function and sub count
                                            if (!inString)
                                            {
                                                functionCount--;
                                                subCount--;
                                            }
                                            if (functionCount == 0)
                                            {
                                                NewPart(part);
                                                ended = true;
                                                break;
                                            }
                                            part += chr;
                                            break;
                                        case ';': // Gobble up the comment
                                            if (subCount == 0 && !inString)
                                            {
                                                while (position < lin.Length)
                                                {
                                                    comment += lin[position];
                                                    position++;
                                                }
                                            }
                                            break;
                                        case ' ': // End parameter if not in a string and sub count is 0
                                        case '\t':
                                            while ((chr == ' ' || chr == '\t') && position < lin.Length - 1) // Gobble whitespace
                                            {
                                                position++;
                                                chr = lin[position];
                                            }
                                            if (chr == ';') // Gobble up the comment after the whitespace
                                            {
                                                while (position < lin.Length)
                                                {
                                                    comment += lin[position];
                                                    position++;
                                                }
                                                break;
                                            }
                                            // If we've hit the end of the line with whitespace, break;
                                            if (position == lin.Length - 1)
                                                break;
                                            // Go back one character if it wasn't a comment
                                            position--;
                                            chr = lin[position];
                                            if (subCount == 0 && !inString && parts.Count < 2)
                                                NewPart(part);
                                            else
                                                part += chr;
                                            break;
                                        default:
                                            part += chr;
                                            break;
                                    }

                                    position++;
                                    if (!ended) continue;

                                    // Read possible comment
                                    while (position < lin.Length)
                                    {
                                        comment += lin[position];
                                        position++;
                                    }
                                    break;
                                } while (functionCount > 0);

                                // Add Name
                                _allEntries.Add(new QuiTextEntry
                                {
                                    Content = content,
                                    SecondParameter = parts[1], // Store the second part in Comment
                                    Type = QuiEntryType.Name,
                                    Name = $"Name{index.ToString()}",
                                    EditedText = parts[0],
                                    OriginalText = parts[0],
                                    IsKwMessage = isKwMessage
                                });

                                // Add Message
                                var message = new QuiTextEntry
                                {
                                    Content = messageContent,
                                    Comment = comment,
                                    Type = QuiEntryType.Message,
                                    Name = $"Message{index.ToString()}",
                                    IsLiteral = parts.Skip(2).Any(pa => pa.Contains("(") || pa.Contains(")") || Regex.IsMatch(pa, @" (r|g|b|n)+( |$)"))
                                };

                                var text = "";
                                foreach (var p in parts.Skip(2))
                                {
                                    if (p.EndsWith("scene-param"))
                                        message.Extras.Add("scene-param");

                                    var fp = p.Replace(" scene-param", string.Empty);

                                    if (message.IsLiteral)
                                        text += fp.TrimEnd(' ') + "\r\n";
                                    else
                                        text += fp.Replace("\\n", "\r\n").Trim('"');
                                }
                                message.EditedText = text.TrimStart(' ').TrimEnd('\r', '\n');
                                message.OriginalText = text.TrimStart(' ').TrimEnd('\r', '\n');

                                // Add the message
                                _allEntries.Add(message);

                                index++;
                            }
                            else // Other function
                                _allEntries.Add(new QuiTextEntry { Content = line, Type = type });

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Identifies the line type.
        /// </summary>
        /// <param name="line"></param>
        private static QuiEntryType IdentifyLine(string line)
        {
            if (line.Length == 0)
                return QuiEntryType.EmptyLine;
            if (Regex.IsMatch(line, @"^\s*\("))
                return QuiEntryType.Function;
            if (Regex.IsMatch(line, @"^\s*\)"))
                return QuiEntryType.EndFunction;
            if (Regex.IsMatch(line, @"^\s*\;"))
                return QuiEntryType.Comment;
            if (Regex.IsMatch(line, "^\\s*\".+\"\\)?"))
                return QuiEntryType.Message;

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
                var tabs = string.Empty;

                foreach (var entry in _allEntries)
                {
                    switch (entry.Type)
                    {
                        case QuiEntryType.Function:
                        case QuiEntryType.EndFunction:
                        case QuiEntryType.Comment:
                        case QuiEntryType.EmptyLine:
                            sw.WriteLine(entry.Content);
                            break;
                        case QuiEntryType.Name:
                            tabs = Regex.Match(entry.Content, @"(\s+)").Groups[1].Value;
                            sw.WriteLine($"{tabs}(message{(entry.IsKwMessage ? "-kw" : "")} {entry.EditedText} {entry.SecondParameter}");
                            break;
                        case QuiEntryType.Message:
                            {
                                var lines = entry.EditedText.Split('\r');
                                var extras = (entry.Extras.Count > 0 ? " " : "") + string.Join(" ", entry.Extras);

                                for (var i = 0; i < lines.Length; i++)
                                {
                                    var line = lines[i];
                                    var startQuote = !line.StartsWith("(") && !line.StartsWith("\"");
                                    var endQuote = !line.EndsWith(")") && !line.EndsWith("\"") && startQuote;

                                    if (entry.IsLiteral)
                                    {
                                        if (i < lines.Length - 1)
                                            sw.WriteLine(tabs + "\t\t" + line.Replace("\n", string.Empty));
                                        else
                                            sw.WriteLine(tabs + "\t\t" + line.Replace("\n", string.Empty) + extras + ")" + entry.Comment);
                                    }
                                    else
                                    {
                                        sw.Write(tabs + "\t\t" + (startQuote ? "\"" : "") + line.Replace("\n", string.Empty));

                                        if (i < lines.Length - 1)
                                            sw.WriteLine(endQuote ? "\\n\"" : "");
                                        else
                                            sw.WriteLine((endQuote ? "\"" : "") + extras + ")" + entry.Comment);
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}
