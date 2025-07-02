using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_sony.Images
{
    // TODO: index/palette data write

    /*https://docs.vitasdk.org/group__SceGxtUser.html*/
    /*https://github.com/xdanieldzd/Scarlet/blob/8d9e9cd34f6563da4a0f9b8797c3a1dd35542a4c/Scarlet/Platform/Sony/PSVita.cs*/
    public class Gxt
    {
        private static readonly int HeaderSize = 0x20;
        private const int EntrySize_ = 0x20;
        private const int P8PaletteSize_ = 256 * 4;
        private const int P4PaletteSize_ = 16 * 4;

        private GxtFile _fileDesc;

        public IList<ImageFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Parse file description
            _fileDesc = ReadFile(br);

            var p8PaletteOffset = _fileDesc.header.dataOffset + _fileDesc.header.dataSize -
                                  _fileDesc.header.p8PalCount * P8PaletteSize_;
            var p4PaletteOffset = p8PaletteOffset - _fileDesc.header.p4PalCount * P4PaletteSize_;

            // Create image infos
            var result = new List<ImageFileInfo>();
            foreach (var entry in _fileDesc.entries)
            {
                input.Position = entry.DataOffset;
                var imageData = br.ReadBytes(entry.DataSize);

                var imageInfo = new ImageFileInfo
                {
                    BitDepth = !GxtSupport.Formats.TryGetValue((uint)entry.Format, out var encoding)
                        ? GxtSupport.IndexFormats[(uint)entry.Format].BitDepth
                        : encoding.BitDepth,
                    ImageData = imageData,
                    ImageFormat = entry.Format,
                    ImageSize = new Size(entry.Width, entry.Height)
                };

                // Apply correct swizzle
                switch ((uint)entry.Type)
                {
                    case 0x60000000:    // Linear
                        break;

                    case 0x00000000:    // Vita swizzle
                    case 0x40000000:
                        imageInfo.RemapPixels = context => new VitaSwizzle(context);
                        break;

                    case 0x80000000:
                        imageInfo.RemapPixels = context => new CtrSwizzle(context);
                        break;
                }

                // Add palette data if necessary
                if ((uint)entry.Format == 0x95000000)   // I8 palette
                {
                    input.Position = p8PaletteOffset + entry.PaletteIndex * P8PaletteSize_;

                    imageInfo.PaletteData = br.ReadBytes(P8PaletteSize_);
                    imageInfo.PaletteFormat = entry.SubFormat;
                }

                if ((uint)entry.Format == 0x94000000)   // I4 palette
                {
                    input.Position = p4PaletteOffset + entry.PaletteIndex * P4PaletteSize_;

                    imageInfo.PaletteData = br.ReadBytes(P4PaletteSize_);
                    imageInfo.PaletteFormat = entry.SubFormat;
                }

                result.Add(imageInfo);
            }

            return result;
        }

        public void Save(Stream output, IList<ImageFileInfo> imageInfos)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var dataOffset = entryOffset + imageInfos.Count * EntrySize_;

            // Write image data
            var dataPosition = dataOffset;
            var p4Index = 0;
            var p8Index = 0;

            output.Position = dataOffset;
            for (var i = 0; i < imageInfos.Count; i++)
            {
                var entry = _fileDesc.entries[i];
                var imageInfo = imageInfos[i];

                // Update entry
                entry.DataOffset = dataPosition;
                entry.DataSize = imageInfo.ImageData.Length;
                entry.Format = imageInfo.ImageFormat;
                entry.Width = imageInfo.ImageSize.Width;
                entry.Height = imageInfo.ImageSize.Height;
                entry.PaletteIndex = -1;

                if ((uint)imageInfo.ImageFormat == 0x94000000 || (uint)imageInfo.ImageFormat == 0x95000000)
                    entry.SubFormat = imageInfo.PaletteFormat;

                if ((uint)imageInfo.ImageFormat == 0x94000000)
                    entry.PaletteIndex = p4Index++;
                if ((uint)imageInfo.ImageFormat == 0x95000000)
                    entry.PaletteIndex = p8Index++;

                // Write image data
                bw.Write(imageInfo.ImageData);

                dataPosition += imageInfo.ImageData.Length;
            }

            // Write palette data
            foreach (var imageInfo in imageInfos)
            {
                if ((uint)imageInfo.ImageFormat == 0x94000000)
                {
                    bw.Write(imageInfo.PaletteData);
                    bw.WritePadding(P4PaletteSize_ - imageInfo.PaletteData.Length);
                }
            }

            foreach (var imageInfo in imageInfos)
            {
                if ((uint)imageInfo.ImageFormat == 0x95000000)
                {
                    bw.Write(imageInfo.PaletteData);
                    bw.WritePadding(P8PaletteSize_ - imageInfo.PaletteData.Length);
                }
            }

            // Write file description
            _fileDesc.header.dataOffset = dataOffset;
            _fileDesc.header.dataSize = (int)(output.Length - dataOffset);
            _fileDesc.header.texCount = imageInfos.Count;
            _fileDesc.header.p4PalCount = p4Index;
            _fileDesc.header.p8PalCount = p8Index;

            output.Position = 0;
            WriteFile(_fileDesc, bw);
        }

        private GxtFile ReadFile(BinaryReaderX reader)
        {
            var file = new GxtFile
            {
                header = ReadHeader(reader),
            };

            file.entries = ReadEntries(reader, file.header.texCount, file.header.version);

            return file;
        }

        private GxtHeader ReadHeader(BinaryReaderX reader)
        {
            return new GxtHeader
            {
                magic = reader.ReadString(4),
                version = reader.ReadUInt32(),
                texCount = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                p4PalCount = reader.ReadInt32(),
                p8PalCount = reader.ReadInt32(),
                zero0 = reader.ReadInt32()
            };
        }

        private IGxtEntry[] ReadEntries(BinaryReaderX reader, int count, uint version)
        {
            var result = new IGxtEntry[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = version switch
                {
                    0x10000001 => ReadEntry1(reader),
                    0x10000002 => ReadEntry2(reader),
                    0x10000003 => ReadEntry3(reader),
                    _ => throw new ArgumentOutOfRangeException($"Unsupported GXT file version {version}.")
                };
            }

            return result;
        }

        private IGxtEntry ReadEntry1(BinaryReaderX reader)
        {
            return new GxtEntry1
            {
                dataOffset = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                paletteIndex = reader.ReadInt32(),
                flags = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                tmp1 = reader.ReadInt32(),
                type = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private IGxtEntry ReadEntry2(BinaryReaderX reader)
        {
            return new GxtEntry2
            {
                dataOffset = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                paletteIndex = reader.ReadInt32(),
                flags = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                tmp1 = reader.ReadInt32(),
                type = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private IGxtEntry ReadEntry3(BinaryReaderX reader)
        {
            return new GxtEntry3
            {
                dataOffset = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                paletteIndex = reader.ReadInt32(),
                flags = reader.ReadInt32(),
                type = reader.ReadInt32(),
                format = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                mipCount = reader.ReadByte(),
                padding = reader.ReadBytes(3)
            };
        }

        private void WriteFile(GxtFile file, BinaryWriterX writer)
        {
            WriteHeader(file.header, writer);
            WriteEntries(file.entries, file.header.version, writer);
        }

        private void WriteHeader(GxtHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.texCount);
            writer.Write(header.dataOffset);
            writer.Write(header.dataSize);
            writer.Write(header.p4PalCount);
            writer.Write(header.p8PalCount);
            writer.Write(header.zero0);
        }

        private void WriteEntries(IGxtEntry[] entries, uint version, BinaryWriterX writer)
        {
            foreach (IGxtEntry entry in entries)
            {
                switch (version)
                {
                    case 0x10000001:
                        WriteEntry1((GxtEntry1)entry, writer);
                        break;

                    case 0x10000002:
                        WriteEntry2((GxtEntry2)entry, writer);
                        break;

                    case 0x10000003:
                        WriteEntry3((GxtEntry3)entry, writer);
                        break;
                }
            }
        }

        private void WriteEntry1(GxtEntry1 entry, BinaryWriterX writer)
        {
            writer.Write(entry.dataOffset);
            writer.Write(entry.dataSize);
            writer.Write(entry.paletteIndex);
            writer.Write(entry.flags);
            writer.Write(entry.unk1);
            writer.Write(entry.tmp1);
            writer.Write(entry.type);
            writer.Write(entry.unk2);
        }

        private void WriteEntry2(GxtEntry2 entry, BinaryWriterX writer)
        {
            writer.Write(entry.dataOffset);
            writer.Write(entry.dataSize);
            writer.Write(entry.paletteIndex);
            writer.Write(entry.flags);
            writer.Write(entry.unk1);
            writer.Write(entry.tmp1);
            writer.Write(entry.type);
            writer.Write(entry.unk2);
        }

        private void WriteEntry3(GxtEntry3 entry, BinaryWriterX writer)
        {
            writer.Write(entry.dataOffset);
            writer.Write(entry.dataSize);
            writer.Write(entry.paletteIndex);
            writer.Write(entry.flags);
            writer.Write(entry.type);
            writer.Write(entry.format);
            writer.Write(entry.width);
            writer.Write(entry.height);
            writer.Write(entry.mipCount);
            writer.Write(entry.padding);
        }
    }
}
