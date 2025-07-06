using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;
using ByteOrder = Komponent.Contract.Enums.ByteOrder;

namespace plugin_koei_tecmo.Images
{
    class G1t
    {
        private static readonly int HeaderSize = 0x1C;

        private G1tPlatform _platform;

        private G1tHeader _header;
        private IList<int> _unkRegion;

        public List<ImageFileInfo> Load(Stream input, G1tPlatform platform)
        {
            _platform = platform;

            using var br = new BinaryReaderX(input);

            // Set endianess
            var magic = br.ReadString(4);
            br.ByteOrder = magic == "GT1G" ? ByteOrder.LittleEndian : ByteOrder.BigEndian;

            // Read header
            input.Position = 0;
            _header = ReadHeader(br);

            // Read unknown region
            _unkRegion = G1tSupport.ReadIntegers(br, _header.texCount);

            // Read offsets
            input.Position = _header.dataOffset;
            var offsets = G1tSupport.ReadIntegers(br, _header.texCount);

            // Read images
            var result = new List<ImageFileInfo>();
            foreach (var offset in offsets)
            {
                // Read entry
                input.Position = _header.dataOffset + offset;
                var entry = ReadEntry(br);

                // Read image data
                var bitDepth = G1tSupport.GetBitDepth(entry.format, platform);

                var dataSize = entry.Width * entry.Height * bitDepth / 8;
                var imageData = br.ReadBytes(dataSize);

                // Read mips
                var mips = new List<byte[]>();
                for (var i = 1; i < entry.MipCount; i++)
                {
                    dataSize = (entry.Width >> i) * (entry.Height >> i) * bitDepth / 8;
                    mips.Add(br.ReadBytes(dataSize));
                }

                // Create image info
                var imageInfo = new G1tImageFileInfo
                {
                    Entry = entry,
                    BitDepth = bitDepth,
                    ImageData = imageData,
                    ImageFormat = entry.format,
                    ImageSize = new Size(entry.Width, entry.Height),
                    MipMapData = mips,
                    RemapPixels = context => G1tSupport.GetSwizzle(context, entry.format, platform),
                    PadSize = builder => builder.ToPowerOfTwo()
                };

                result.Add(imageInfo);
            }

            return result;
        }

        public void Save(Stream output, IList<ImageFileInfo> imageInfos)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var unkRegionOffset = HeaderSize;
            var offsetsOffset = unkRegionOffset + imageInfos.Count * 4;
            var dataOffset = offsetsOffset + imageInfos.Count * 4;

            // Write image data
            var offsets = new List<int>();

            output.Position = dataOffset;
            foreach (var imageInfo in imageInfos.Cast<G1tImageFileInfo>())
            {
                offsets.Add((int)(output.Position - offsetsOffset));

                // Update entry
                imageInfo.Entry.Width = imageInfo.ImageSize.Width;
                imageInfo.Entry.Height = imageInfo.ImageSize.Height;
                imageInfo.Entry.format = (byte)imageInfo.ImageFormat;

                // Write entry
                WriteEntry(imageInfo.Entry, bw);

                // Write image data
                bw.Write(imageInfo.ImageData);

                // Write mips
                if ((imageInfo.MipMapData?.Count ?? 0) > 0)
                    foreach (var mip in imageInfo.MipMapData)
                        bw.Write(mip);
            }

            // Write offsets
            output.Position = offsetsOffset;
            WriteIntegers(offsets, bw);

            // Write unknown region
            output.Position = unkRegionOffset;
            WriteIntegers(_unkRegion, bw);

            // Write header
            _header.dataOffset = offsetsOffset;
            _header.texCount = imageInfos.Count;
            _header.fileSize = (int)output.Length;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private G1tHeader ReadHeader(BinaryReaderX reader)
        {
            return new G1tHeader
            {
                magic = reader.ReadString(8),
                fileSize = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                texCount = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private G1tEntry ReadEntry(BinaryReaderX reader)
        {
            var entry = new G1tEntry
            {
                mipUnk = reader.ReadByte(),
                format = reader.ReadByte(),
                dimension = reader.ReadByte(),
                zero0 = reader.ReadByte(),
                swizzle = reader.ReadByte(),
                unk3 = reader.ReadByte(),
                unk4 = reader.ReadByte(),
                extHeader = reader.ReadByte()
            };

            if (entry.extHeader > 0)
            {
                entry.extHeaderSize = reader.ReadInt32();
                entry.extHeaderContent = reader.ReadBytes(entry.extHeaderSize - 4);
            }

            return entry;
        }

        private void WriteHeader(G1tHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileSize);
            writer.Write(header.dataOffset);
            writer.Write(header.texCount);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
        }

        private void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }

        private void WriteEntry(G1tEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.mipUnk);
            writer.Write(entry.format);
            writer.Write(entry.dimension);
            writer.Write(entry.zero0);
            writer.Write(entry.swizzle);
            writer.Write(entry.unk3);
            writer.Write(entry.unk4);
            writer.Write(entry.extHeader);

            if (entry.extHeader > 0)
            {
                writer.Write(entry.extHeaderSize);
                writer.Write(entry.extHeaderContent);
            }
        }
    }
}
