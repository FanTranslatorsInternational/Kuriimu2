using System;
using System.Text;

namespace Kontract.Models.Text.ControlCodeProcessor
{
    /// <summary>
    /// The default implementation of <see cref="IControlCodeProcessor"/>.
    /// </summary>
    class DefaultControlCodeProcessor : IControlCodeProcessor
    {
        public ProcessedText Read(byte[] data, Encoding encoding)
        {
            if (data == null || encoding == null)
                return new ProcessedText(string.Empty);

            return new ProcessedText(encoding.GetString(data));
        }

        public byte[] Write(ProcessedText text, Encoding encoding)
        {
            if (text == null || encoding == null)
                return Array.Empty<byte>();

            return encoding.GetBytes(text.ToString());
        }
    }
}
