using System.Drawing;
using System.IO;
using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_atlus.Images
{
    class Stex
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(StexHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(StexEntry));

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            var header = br.ReadType<StexHeader>();

            // Read entry
            var entry = br.ReadType<StexEntry>();

            // Read name
            var name = br.ReadCStringASCII();

            // Create image info
            input.Position = entry.offset;
            var imageData = br.ReadBytes(header.dataSize);

            var format = (header.dataType << 16) | header.imageFormat;

            var imageInfo = new StexImageInfo(imageData, (int)format, new Size(header.width, header.height), entry)
            {
                Name = name
            };
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var nameOffset = entryOffset + EntrySize;
            var dataOffset = (nameOffset + Encoding.ASCII.GetByteCount(imageInfo.Name) + 1 + 0x7F) & ~0x7F;

            // Write image data
            output.Position = dataOffset;
            output.Write(imageInfo.ImageData);

            // Write name
            output.Position = nameOffset;
            bw.WriteString(imageInfo.Name, Encoding.ASCII, false);

            // Write entry
            output.Position = entryOffset;
            bw.WriteType(new StexEntry
            {
                offset = dataOffset,
                unk1 = (imageInfo as StexImageInfo).Entry.unk1
            });

            // Write header
            var header = new StexHeader
            {
                width = imageInfo.ImageSize.Width,
                height = imageInfo.ImageSize.Height,
                dataSize = (int)(output.Length - dataOffset),
                dataType = (uint)((imageInfo.ImageFormat >> 16) & 0xFFFF),
                imageFormat = (uint)(imageInfo.ImageFormat & 0xFFFF)
            };

            output.Position = 0;
            bw.WriteType(header);
        }
    }
}
