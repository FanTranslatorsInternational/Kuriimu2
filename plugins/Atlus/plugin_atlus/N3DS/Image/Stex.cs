using Kanvas;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Kryptography.Checksum.Crc;
using SixLabors.ImageSharp;
using System.Text;

namespace plugin_atlus.N3DS.Image
{
    public class Stex
    {
        private static readonly int HeaderSize = 32;
        private static readonly int EntrySize = 8;
        private static int unk1;

        public ImageFileInfo Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input);
            
            // Read header
            var header = typeReader.Read<StexHeader>(br);

            // Read entry
            var entry = typeReader.Read<StexEntry>(br);

            // Quick hack (We will probably replace this in the future)
            unk1 = entry.unk1;

            // Read name
            var name = br.ReadString();

            // Create image info
            input.Position = entry.offset;
            var imageData = br.ReadBytes(header.dataSize);

            var format = (header.dataType << 16) | header.imageFormat;

            var imageInfo = new ImageFileInfo
            {
                Name = name,
                BitDepth = imageData.Length * 8 / (header.width * header.height),
                ImageData = imageData,
                ImageFormat = (int)format,
                ImageSize = new Size(header.width, header.height),
                RemapPixels = context => new CtrSwizzle(context),
            };

            return imageInfo;            
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            var typeWriter = new BinaryTypeWriter();
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
            typeWriter.Write(new StexEntry
            {
                offset = dataOffset,
                unk1 = unk1
            }, bw);

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
            typeWriter.Write(header, bw);
        }
    }
}
