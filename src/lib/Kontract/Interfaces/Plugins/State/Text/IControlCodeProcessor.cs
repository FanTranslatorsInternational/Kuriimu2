using System.Text;
using Kontract.Models.Plugins.State.Text;

namespace Kontract.Interfaces.Plugins.State.Text
{
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
}
