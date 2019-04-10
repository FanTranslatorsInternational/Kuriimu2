using Kanvas;
using Komponent.IO;
using Kontract.Interfaces.Archive;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace plugin_blue_reflection.KSLT
{
    public class KSLT
    {
        private FileHeader _header;
        public List<Bitmap> bitmaps = new List<Bitmap>();

        public KSLT(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                _header = br.ReadType<FileHeader>();
                br.BaseStream.Position = 0x40;
                //var padding = br.ReadMultiple<unkPadding>(_header.FileCount);
                br.BaseStream.Position += _header.OffsetTable;
                var offsets = br.ReadMultiple<OffsetEntry>(_header. FileCount);
                var fileNames = new List<string>();
                for (int i = 0; i < _header.FileCount; i++)
                {
                    fileNames.Add(br.ReadCStringASCII());
                }
                foreach (var o in offsets)
                {
                    br.BaseStream.Position = o.Offset;
                    var f = br.ReadType<ImageHeader>();
                    var largeTexture = br.ReadBytes(f.DataSize);
                    var settings = new ImageSettings
                    {
                        Width = f.Width,
                        Height = f.Height,
                        Format = ImageFormats.Formats[0x0], //0x0 -> unk
                    };
                    bitmaps.Add(Kanvas.Common.Load(largeTexture, settings));
                }
            }
        }
    }
}
