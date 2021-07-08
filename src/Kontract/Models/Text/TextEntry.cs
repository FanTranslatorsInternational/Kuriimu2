using System.Collections.Generic;
using System.Text;

namespace Kontract.Models.Text
{
    /// <summary>
    /// The base class for pages.
    /// </summary>
    public class TextEntry
    {
        /// <summary>
        /// The text data in bytes for this entry.
        /// </summary>
        public byte[] TextData { get; private set; }

        /// <summary>
        /// The encoding for the text data.
        /// </summary>
        public Encoding Encoding { get; } = Encoding.UTF8;

        /// <summary>
        /// The name for this entry.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// (Optional) The processor to parse control codes given in the text data.
        /// </summary>
        public IControlCodeProcessor ControlCodeProcessor { get; set; }

        /// <summary>
        /// (Optional) The pager to split pages from the processed text.
        /// </summary>
        public ITextPager TextPager { get; set; }

        /// <summary>
        /// Determines if the content of this entry was modified.
        /// </summary>
        public bool ContentChanged { get; private set; }

        /// <summary>
        /// Creates an empty <see cref="TextEntry"/> without a name.
        /// </summary>
        /// <param name="textData">The data to decode and process.</param>
        /// <param name="encoding">The encoding to decode <see cref="TextData"/> with.</param>
        public TextEntry(byte[] textData, Encoding encoding)
        {
            TextData = textData;
            Encoding = encoding;
        }

        /// <summary>
        /// Creates a new <see cref="TextEntry"/>.
        /// </summary>
        /// <param name="text">The text to process.</param>
        public TextEntry(string text)
        {
            TextData = Encoding.GetBytes(text);
        }

        #region Get methods

        /// <summary>
        /// Decodes <see cref="TextData"/> with <see cref="Encoding"/>.
        /// </summary>
        /// <returns>The decoded text.</returns>
        public string GetText()
        {
            return Encoding.GetString(TextData);
        }

        /// <summary>
        /// Decodes <see cref="TextData"/> with <see cref="Encoding"/> and processes control codes with <see cref="ControlCodeProcessor"/>. 
        /// </summary>
        /// <returns>The decoded and processed text.</returns>
        public ProcessedText GetProcessedText()
        {
            if (ControlCodeProcessor != null)
                return ControlCodeProcessor.Read(TextData, Encoding);

            return new ProcessedText(GetText());
        }

        /// <summary>
        /// Retrieves the decoded, processed and paged text of this entry.
        /// This will return the same as <see cref="GetText"/> if no <see cref="TextPager"/> is given.
        /// </summary>
        /// <returns>The decoded and processed text, paged by <see cref="TextPager"/>.</returns>
        public IList<ProcessedText> GetPagedText()
        {
            if (TextPager != null)
                return TextPager.Split(GetProcessedText());

            return new List<ProcessedText> { GetProcessedText() };
        }

        #endregion

        #region Set methods

        /// <summary>
        /// Sets the <see cref="TextData"/> of this entry.
        /// </summary>
        /// <param name="text">The text to encode.</param>
        public void SetText(ProcessedText text)
        {
            TextData = Encoding.GetBytes(text.Serialize());
            ContentChanged = true;
        }

        /// <summary>
        /// Sets the <see cref="TextData"/> of this entry.
        /// </summary>
        /// <param name="text">The text to encode and process.</param>
        public void SetProcessedText(ProcessedText text)
        {
            if (text == null)
                return;

            if (ControlCodeProcessor != null)
            {
                TextData = ControlCodeProcessor.Write(text, Encoding);
                ContentChanged = true;

                return;
            }

            SetText(text);
        }

        /// <summary>
        /// Sets the <see cref="TextData"/> of this entry, after merging pages back together.
        /// </summary>
        /// <param name="pages">The pages to merge, process, and encode.</param>
        public void SetPagedText(IList<ProcessedText> pages)
        {
            if (pages.Count <= 0)
                return;

            if (TextPager != null && pages.Count > 1)
            {
                SetProcessedText(TextPager.Merge(pages));
                return;
            }

            SetProcessedText(pages[0]);
        }

        #endregion
    }

    /// <summary>
    /// The base interface for processing control codes.
    /// </summary>
    public interface IControlCodeProcessor
    {
        /// <summary>
        /// Decodes and processes text data.
        /// </summary>
        /// <param name="data">The data to decode and process.</param>
        /// <param name="encoding">The encoding the text data is presented in.</param>
        /// <returns>The decoded and processed text.</returns>
        public ProcessedText Read(byte[] data, Encoding encoding);

        /// <summary>
        /// Encodes and processes text.
        /// </summary>
        /// <param name="text">The text to encode and process.</param>
        /// <param name="encoding">The encoding the text should be encoded in.</param>
        /// <returns>The encoded and processed text data.</returns>
        public byte[] Write(ProcessedText text, Encoding encoding);
    }

    /// <summary>
    /// The base interface for paging processed texts.
    /// </summary>
    public interface ITextPager
    {
        /// <summary>
        /// Splits a processed text by certain conditions into multiple processed texts.
        /// </summary>
        /// <param name="text">The text to split.</param>
        /// <returns>The split text.</returns>
        IList<ProcessedText> Split(ProcessedText text);

        /// <summary>
        /// Merges multiple processed texts by certain conditions.
        /// </summary>
        /// <param name="texts">The texts to merge.</param>
        /// <returns>The merged text.</returns>
        ProcessedText Merge(IList<ProcessedText> texts);
    }
}
