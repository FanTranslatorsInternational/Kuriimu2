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
                            var isMessage = line.Trim().StartsWith("(message ");

                            if (isMessage)
                            {
                                //var tabs = Regex.Match(line, @"(\s+)").Value;
                                var lin = Regex.Match(line, "^\\s*\\(message (.+)$").Groups[1].Value;
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
                                        case ' ': // End parameter if not in a string and sub count is 0
                                            if (subCount == 0 && !inString)
                                                NewPart(part);
                                            else
                                                part += chr;
                                            break;
                                        default:
                                            part += chr;
                                            break;
                                    }

                                    position++;

                                    if (ended)
                                    {
                                        // Read possible comment
                                        while (position < lin.Length)
                                        {
                                            comment += lin[position];
                                            position++;
                                        }
                                        break;
                                    }
                                } while (functionCount > 0);

                                // Add Name
                                if (parts[0] != "nil")
                                    _allEntries.Add(new QuiTextEntry
                                    {
                                        Content = content,
                                        Comment = parts[1], // Store the second part in Comment
                                        Type = QuiEntryType.Name,
                                        Name = $"Name{index.ToString()}",
                                        EditedText = parts[0],
                                        OriginalText = parts[0]
                                    });

                                // Add Message
                                var message = new QuiTextEntry
                                {
                                    Content = messageContent,
                                    Comment = comment,
                                    Type = QuiEntryType.Message,
                                    Name = $"Message{index.ToString()}"
                                };

                                var text = "";
                                foreach (var p in parts.Skip(2))
                                {
                                    // Spin extras off
                                    switch (p)
                                    {
                                        case "extra":
                                            message.Extras.Add(p);
                                            break;
                                        default:
                                            text += p.Replace("\\n", "\r\n").Trim('"');
                                            break;
                                    }
                                }
                                message.EditedText = text;
                                message.OriginalText = text;

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
