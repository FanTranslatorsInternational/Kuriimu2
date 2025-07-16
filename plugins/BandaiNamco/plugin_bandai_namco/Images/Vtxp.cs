using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Kryptography.Checksum.Crc;
using SixLabors.ImageSharp;

namespace plugin_bandai_namco.Images
{
    class Vtxp
    {
        private static readonly int HeaderSize = 0x20;
        private static readonly int EntrySize = 0x20;

        private VtxpHeader _header;

        public List<VtxpImageFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);
            br.SeekAlignment(0x20);

            // Read entries
            var entries = ReadEntries(br, _header.imgCount);

            // Read image infos
            var result = new List<VtxpImageFile>();
            var encodingDefinition = VtxpSupport.GetEncodingDefinition();

            foreach (var entry in entries)
            {
                // Read name
                input.Position = entry.nameOffset;
                var name = br.ReadNullTerminatedString();

                // Read palette
                input.Position = entry.paletteOffset;
                var paletteData = br.ReadBytes(entry.dataOffset - entry.paletteOffset);

                // Read data
                input.Position = entry.dataOffset;
                var imgData = br.ReadBytes(entry.dataSize);

                var format = entry.format >> 24 == 0x94 || entry.format >> 24 == 0x95 ? entry.format & 0xFFFF0000 : entry.format;
                var imageInfo = new VtxpImageFile(new ImageFileInfo
                {
                    Name = name,
                    BitDepth = encodingDefinition.GetColorEncoding((int)format)?.BitDepth ??
                               encodingDefinition.GetIndexEncoding((int)format)?.IndexEncoding.BitDepth ?? 0,
                    ImageData = imgData,
                    ImageFormat = (int)format,
                    ImageSize = new Size(entry.width, entry.height),
                }, encodingDefinition, entry);

                switch (entry.type)
                {
                    case 0x02:
                        imageInfo.ImageInfo.RemapPixels = context => new VitaSwizzle(context);
                        break;
                }

                if ((uint)imageInfo.ImageInfo.ImageFormat == 0x94000000 || (uint)imageInfo.ImageInfo.ImageFormat == 0x95000000)
                {
                    imageInfo.ImageInfo.PaletteData = paletteData;
                    imageInfo.ImageInfo.PaletteFormat = (int)(entry.format & 0xFFFF);
                }

                result.Add(imageInfo);
            }

            return result;
        }

        public void Save(Stream output, IList<VtxpImageFile> imageInfos)
        {
            var crc32 = Crc32.Crc32B;
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var stringOffset = entryOffset + imageInfos.Count * EntrySize;
            var hashOffset = (stringOffset + imageInfos.Sum(x => x.ImageInfo.Name.Length + 1) + 3) & ~3;
            var dataOffset = (hashOffset + imageInfos.Count * 8 + 0x7F) & ~0x7F;

            // Write image data
            var entries = new List<VtxpImageEntry>();

            var stringPosition = stringOffset;
            var dataPosition = dataOffset;
            foreach (var imageInfo in imageInfos)
            {
                output.Position = dataPosition;

                // Write palette
                if (imageInfo.ImageInfo.PaletteData is not null)
                    bw.Write(imageInfo.ImageInfo.PaletteData);

                // Write data
                bw.Write(imageInfo.ImageInfo.ImageData);

                // Add entry
                imageInfo.Entry.dataSize = imageInfo.ImageInfo.ImageData.Length + (imageInfo.ImageInfo.PaletteData?.Length ?? 0);
                imageInfo.Entry.paletteOffset = imageInfo.ImageInfo.PaletteData is not null ? dataPosition : 0;
                imageInfo.Entry.dataOffset = dataPosition + imageInfo.ImageInfo.PaletteData?.Length ?? dataPosition;
                imageInfo.Entry.width = (short)imageInfo.ImageInfo.ImageSize.Width;
                imageInfo.Entry.height = (short)imageInfo.ImageInfo.ImageSize.Height;
                imageInfo.Entry.nameOffset = stringPosition;

                imageInfo.Entry.format = (uint)imageInfo.ImageInfo.ImageFormat;
                if ((uint)imageInfo.ImageInfo.ImageFormat == 0x94000000 || (uint)imageInfo.ImageInfo.ImageFormat == 0x95000000)
                    imageInfo.Entry.format |= (uint)imageInfo.ImageInfo.PaletteFormat;

                entries.Add(imageInfo.Entry);

                // Increase positions
                stringPosition += imageInfo.ImageInfo.Name.Length + 1;
                dataPosition += imageInfo.ImageInfo.ImageData.Length + (imageInfo.ImageInfo.PaletteData is null ? imageInfo.ImageInfo.PaletteData.Length : 0);
                dataPosition = (dataPosition + 0x3F) & ~0x3F;
            }

            // Write hash entries
            output.Position = hashOffset;

            var hashEntries = imageInfos.Select((x, i) => (crc32.ComputeValue(x.ImageInfo.Name), i));
            foreach (var (hash, index) in hashEntries.OrderBy(x => x.Item1))
            {
                bw.Write(hash);
                bw.Write(index);
            }

            // Write strings
            output.Position = stringOffset;
            foreach (var name in imageInfos.Select(x => x.ImageInfo.Name))
                bw.WriteString(name, Encoding.ASCII);

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            _header.hashOffset = hashOffset;
            _header.imgCount = imageInfos.Count;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private VtxpHeader ReadHeader(BinaryReaderX reader)
        {
            return new VtxpHeader
            {
                magic = reader.ReadString(4),
                version = reader.ReadInt32(),
                imgCount = reader.ReadInt32(),
                hashOffset = reader.ReadInt32()
            };
        }

        private VtxpImageEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new VtxpImageEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private VtxpImageEntry ReadEntry(BinaryReaderX reader)
        {
            return new VtxpImageEntry
            {
                nameOffset = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                paletteOffset = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                format = reader.ReadUInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                mipLevel = reader.ReadByte(),
                type = reader.ReadByte(),
                unk1 = reader.ReadInt16(),
                unk2 = reader.ReadInt32()
            };
        }

        private void WriteHeader(VtxpHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.imgCount);
            writer.Write(header.hashOffset);
        }

        private void WriteEntries(IList<VtxpImageEntry> entries, BinaryWriterX writer)
        {
            foreach (VtxpImageEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(VtxpImageEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.nameOffset);
            writer.Write(entry.dataSize);
            writer.Write(entry.paletteOffset);
            writer.Write(entry.dataOffset);
            writer.Write(entry.format);
            writer.Write(entry.width);
            writer.Write(entry.height);
            writer.Write(entry.mipLevel);
            writer.Write(entry.type);
            writer.Write(entry.unk1);
            writer.Write(entry.unk2);
        }
    }
}
