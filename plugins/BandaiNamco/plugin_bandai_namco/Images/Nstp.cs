using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Kryptography.Checksum.Crc;
using SixLabors.ImageSharp;

namespace plugin_bandai_namco.Images
{
    class Nstp
    {
        private static readonly int HeaderSize = 0x20;
        private static readonly int EntrySize = 0x20;

        private NstpHeader _header;

        public List<NstpImageFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);
            br.SeekAlignment(0x20);

            // Read entries
            var entries = ReadEntries(br, _header.imgCount);

            // Read image infos
            var result = new List<NstpImageFile>();
            foreach (var entry in entries)
            {
                // Read name
                input.Position = entry.nameOffset;
                var name = br.ReadNullTerminatedString();

                // Read data
                input.Position = entry.dataOffset;
                var imgData = br.ReadBytes(entry.dataSize);

                var imageInfo = new NstpImageFile(new ImageFileInfo
                {
                    Name = name,
                    BitDepth = NstpSupport.GetEncodingDefinition().GetColorEncoding(entry.format)?.BitDepth ?? 0,
                    ImageData = imgData,
                    ImageFormat = entry.format,
                    ImageSize = new Size(entry.width, entry.height),
                }, NstpSupport.GetEncodingDefinition(), entry);

                switch (entry.swizzleMode)
                {
                    // swizzleMode == 0
                    // Linear swizzle

                    // Switch swizzle
                    case 1:
                        imageInfo.ImageInfo.RemapPixels = context => new NxSwizzle(context);
                        break;
                }

                result.Add(imageInfo);
            }

            return result;
        }

        public void Save(Stream output, IList<NstpImageFile> imageInfos)
        {
            var crc32 = Crc32.Crc32B;
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var stringOffset = entryOffset + imageInfos.Count * EntrySize;
            var hashOffset = (stringOffset + imageInfos.Sum(x => x.ImageInfo.Name.Length + 1) + 3) & ~3;
            var dataOffset = (hashOffset + imageInfos.Count * 8 + 0x7F) & ~0x7F;

            // Write image data
            var entries = new List<NstpImageEntry>();

            var stringPosition = stringOffset;
            var dataPosition = dataOffset;
            foreach (var imageInfo in imageInfos)
            {
                // Write data
                output.Position = dataPosition;
                bw.Write(imageInfo.ImageInfo.ImageData);

                // Add entry
                imageInfo.Entry.dataOffset = dataPosition;
                imageInfo.Entry.dataSize = imageInfo.ImageInfo.ImageData.Length;
                imageInfo.Entry.format = imageInfo.ImageInfo.ImageFormat;
                imageInfo.Entry.width = (short)imageInfo.ImageInfo.ImageSize.Width;
                imageInfo.Entry.height = (short)imageInfo.ImageInfo.ImageSize.Height;
                imageInfo.Entry.nameOffset = stringPosition;
                entries.Add(imageInfo.Entry);

                dataPosition += (imageInfo.ImageInfo.ImageData.Length + 0x3F) & ~0x3F;
                stringPosition += imageInfo.ImageInfo.Name.Length + 1;
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
            bw.WriteAlignment(0x20);
        }

        private NstpHeader ReadHeader(BinaryReaderX reader)
        {
            return new NstpHeader
            {
                magic = reader.ReadString(4),
                version = reader.ReadInt32(),
                imgCount = reader.ReadInt32(),
                hashOffset = reader.ReadInt32()
            };
        }

        private NstpImageEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new NstpImageEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private NstpImageEntry ReadEntry(BinaryReaderX reader)
        {
            return new NstpImageEntry
            {
                nameOffset = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                format = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                mipLevels = reader.ReadByte(),
                swizzleMode = reader.ReadByte(),
                unk3 = reader.ReadInt32(),
                unk4 = reader.ReadInt32()
            };
        }

        private void WriteHeader(NstpHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.imgCount);
            writer.Write(header.hashOffset);
        }

        private void WriteEntries(List<NstpImageEntry> entries, BinaryWriterX writer)
        {
            foreach (NstpImageEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(NstpImageEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.nameOffset);
            writer.Write(entry.dataSize);
            writer.Write(entry.dataOffset);
            writer.Write(entry.format);
            writer.Write(entry.width);
            writer.Write(entry.height);
            writer.Write(entry.mipLevels);
            writer.Write(entry.swizzleMode);
            writer.Write(entry.unk3);
            writer.Write(entry.unk4);
        }
    }
}
