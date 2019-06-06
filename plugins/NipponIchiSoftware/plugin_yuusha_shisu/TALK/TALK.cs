using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Interfaces.Text;

namespace plugin_yuusha_shisu.TALK
{
    public class TALK
    {
        private TALKContent _content;
        private List<DrawingFrame> _drawingFrames;
        public List<TextEntry> Entries;

        public TALK(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Read
                _content = br.ReadType<TALKContent>();
                _drawingFrames = br.ReadMultiple<DrawingFrame>(_content.NumberSection);               
                var color = br.ReadMultiple<ColorStruct>(_content.NumberSection);
                var text = br.ReadString(_content.CharacterDataSize, Encoding.UTF8);

                Entries = new List<TextEntry>
                {
                    new TextEntry
                    {
                        Name = "content",
                        EditedText = text,
                        OriginalText = text
                    }
                };
            }
        }
        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
                _content.CharacterCount = (short)Entries.First().EditedText.Trim().Length;
                _content.CharacterDataSize = (short)Encoding.UTF8.GetByteCount(Entries.First().EditedText);
                bw.WriteType(_content);
                bw.Write((byte)0x0);
            }
        }
    }

    public class TALKContent
    {
        public int Magic;
        public short TextOffset;
        public short NumberSection;
        public short CharacterCount;
        public short CharacterDataSize;
        public int Null;
    }

    public class DrawingFrame
    {
        public short Indicator = 0x0200;
        public short FrameCounter;
    }

    public class ColorStruct
    {
        public int Unk1;
        public int RGBA;
        public int Unk2;
        public int Unk3;
    }
}
