using System.Collections.Generic;
using System.IO;
using Kanvas;
using Komponent.IO;
using Kontract.Interfaces.Image;

namespace plugin_blue_reflection.KSLT
{
    /// <summary>
    /// 
    /// </summary>
    public class KSLT
    {
        /// <summary>
        /// 
        /// </summary>
        private FileHeader _header;

        /// <summary>
        /// 
        /// </summary>
        private List<UnkPadding> _padding;

        /// <summary>
        /// 
        /// </summary>
        public List<BitmapInfo> Bitmaps = new List<BitmapInfo>();        

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
                _padding = br.ReadMultiple<UnkPadding>(_header.FileCount);
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
                    Bitmaps.Add(new KsltBitmapInfo(Common.Load(texture, settings), new FormatInfo(0x0, ImageFormats.Formats[0x0].FormatName)) {
                        Name = fileNames[offsets.IndexOf(o)],
                        Header = imgHeader,
                        ImageData = texture
                    });
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public bool Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
                bw.WriteType(_header);
                bw.BaseStream.Position = 0x40;
                bw.WriteMultiple(_padding);
                var offsetTablePos = bw.BaseStream.Position;
                bw.BaseStream.Position += (0x14 * _header.FileCount);
                foreach (var b in Bitmaps)
                    bw.WriteString(b.Name, System.Text.Encoding.ASCII, false, true);
                var newOffsets = new List<OffsetEntry>();
                foreach (var b in Bitmaps)
                {
                    var kbi = b as KsltBitmapInfo;
                    newOffsets.Add(new OffsetEntry() { Offset = (int)bw.BaseStream.Position });
                    kbi.Header.Width = (short)kbi.Image.Width;
                    kbi.Header.Height = (short)kbi.Image.Height;
                    kbi.Header.DataSize = kbi.ImageData.Length;
                    bw.WriteType(kbi.Header);
                    bw.Write(kbi.ImageData);                    
                }
                bw.BaseStream.Position = offsetTablePos;
                bw.WriteMultiple(newOffsets);
            }
            return true;
        }
    }
}
