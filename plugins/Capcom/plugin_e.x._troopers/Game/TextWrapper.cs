using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Kontract.Interfaces;

namespace plugin_e.x._troopers.Game
{
    public class TextWrapper
    {
        public class TextWrappingResults
        {
            public string Text { get; set; } = string.Empty;
            public int LineCount { get; set; }
        }

        public static TextWrappingResults WrapText(string text, IFontRenderer font, RectangleF textBox, float scaleX, float xAdjust, string newLine = "\r\n")
        {
            if (text.Length == 0) return new TextWrappingResults();

            // Nav
            var cursor = 0;
            var wordStart = -1;
            var word = string.Empty;
            var line = string.Empty;
            var lineCount = 1;

            var result = string.Empty;

            while (cursor < text.Length)
            {
                var c = text[cursor];

                // New Lines
                if (c == '\n' || c == '\u000A')
                {
                    // Render the current word
                    if (word.Length > 0)
                    {
                        if (MeasureText(line + word, font, scaleX, xAdjust) <= textBox.Width)
                            line += word;
                        else // Next line
                        {
                            line += newLine;
                            result += line;

                            // Reset
                            lineCount++;
                            line = word;
                        }

                        wordStart = -1;
                        word = string.Empty;
                    }

                    // Add the new line
                    line += newLine;
                    result += line;

                    // Reset
                    lineCount++;
                    line = string.Empty;
                }
                // Spaces
                else if (Regex.IsMatch(c.ToString(), @"\s"))
                {
                    // Render the current word
                    if (word.Length > 0)
                    {
                        // Add the word and the space if they fit
                        if (MeasureText(line + word + c, font, scaleX, xAdjust) <= textBox.Width)
                        {
                            line += word + c;
                        }
                        // Add the word if it fits
                        else if (MeasureText(line + word, font, scaleX, xAdjust) <= textBox.Width)
                        {
                            line += word + newLine;
                            result += line;

                            // Reset
                            lineCount++;
                            line = string.Empty;
                        }
                        // Add a new line if nothing fits
                        else
                        {
                            line += newLine;
                            result += line;

                            // Reset
                            lineCount++;
                            line = word + c;
                        }

                        wordStart = -1;
                        word = string.Empty;
                    }
                    // Render the space
                    else
                    {
                        if (MeasureText(line + c, font, scaleX, xAdjust) <= textBox.Width)
                            line += c;
                        else
                        {
                            line += newLine;
                            result += line;

                            // Reset
                            lineCount++;
                            line = c.ToString();
                        }
                    }
                }
                // Words
                else
                {
                    if (wordStart == -1)
                        wordStart = cursor;
                    word += c;

                    // Single word exceeds line width
                    if (MeasureText(word, font, scaleX, xAdjust) > textBox.Width)
                    {
                        // Might need to be just >
                        for (var i = cursor; i > wordStart; i--)
                        {
                            word = word.Substring(0, i - wordStart);
                            if (MeasureText(line + word, font, scaleX, xAdjust) <= textBox.Width)
                            {
                                cursor = i - 1;
                                break;
                            }
                        }

                        line += word + newLine;
                        result += line;

                        // Reset
                        lineCount++;
                        line = string.Empty;

                        wordStart = -1;
                        word = string.Empty;
                    }

                    // If the final word doesn't fit at the end of the line
                    if (cursor == text.Length - 1 && MeasureText(line + word, font, scaleX, xAdjust) > textBox.Width)
                    {
                        line += newLine;
                        result += line;

                        // Reset
                        lineCount++;
                        line = string.Empty;
                    }
                }

                cursor++;
            }

            // Add the final word/line
            line += word;
            result += line;

            return new TextWrappingResults
            {
                Text = result,
                LineCount = lineCount
            };
        }

        public static float MeasureText(string text, IFontRenderer font, float scaleX, float xAdjust)
        {
            return text.Sum(c => font.GetCharWidthInfo(c).GlyphWidth * scaleX + xAdjust);
        }
    }
}
