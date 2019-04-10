using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas;
using Komponent.IO;

namespace plugin_blue_reflection.KSLT
{
    public class KSLT
    {
        private FileHeader _header;

        /// <summary>
        /// 
        /// </summary>
        public List<Bitmap> Bitmaps = new List<Bitmap>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        public KSLT(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                _header = br.ReadType<FileHeader>();
                br.BaseStream.Position = 0x40;
                br.BaseStream.Position += _header.OffsetTable;

                var offsets = br.ReadMultiple<OffsetEntry>(_header. FileCount);
                
                var fileNames = new List<string>();
                for (int i = 0; i < _header.FileCount; i++)
                    fileNames.Add(br.ReadCStringASCII());

                foreach (var o in offsets)
                {
                    br.BaseStream.Position = o.Offset;
                    var imgHeader = br.ReadType<ImageHeader>();
                    var texture = br.ReadBytes(imgHeader.DataSize);
                    var settings = new ImageSettings
                    {
                        Width = imgHeader.Width,
                        Height = imgHeader.Height,
                        Format = ImageFormats.Formats[0x0]
                    };
                    Bitmaps.Add(Common.Load(texture, settings));
                }
            }
        }

        public bool Save(Stream output)
        {
            /*using (var bw = new BinaryWriterX(output, true))
            {
                
            }
            return true;*/
        }
    }
}
