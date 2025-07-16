using Kanvas;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Kryptography.Checksum.Crc;
using SixLabors.ImageSharp;

namespace plugin_level5.N3DS.Image
{
    public class Ztex
    {
        private const int HeaderSize_ = 8;
        private const int EntrySize_ = 0x56;
        private const int UnkEntrySize_ = 8;

        private ZtexHeader _header;
        private IList<ZtexUnkEntry> _unkEntries;

        public IList<ImageFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            var unkCount = 0;
            if (_header.HasUnknownEntries)
                unkCount = br.ReadInt32();

            // Read entries
            var entries = new ZtexEntry[_header.imageCount];
            for (var i = 0; i < _header.imageCount; i++)
            {
                if (_header.HasExtendedEntries)
                    input.Position += 4;
                entries[i] = ReadEntry(br);
            }

            // Read unknown entries
            if (_header.HasUnknownEntries)
                _unkEntries = ReadUnknownEntries(br, unkCount);

            // Add images
            var encodingDefinition = ZtexSupport.GetEncodingDefinition();

            var result = new List<ImageFileInfo>();
            foreach (var entry in entries)
            {
                var bitDepth = encodingDefinition.GetColorEncoding(entry.format)?.BitDepth ?? 1;

                // Read image data
                var imgDataSize = entry.width * entry.height * bitDepth / 8;

                input.Position = entry.offset;
                var imgData = br.ReadBytes(imgDataSize);

                // Read mip data
                var mipData = new List<byte[]>();
                for (var i = 1; i < entry.mipCount; i++)
                    mipData.Add(br.ReadBytes(imgDataSize >> (i * 2)));

                // Create image info
                var imgInfo = new ImageFileInfo
                {
                    Name = entry.name.Trim('\0'),
                    BitDepth = imgData.Length * 8 / (entry.width * entry.height),
                    ImageData = imgData,
                    MipMapData = mipData,
                    ImageFormat = entry.format,
                    ImageSize = new Size(entry.width, entry.height),
                    RemapPixels = context => new CtrSwizzle(context),
                    PadSize = options => options.ToPowerOfTwo()
                };

                result.Add(imgInfo);
            }

            return result;
        }

        public void Save(Stream output, IList<ImageFileInfo> imageInfos)
        {
            var crc32 = Crc32.Crc32B;

            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize_ + (_header.HasUnknownEntries ? 4 : 0);
            var unkOffset = entryOffset + imageInfos.Count * EntrySize_ + (_header.HasExtendedEntries ? imageInfos.Count * 4 : 0);
            var dataOffset = (unkOffset + (_header.HasUnknownEntries ? _unkEntries.Count * UnkEntrySize_ : 0) + 0x7F) & ~0x7F;

            // Write image data
            var entries = new List<ZtexEntry>();

            var dataPosition = dataOffset;
            foreach (var imageInfo in imageInfos)
            {
                // Write image data
                output.Position = dataPosition;
                bw.Write(imageInfo.ImageData);

                foreach (var mipData in imageInfo.MipMapData)
                    bw.Write(mipData);

                // Create entry
                entries.Add(new ZtexEntry
                {
                    name = imageInfo.Name?.PadRight(0x40, '\0'),
                    crc32 = crc32.ComputeValue(imageInfo.Name),
                    offset = dataPosition,
                    dataSize = (int)(output.Position - dataPosition),
                    width = (short)SizePadding.PowerOfTwo(imageInfo.ImageSize.Width),
                    height = (short)SizePadding.PowerOfTwo(imageInfo.ImageSize.Height),
                    mipCount = (byte)((imageInfo.MipMapData?.Count ?? 0) + 1),
                    format = (byte)imageInfo.ImageFormat,
                    unk3 = 0xFF
                });

                dataPosition = (int)((output.Position + 0x7F) & ~0x7F);
            }

            // Write entries
            output.Position = entryOffset;
            foreach (var entry in entries)
            {
                if (_header.HasExtendedEntries)
                    bw.Write(0);
                WriteEntry(entry, bw);
            }

            if (_header.HasUnknownEntries)
                WriteUnknownEntries(_unkEntries, bw);

            // Write header
            _header.imageCount = (short)imageInfos.Count;

            output.Position = 0;
            WriteHeader(_header, bw);

            if (_header.HasUnknownEntries)
                bw.Write(_unkEntries.Count);
        }

        private ZtexHeader ReadHeader(BinaryReaderX reader)
        {
            return new ZtexHeader
            {
                magic = reader.ReadString(4),
                imageCount = reader.ReadInt16(),
                flags = reader.ReadInt16()
            };
        }

        private ZtexEntry ReadEntry(BinaryReaderX reader)
        {
            return new ZtexEntry
            {
                name = reader.ReadString(0x40),
                crc32 = reader.ReadUInt32(),
                offset = reader.ReadInt32(),
                zero1 = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                mipCount = reader.ReadByte(),
                format = reader.ReadByte(),
                unk3 = reader.ReadInt16()
            };
        }

        private ZtexUnkEntry[] ReadUnknownEntries(BinaryReaderX reader, int count)
        {
            var result = new ZtexUnkEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadUnknownEntry(reader);

            return result;
        }

        private ZtexUnkEntry ReadUnknownEntry(BinaryReaderX reader)
        {
            return new ZtexUnkEntry
            {
                unk0 = reader.ReadInt32(),
                zero0 = reader.ReadInt32()
            };
        }

        private void WriteHeader(ZtexHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.imageCount);
            writer.Write(header.flags);
        }

        private void WriteEntry(ZtexEntry entry, BinaryWriterX writer)
        {
            writer.WriteString(entry.name, writeNullTerminator: false);
            writer.Write(entry.crc32);
            writer.Write(entry.offset);
            writer.Write(entry.zero1);
            writer.Write(entry.dataSize);
            writer.Write(entry.width);
            writer.Write(entry.height);
            writer.Write(entry.mipCount);
            writer.Write(entry.format);
            writer.Write(entry.unk3);
        }

        private void WriteUnknownEntries(IList<ZtexUnkEntry> entries, BinaryWriterX writer)
        {
            foreach (ZtexUnkEntry entry in entries)
                WriteUnknownEntry(entry, writer);
        }

        private void WriteUnknownEntry(ZtexUnkEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.unk0);
            writer.Write(entry.zero0);
        }
    }
}
