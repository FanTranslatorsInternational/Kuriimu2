using Kanvas;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_ganbarion.Images
{
    class Jtex
    {
        private JtexHeader _header;
        private byte[] _unkRegion;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Read unknown region
            _unkRegion = br.ReadBytes(_header.dataOffset - (int)br.BaseStream.Position);

            // Create image info
            input.Position = _header.dataOffset;
            var imageData = br.ReadBytes(_header.dataSize);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = JtexSupport.GetEncodingDefinition().GetColorEncoding(_header.format)?.BitDepth ?? 0,
                ImageData = imageData,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height),
                PadSize = builder => builder.ToPowerOfTwo(),
                RemapPixels = context => new CtrSwizzle(context)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            var paddedSize = SizePadding.PowerOfTwo(imageInfo.ImageSize);

            // Write image data
            output.Position = _header.dataOffset;
            output.Write(imageInfo.ImageData);

            // Update header
            _header.fileSize = (int)output.Length;
            _header.width = (short)paddedSize.Width;
            _header.height = (short)paddedSize.Height;
            _header.format = (byte)imageInfo.ImageFormat;
            _header.dataSize = imageInfo.ImageData.Length;

            // Write header
            output.Position = 0;
            WriteHeader(_header, bw);

            // Write unknown region
            bw.Write(_unkRegion);
        }

        private JtexHeader ReadHeader(BinaryReaderX reader)
        {
            var header = new JtexHeader
            {
                magic = reader.ReadString(4),
                fileSize = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                format = reader.ReadByte(),
                unkCount = reader.ReadByte(),
                unk1 = reader.ReadByte(),
                unk2 = reader.ReadByte()
            };

            header.unkList = ReadIntegers(reader, header.unkCount);
            header.unk3 = reader.ReadInt16();
            header.dataOffset = reader.ReadInt16();
            header.dataSize = reader.ReadInt32();

            return header;
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteHeader(JtexHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileSize);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.format);
            writer.Write(header.unkCount);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            WriteIntegers(header.unkList, writer);
            writer.Write(header.unk3);
            writer.Write(header.dataOffset);
            writer.Write(header.dataSize);
        }

        private void WriteIntegers(int[] elements, BinaryWriterX writer)
        {
            foreach (int element in elements)
                writer.Write(element);
        }
    }
}
