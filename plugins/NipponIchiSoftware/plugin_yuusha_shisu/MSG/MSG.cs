using System.IO;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Models.Text;

namespace plugin_yuusha_shisu.MSG
{
    /// <summary>
    /// 
    /// </summary>
    public class MSG
    {
        /// <summary>
        /// 
        /// </summary>
        private MSGContent _content;

        public TextEntry Load(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Read
                _content = br.ReadType<MSGContent>();
                return new TextEntry
                {
                    Name = "content",
                    EditedText = _content.Content,
                    OriginalText = _content.Content
                };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        public void Save(Stream output, TextEntry entry)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
                _content.CharacterCount = (short)entry.EditedText.Trim().Length;
                _content.CharacterDataSize = (short)Encoding.UTF8.GetByteCount(entry.EditedText);
                bw.WriteType(_content);
                bw.Write((byte)0x0);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class MSGContent
    {
        public int Magic;
        public int HeaderSize;
        public short CharacterCount;
        public short CharacterDataSize;
        public int ExtraMagic;
        [VariableLength("CharacterDataSize", StringEncoding = StringEncoding.UTF8)]
        public string Content;
    }
}
