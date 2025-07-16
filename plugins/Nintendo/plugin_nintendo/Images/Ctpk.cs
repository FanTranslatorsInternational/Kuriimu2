using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Kryptography.Checksum.Crc;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class Ctpk
    {
        private const int TexEntrySize_ = 0x20;
        private const int HashEntrySize_ = 0x8;
        private const int MipMapEntrySize_ = 0x4;

        private CtpkHeader _header;

        public List<ImageFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, Encoding.GetEncoding("Shift-JIS"));

            // Read header
            _header = ReadHeader(br);

            // Read tex entries
            br.BaseStream.Position = 0x20;
            var texEntries = ReadTexEntries(br, _header.texCount);

            // Read data sizes
            var dataSizes = new int[_header.texCount][];
            for (var i = 0; i < _header.texCount; i++)
                dataSizes[i] = ReadIntegers(br, texEntries[i].mipLvl);

            // Read names
            var names = new string[_header.texCount];
            for (var i = 0; i < _header.texCount; i++)
                names[i] = br.ReadNullTerminatedString();

            // Read hash entries
            br.BaseStream.Position = _header.crc32SecOffset;
            var hashEntries = ReadHashEntries(br, _header.texCount).OrderBy(x => x.id).ToArray();

            // Read mip map infos
            br.BaseStream.Position = _header.texInfoOffset;
            var mipMapEntries = ReadMipEntries(br, _header.texCount);

            // Add images
            var result = new List<ImageFileInfo>(_header.texCount);
            for (var i = 0; i < _header.texCount; i++)
            {
                // Read image data
                br.BaseStream.Position = _header.texSecOffset + texEntries[i].texOffset;
                var imageData = br.ReadBytes(dataSizes[i][0]);

                // Read mip maps
                var mipMaps = Enumerable.Range(1, texEntries[i].mipLvl - 1)
                    .Select(x => br.ReadBytes(dataSizes[i][x]))
                    .ToArray();

                result.Add(new CtpkImageFileInfo
                {
                    Name = names[i],
                    BitDepth = CtpkSupport.GetEncodingDefinitions().GetColorEncoding(texEntries[i].imageFormat).BitDepth,
                    ImageData = imageData,
                    ImageFormat = texEntries[i].imageFormat,
                    ImageSize = new Size(texEntries[i].width, texEntries[i].height),
                    MipMapData = mipMaps,
                    Entry = texEntries[i],
                    MipEntry = mipMapEntries[i],
                    RemapPixels = context => new CtrSwizzle(context),
                    PadSize = builder => builder.ToPowerOfTwo()
                });
            }

            return result;
        }

        public void Save(Stream output, List<ImageFileInfo> images)
        {
            var crc32 = Crc32.Crc32B;

            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var texEntryOffset = 0x20;
            var dataSizeOffset = texEntryOffset + images.Count * TexEntrySize_;
            var namesOffset = dataSizeOffset + images.Sum(x => (x.MipMapData?.Count ?? 0) + 1) * 4;
            var hashEntryOffset = namesOffset + ((images.Sum(x => Encoding.GetEncoding("SJIS").GetByteCount(x.Name) + 1) + 3) & ~3);
            var mipEntriesOffset = hashEntryOffset + images.Count * HashEntrySize_;
            var dataOffset = (mipEntriesOffset + images.Count * MipMapEntrySize_ + 0x7F) & ~0x7F;

            // Write tex data
            var namePosition = namesOffset;
            var texSecPosition = 0;
            var sizePosition = dataSizeOffset;
            var infoIndex = 0;

            var texEntries = new List<TexEntry>();
            var hashEntries = new List<HashEntry>();
            var mipEntries = new List<MipmapEntry>();
            foreach (var info in images.Cast<CtpkImageFileInfo>())
            {
                // Write data
                output.Position = dataOffset + texSecPosition;
                output.Write(info.ImageData);
                foreach (var mipData in info.MipMapData)
                    output.Write(mipData);

                // Add new tex entry
                texEntries.Add(new TexEntry
                {
                    width = (short)info.ImageSize.Width,
                    height = (short)info.ImageSize.Height,
                    imageFormat = info.ImageFormat,
                    mipLvl = (byte)((info.MipMapData?.Count ?? 0) + 1),
                    nameOffset = namePosition,
                    texDataSize = info.ImageData.Length + info.MipMapData.Sum(x => x.Length),
                    texOffset = texSecPosition,
                    timeStamp = info.Entry.timeStamp,
                    sizeOffset = sizePosition >> 2,
                    type = info.Entry.type
                });

                namePosition += Encoding.GetEncoding("SJIS").GetByteCount(info.Name) + 1;
                texSecPosition += info.ImageData.Length + info.MipMapData.Sum(x => x.Length);
                sizePosition += ((info.MipMapData?.Count ?? 0) + 1) * 4;

                // Add new hash entry
                hashEntries.Add(new HashEntry
                {
                    id = infoIndex++,
                    crc32 = crc32.ComputeValue(info.Name)
                });

                // Add new mip entry
                mipEntries.Add(new MipmapEntry
                {
                    mipLvl = (byte)((info.MipMapData?.Count ?? 0) + 1),
                    mipmapFormat = (byte)info.ImageFormat,
                    compression = info.MipEntry.compression,
                    compMethod = info.MipEntry.compMethod
                });
            }

            // Write tex entries
            output.Position = texEntryOffset;
            WriteTexEntries(texEntries, bw);

            // Write data sizes
            output.Position = dataSizeOffset;
            foreach (var info in images)
            {
                bw.Write(info.ImageData.Length);
                WriteIntegers(info.MipMapData.Select(x => x.Length).ToArray(), bw);
            }

            // Write names
            output.Position = namesOffset;
            foreach (var info in images)
                bw.WriteString(info.Name, Encoding.GetEncoding("Shift-JIS"));

            // Write hash entries
            output.Position = hashEntryOffset;
            WriteHashEntries(hashEntries.OrderBy(x => x.crc32).ToArray(), bw);

            // Write mip entries
            output.Position = mipEntriesOffset;
            WriteMipEntries(mipEntries, bw);

            // Write header
            _header.texCount = (short)images.Count;
            _header.crc32SecOffset = hashEntryOffset;
            _header.texInfoOffset = mipEntriesOffset;
            _header.texSecOffset = dataOffset;
            _header.texSecSize = (int)(output.Length - dataOffset);

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private CtpkHeader ReadHeader(BinaryReaderX reader)
        {
            return new CtpkHeader
            {
                magic = reader.ReadString(4),
                version = reader.ReadInt16(),
                texCount = reader.ReadInt16(),
                texSecOffset = reader.ReadInt32(),
                texSecSize = reader.ReadInt32(),
                crc32SecOffset = reader.ReadInt32(),
                texInfoOffset = reader.ReadInt32()
            };
        }

        private TexEntry[] ReadTexEntries(BinaryReaderX reader, int count)
        {
            var result = new TexEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadTexEntry(reader);

            return result;
        }

        private TexEntry ReadTexEntry(BinaryReaderX reader)
        {
            return new TexEntry
            {
                nameOffset = reader.ReadInt32(),
                texDataSize = reader.ReadInt32(),
                texOffset = reader.ReadInt32(),
                imageFormat = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                mipLvl = reader.ReadByte(),
                type = reader.ReadByte(),
                zero0 = reader.ReadInt16(),
                sizeOffset = reader.ReadInt32(),
                timeStamp = reader.ReadUInt32()
            };
        }

        private HashEntry[] ReadHashEntries(BinaryReaderX reader, int count)
        {
            var result = new HashEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadHashEntry(reader);

            return result;
        }

        private HashEntry ReadHashEntry(BinaryReaderX reader)
        {
            return new HashEntry
            {
                crc32 = reader.ReadUInt32(),
                id = reader.ReadInt32()
            };
        }

        private MipmapEntry[] ReadMipEntries(BinaryReaderX reader, int count)
        {
            var result = new MipmapEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadMipEntry(reader);

            return result;
        }

        private MipmapEntry ReadMipEntry(BinaryReaderX reader)
        {
            return new MipmapEntry
            {
                mipmapFormat = reader.ReadByte(),
                mipLvl = reader.ReadByte(),
                compression = reader.ReadByte(),
                compMethod = reader.ReadByte()
            };
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteHeader(CtpkHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.texCount);
            writer.Write(header.texSecOffset);
            writer.Write(header.texSecSize);
            writer.Write(header.crc32SecOffset);
            writer.Write(header.texInfoOffset);
        }

        private void WriteTexEntries(IList<TexEntry> entries, BinaryWriterX writer)
        {
            foreach (TexEntry entry in entries)
                WriteTexEntry(entry, writer);
        }

        private void WriteTexEntry(TexEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.nameOffset);
            writer.Write(entry.texDataSize);
            writer.Write(entry.texOffset);
            writer.Write(entry.imageFormat);
            writer.Write(entry.width);
            writer.Write(entry.height);
            writer.Write(entry.mipLvl);
            writer.Write(entry.type);
            writer.Write(entry.zero0);
            writer.Write(entry.sizeOffset);
            writer.Write(entry.timeStamp);
        }

        private void WriteHashEntries(IList<HashEntry> entries, BinaryWriterX writer)
        {
            foreach (HashEntry entry in entries)
                WriteHashEntry(entry, writer);
        }

        private void WriteHashEntry(HashEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.crc32);
            writer.Write(entry.id);
        }

        private void WriteMipEntries(IList<MipmapEntry> entries, BinaryWriterX writer)
        {
            foreach (MipmapEntry entry in entries)
                WriteMipEntry(entry, writer);
        }

        private void WriteMipEntry(MipmapEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.mipmapFormat);
            writer.Write(entry.mipLvl);
            writer.Write(entry.compression);
            writer.Write(entry.compMethod);
        }

        private void WriteIntegers(int[] entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }
    }
}
