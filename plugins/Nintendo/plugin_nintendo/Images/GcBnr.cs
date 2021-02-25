using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Configuration;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_nintendo.Images
{
    class GcBnr
    {
        private static int TitleInfoSize = Tools.MeasureType(typeof(GcBnrTitleInfo));

        private GcBnrHeader _header;
        private IList<GcBnrTitleInfo> _titleInfos;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<GcBnrHeader>();

            // Read image data
            var imageData = br.ReadBytes(0x1800);

            // Read title info
            var titleInfoCount = (int)(input.Length - input.Position) / TitleInfoSize;
            _titleInfos = br.ReadMultiple<GcBnrTitleInfo>(titleInfoCount);

            return new ImageInfo(imageData, 0, new Size(96, 32))
            {
                Configuration = new ImageConfiguration().RemapPixelsWith(size => new DolphinSwizzle(size.Width, size.Height))
            };
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var imageDataOffset = 0x20;
            var titleInfoOffset = imageDataOffset + 0x1800;

            // Write title info
            output.Position = titleInfoOffset;
            bw.WriteMultiple(_titleInfos);

            // Write image data
            output.Position = imageDataOffset;
            output.Write(imageInfo.ImageData);

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
