using System;
using System.Collections.Generic;
using System.Text;
using Kontract.Interfaces.Plugins.State.Text;

namespace Kontract.Models.Plugins.State.Text
{
    /// <summary>
    /// The base class for pages.
    /// </summary>
    public class TextInfo : ICloneable
    {
        /// <summary>
        /// The text data in bytes for this entry.
        /// </summary>
        public byte[] TextData { get; private set; }

        /// <summary>
        /// The encoding for the text data.
        /// </summary>
        public Encoding Encoding { get; private set; } = Encoding.UTF8;

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
        /// Determines whether the entry can parse control codes.
        /// </summary>
        public bool CanParseControlCodes => ControlCodeProcessor != null;

        /// <summary>
        /// Determines whether the entry can split the text into pages.
        /// </summary>
        public bool CanPageText => TextPager != null;

        /// <summary>
        /// Creates an empty <see cref="TextInfo"/> without a name.
        /// </summary>
        /// <param name="textData">The data to decode and process.</param>
        /// <param name="encoding">The encoding to decode <see cref="TextData"/> with.</param>
        public TextInfo(byte[] textData, Encoding encoding)
        {
            TextData = textData;
            Encoding = encoding;
        }

        /// <summary>
        /// Creates a new <see cref="TextInfo"/>.
        /// </summary>
        /// <param name="text">The text to process.</param>
        public TextInfo(string text)
        {
            TextData = Encoding.GetBytes(text);
        }

        /// <summary>
        /// Creates a new text entry instance; For usage in <see cref="Clone"/>.
        /// </summary>
        private TextInfo()
        {
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
            if (CanParseControlCodes)
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
            if (CanPageText)
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

            if (CanParseControlCodes)
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

            if (CanPageText && pages.Count > 1)
            {
                SetProcessedText(TextPager.Merge(pages));
                return;
            }

            SetProcessedText(pages[0]);
        }

        #endregion

        public object Clone()
        {
            return new TextInfo
            {
                TextData = TextData,
                Encoding = Encoding,
                Name = Name,
                ControlCodeProcessor = ControlCodeProcessor,
                TextPager = TextPager
            };
        }
    }
}
