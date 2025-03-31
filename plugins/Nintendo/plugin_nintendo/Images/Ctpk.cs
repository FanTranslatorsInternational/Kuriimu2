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
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, Encoding.GetEncoding("Shift-JIS"));

            // Read header
            _header = typeReader.Read<CtpkHeader>(br);

            // Read tex entries
            br.BaseStream.Position = 0x20;
            var texEntries = typeReader.ReadMany<TexEntry>(br, _header.texCount);

            // Read data sizes
            var dataSizes = new int[_header.texCount][];
            for (var i = 0; i < _header.texCount; i++)
                dataSizes[i] = typeReader.ReadMany<int>(br, texEntries[i].mipLvl).ToArray();

            // Read names
            var names = new string[_header.texCount];
            for (var i = 0; i < _header.texCount; i++)
                names[i] = br.ReadNullTerminatedString();

            // Read hash entries
            br.BaseStream.Position = _header.crc32SecOffset;
            var hashEntries = typeReader.ReadMany<HashEntry>(br, _header.texCount).OrderBy(x => x.id).ToArray();

            // Read mip map infos
            br.BaseStream.Position = _header.texInfoOffset;
            var mipMapEntries = typeReader.ReadMany<MipmapEntry>(br, _header.texCount);

            // Add images
            var result = new List<ImageFileInfo>(_header.texCount);
            for (var i = 0; i < _header.texCount; i++)
            {
                // Read image data
                br.BaseStream.Position = _header.texSecOffset + texEntries[i].texOffset;
                var imageData = br.ReadBytes(texEntries[i].texDataSize);

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

            var typeWriter = new BinaryTypeWriter();
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
                    texDataSize = info.ImageData.Length,
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
            typeWriter.WriteMany(texEntries, bw);

            // Write data sizes
            output.Position = dataSizeOffset;
            foreach (var info in images)
            {
                bw.Write(info.ImageData.Length);
                typeWriter.WriteMany(info.MipMapData.Select(x => x.Length), bw);
            }

            // Write names
            output.Position = namesOffset;
            foreach (var info in images)
                bw.WriteString(info.Name, Encoding.GetEncoding("Shift-JIS"));

            // Write hash entries
            output.Position = hashEntryOffset;
            typeWriter.WriteMany(hashEntries.OrderBy(x => x.crc32), bw);

            // Write mip entries
            output.Position = mipEntriesOffset;
            typeWriter.WriteMany(mipEntries, bw);

            // Write header
            _header.texCount = (short)images.Count;
            _header.crc32SecOffset = hashEntryOffset;
            _header.texInfoOffset = mipEntriesOffset;
            _header.texSecOffset = dataOffset;
            _header.texSecSize = (int)(output.Length - dataOffset);

            output.Position = 0;
            typeWriter.Write(_header, bw);
        }
    }
}
