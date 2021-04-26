using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;
using Kryptography.Hash.Crc;

namespace plugin_nintendo.Images
{
    class Ctpk
    {
        private static readonly int TexEntrySize = Tools.MeasureType(typeof(TexEntry));
        private static readonly int HashEntrySize = Tools.MeasureType(typeof(HashEntry));
        private static readonly int MipMapEntrySize = Tools.MeasureType(typeof(MipmapEntry));

        private CtpkHeader _header;

        public IList<ImageInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<CtpkHeader>();

            // Read tex entries
            br.BaseStream.Position = 0x20;
            var texEntries = br.ReadMultiple<TexEntry>(_header.texCount);

            // Read data sizes
            var dataSizes = new int[_header.texCount][];
            for (var i = 0; i < _header.texCount; i++)
                dataSizes[i] = br.ReadMultiple<int>(texEntries[i].mipLvl).ToArray();

            // Read names
            var names = new string[_header.texCount];
            for (var i = 0; i < _header.texCount; i++)
                names[i] = br.ReadCStringSJIS();

            // Read hash entries
            br.BaseStream.Position = _header.crc32SecOffset;
            var hashEntries = br.ReadMultiple<HashEntry>(_header.texCount).OrderBy(x => x.id).ToArray();

            // Read mip map infos
            br.BaseStream.Position = _header.texInfoOffset;
            var mipMapEntries = br.ReadMultiple<MipmapEntry>(_header.texCount);

            // Add images
            var result = new ImageInfo[_header.texCount];
            for (var i = 0; i < _header.texCount; i++)
            {
                // Read image data
                br.BaseStream.Position = _header.texSecOffset + texEntries[i].texOffset;
                var imageData = br.ReadBytes(texEntries[i].texDataSize);

                // Read mip maps
                var mipMaps = Enumerable.Range(1, texEntries[i].mipLvl - 1)
                    .Select(x => br.ReadBytes(dataSizes[i][x]))
                    .ToArray();

                result[i] = new CtpkImageInfo(imageData, mipMaps, texEntries[i].imageFormat, new Size(texEntries[i].width, texEntries[i].height), texEntries[i], mipMapEntries[i])
                {
                    Name = names[i]
                };
                result[i].RemapPixels.With(context => new CtrSwizzle(context));
                result[i].PadSize.ToPowerOfTwo();
            }

            return result;
        }

        public void Save(Stream output, IList<ImageInfo> images)
        {
            var crc32 = Crc32.Default;
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var texEntryOffset = 0x20;
            var dataSizeOffset = texEntryOffset + images.Count * TexEntrySize;
            var namesOffset = dataSizeOffset + images.Sum(x => x.MipMapCount + 1) * 4;
            var hashEntryOffset = namesOffset + ((images.Sum(x => Encoding.GetEncoding("SJIS").GetByteCount(x.Name) + 1) + 3) & ~3);
            var mipEntriesOffset = hashEntryOffset + images.Count * HashEntrySize;
            var dataOffset = (mipEntriesOffset + images.Count * MipMapEntrySize + 0x1F) & ~0x1F;

            // Write tex data
            var namePosition = namesOffset;
            var texSecPosition = 0;
            var sizePosition = dataSizeOffset;
            var infoIndex = 0;

            var texEntries = new List<TexEntry>();
            var hashEntries = new List<HashEntry>();
            var mipEntries = new List<MipmapEntry>();
            foreach (var info in images.Cast<CtpkImageInfo>())
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
                    mipLvl = (byte)(info.MipMapCount + 1),
                    nameOffset = namePosition,
                    texDataSize = info.ImageData.Length,
                    texOffset = texSecPosition,
                    timeStamp = info.Entry.timeStamp,
                    sizeOffset = sizePosition >> 2,
                    type = info.Entry.type
                });

                namePosition += Encoding.GetEncoding("SJIS").GetByteCount(info.Name) + 1;
                texSecPosition += info.ImageData.Length + info.MipMapData.Sum(x => x.Length);
                sizePosition += (info.MipMapCount + 1) * 4;

                // Add new hash entry
                hashEntries.Add(new HashEntry
                {
                    id = infoIndex++,
                    crc32 = crc32.ComputeValue(info.Name)
                });

                // Add new mip entry
                mipEntries.Add(new MipmapEntry
                {
                    mipLvl = (byte)(info.MipMapCount + 1),
                    mipmapFormat = (byte)info.ImageFormat,
                    compression = info.MipEntry.compression,
                    compMethod = info.MipEntry.compMethod
                });
            }

            // Write tex entries
            output.Position = texEntryOffset;
            bw.WriteMultiple(texEntries);

            // Write data sizes
            output.Position = dataSizeOffset;
            foreach (var info in images)
            {
                bw.Write(info.ImageData.Length);
                bw.WriteMultiple(info.MipMapData.Select(x => x.Length));
            }

            // Write names
            output.Position = namesOffset;
            foreach (var info in images)
                bw.WriteString(info.Name, Encoding.GetEncoding("SJIS"), false);

            // Write hash entries
            output.Position = hashEntryOffset;
            bw.WriteMultiple(hashEntries.OrderBy(x => x.crc32));

            // Write mip entries
            output.Position = mipEntriesOffset;
            bw.WriteMultiple(mipEntries);

            // Write header
            _header.texCount = (short)images.Count;
            _header.crc32SecOffset = hashEntryOffset;
            _header.texInfoOffset = mipEntriesOffset;
            _header.texSecOffset = dataOffset;
            _header.texSecSize = (int)(output.Length - dataOffset);

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
