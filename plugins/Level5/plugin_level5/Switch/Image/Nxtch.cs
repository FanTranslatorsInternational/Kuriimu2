using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_level5.Switch.Image
{
    public class Nxtch
    {
        private const int HeaderSize_ = 0x30;

        private NxtchHeader _header;
        private byte[] _unkData;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Read mip offsets
            var mipOffsets = ReadMipOffsets(br, _header.mipMapCount);

            // Read unknown data
            _unkData = br.ReadBytes(0x100 - (int)input.Position);

            input.Position = 0x74;
            var swizzleMode = br.ReadInt32();

            // Read image data
            var baseOffset = 0x100;

            input.Position = baseOffset + mipOffsets[0];
            var dataSize = mipOffsets.Length > 1 ? mipOffsets[1] - mipOffsets[0] : input.Length - baseOffset;
            var imageData = br.ReadBytes((int)dataSize);

            // Read mip data
            var mipData = new List<byte[]>();
            for (var i = 1; i < _header.mipMapCount; i++)
            {
                input.Position = mipOffsets[i];
                var mipSize = i + 1 >= _header.mipMapCount ? input.Length - baseOffset : mipOffsets[i + 1] - mipOffsets[i];
                mipData.Add(br.ReadBytes((int)mipSize));
            }

            // Create image info
            var imageInfo = new ImageFileInfo
            {
                BitDepth = imageData.Length * 8 / (_header.width * _header.height),
                ImageData = imageData,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height),
                MipMapData = mipData,
                RemapPixels = context => new NxSwizzle(context, swizzleMode)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            var mipOffset = HeaderSize_;
            var dataOffset = 0x100;

            // Write image and mip data
            var mipOffsets = new List<int> { 0 };

            var dataPosition = dataOffset;
            output.Position = dataPosition;
            bw.Write(imageInfo.ImageData);
            dataPosition += imageInfo.ImageData.Length;

            if ((imageInfo.MipMapData?.Count ?? 0) > 0)
            {
                foreach (byte[] mipData in imageInfo.MipMapData!)
                {
                    mipOffsets.Add(dataPosition - dataOffset);
                    bw.Write(mipData);
                    dataPosition += mipData.Length;
                }
            }

            // Write mip offsets
            output.Position = mipOffset;
            WriteMipOffsets(mipOffsets, bw);

            // Write unknown data
            bw.Write(_unkData);

            // Write header
            _header.mipMapCount = imageInfo.MipMapData?.Count + 1 ?? 1;
            _header.format = imageInfo.ImageFormat;
            _header.width = imageInfo.ImageSize.Width;
            _header.height = imageInfo.ImageSize.Height;
            _header.textureDataSize = (int)(output.Length - dataOffset);
            _header.textureDataSize2 = (int)(output.Length - dataOffset);

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private NxtchHeader ReadHeader(BinaryReaderX reader)
        {
            return new NxtchHeader
            {
                magic = reader.ReadString(8),
                textureDataSize = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                width = reader.ReadInt32(),
                height = reader.ReadInt32(),
                unk3 = reader.ReadInt32(),
                unk4 = reader.ReadInt32(),
                format = reader.ReadInt32(),
                mipMapCount = reader.ReadInt32(),
                textureDataSize2 = reader.ReadInt32()
            };
        }

        private int[] ReadMipOffsets(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteHeader(NxtchHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.textureDataSize);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.unk3);
            writer.Write(header.unk4);
            writer.Write(header.format);
            writer.Write(header.mipMapCount);
            writer.Write(header.textureDataSize2);
        }

        private void WriteMipOffsets(IList<int> offsets, BinaryWriterX writer)
        {
            foreach (int offset in offsets)
                writer.Write(offset);
        }
    }
}
