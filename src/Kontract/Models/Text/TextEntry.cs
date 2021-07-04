using System;
using System.Text;
using Kontract.Models.Text.ControlCodeProcessor;

namespace Kontract.Models.Text
{
    /// <summary>
    /// The base class for texts.
    /// </summary>
    public class TextEntry
    {
        /// <summary>
        /// The name for this entry.
        /// </summary>
        public string Name { get; } = string.Empty;

        /// <summary>
        /// The text data in bytes for this entry.
        /// </summary>
        public byte[] TextData { get; set; }

        /// <summary>
        /// The encoding for the text data.
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// (Optional) The processor to parse control codes given in the text data.
        /// </summary>
        public IControlCodeProcessor ControlCodeProcessor { get; set; }

        /// <summary>
        /// Creates an empty <see cref="TextEntry"/> without a name.
        /// </summary>
        public TextEntry()
        {
        }

        /// <summary>
        /// Creates a new <see cref="TextEntry"/>.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        public TextEntry(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Retrieves the decoded and processed text of this entry.
        /// </summary>
        /// <returns>The processed text of this entry.</returns>
        public ProcessedText GetText()
        {
            var processor = ControlCodeProcessor ?? new DefaultControlCodeProcessor();
            var enc = Encoding ?? Encoding.ASCII;
            var data = TextData ?? Array.Empty<byte>();

            return processor.Read(data, enc);
        }

        /// <summary>
        /// Sets the <see cref="TextData"/> of this entry.
        /// </summary>
        /// <param name="text">The text to encode and process.</param>
        public void SetText(ProcessedText text)
        {
            var processor = ControlCodeProcessor ?? new DefaultControlCodeProcessor();
            var enc = Encoding ?? Encoding.ASCII;
            text ??= new ProcessedText(string.Empty);

            TextData = processor.Write(text, enc);
        }
    }
}
