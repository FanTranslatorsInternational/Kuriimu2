using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Kontract.Models.Text;

namespace plugin_yuusha_shisu.TALK
{
    public class TALK
    {
        private TALKContent _content;
        private IList<DrawingFrame> _drawingFrames;
        private IList<ColorStruct> _colorStructs;

        public TextEntry Load(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Read
                _content = br.ReadType<TALKContent>();
                _drawingFrames = br.ReadMultiple<DrawingFrame>(_content.NumberSection);
                _colorStructs = br.ReadMultiple<ColorStruct>(_content.NumberSection);
                var text = br.ReadString(_content.CharacterDataSize, Encoding.UTF8);
                /*var str = "";
                var color= 0xFFFFFFFF;
                for (var i = 0; i < text.Length ; i++)
                {
                    color = _colorStructs[i].RGBA;
                    if (i < 0)
                    {

                    }
                    str += text[i];
                }*/

                return new TextEntry
                {
                    Name = "content",
                    EditedText = text,
                    OriginalText = text
                };
            }
        }
        public void Save(Stream output,TextEntry textEntry)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
                _content.CharacterCount = (short)textEntry.EditedText.Trim().Length;
                _content.CharacterDataSize = (short)Encoding.UTF8.GetByteCount(textEntry.EditedText);
                bw.WriteType(_content);
                bw.WriteType(_drawingFrames[0]);

                short newFrame = _drawingFrames[0].FrameCounter;
                string TextLength = textEntry.EditedText.Trim();
                TextLength = TextLength.Replace("\r", "").Replace("\n", "");
                for (var i = 1; i < TextLength.Length; i++)
                {
                    bw.WriteType(_drawingFrames[0].Indicator);
                    newFrame += 0x0C;
                    bw.Write(newFrame);
                }

                for (var i = 0; i < TextLength.Length; i++)
                {
                    bw.WriteType(_colorStructs[0]);
                }

                bw.WriteString(textEntry.EditedText, Encoding.UTF8, false);
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
        public short Indicator = 0x0002;
        public short FrameCounter;
    }

    public class ColorStruct
    {
        public int Unk1 = 0x00000000;
        public uint RGBA = 0xFFFFFFFF;
        public int Unk2 = 0x00000001;
        public int Unk3 = 0x0000001A;
    }
}
