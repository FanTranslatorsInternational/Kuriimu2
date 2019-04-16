using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Text;

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

        /// <summary>
        /// 
        /// </summary>
        public List<TextEntry> Entries;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        public MSG(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Read
                _content = br.ReadType<MSGContent>();
                Entries = new List<TextEntry>
                {
                    new TextEntry
                    {
                        Name = "content",
                        EditedText = _content.Content,
                        OriginalText = _content.Content
                    }
                };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
                _content.CharacterCount = (short) Entries.First().EditedText.Trim().Length;
                _content.CharacterDataSize = (short)Encoding.UTF8.GetByteCount(Entries.First().EditedText);
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
