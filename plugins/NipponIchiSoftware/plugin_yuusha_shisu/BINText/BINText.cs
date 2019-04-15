using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Text;

namespace plugin_yuusha_shisu.BINText
{
    class BINText
    {
        private BINTextContent _content;
        public List<TextEntry> Entries;


        public BINText(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Read
                _content = br.ReadType<BINTextContent>();
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

    public class BINTextContent
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
