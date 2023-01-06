using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Plugins.State.Image;
using Kryptography.Hash.Crc;

namespace plugin_level5._3DS.Images
{
    class Ztex
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(ZtexHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(ZtexEntry));
        private static readonly int UnkEntrySize = Tools.MeasureType(typeof(ZtexUnkEnrty));

        private ZtexHeader _header;
        private IList<ZtexUnkEnrty> _unkEntries;

        public IList<ImageData> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<ZtexHeader>();

            var unkCount = 0;
            if (_header.HasUnknownEntries)
                unkCount = br.ReadInt32();

            // Read entries
            var entries = new ZtexEntry[_header.imageCount];
            for (var i = 0; i < _header.imageCount; i++)
            {
                if (_header.HasExtendedEntries)
                    input.Position += 4;
                entries[i] = br.ReadType<ZtexEntry>();
            }

            // Read unknown entries
            if (_header.HasUnknownEntries)
                _unkEntries = br.ReadMultiple<ZtexUnkEnrty>(unkCount);

            // Add images
            var result = new List<ImageData>();
            foreach (var entry in entries)
            {
                // Read image data
                input.Position = entry.offset;
                var imgData = br.ReadBytes(entry.dataSize);

                // Read mip data
                var mipData = new List<byte[]>();
                for (var i = 1; i < entry.mipCount; i++)
                    mipData.Add(br.ReadBytes(entry.dataSize >> (i * 2)));

                // Create image info
                var imgInfo = new ImageData(imgData, mipData, entry.format, new Size(entry.width, entry.height))
                {
                    Name = entry.name.Trim('\0')
                };
                imgInfo.RemapPixels.With(context => new CtrSwizzle(context));
                imgInfo.PadSize.ToPowerOfTwo();

                result.Add(imgInfo);
            }

            return result;
        }

        public void Save(Stream output, IList<ImageData> imageInfos)
        {
            var crc32 = Crc32.Crc32B;
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize + (_header.HasUnknownEntries ? 4 : 0);
            var unkOffset = entryOffset + imageInfos.Count * EntrySize + (_header.HasExtendedEntries ? imageInfos.Count * 4 : 0);
            var dataOffset = (unkOffset + (_header.HasUnknownEntries ? _unkEntries.Count * UnkEntrySize : 0) + 0x7F) & ~0x7F;

            // Write image data
            var entries = new List<ZtexEntry>();

            var dataPosition = dataOffset;
            foreach (var imageInfo in imageInfos)
            {
                // Write image data
                output.Position = dataPosition;
                bw.Write(imageInfo.Data);

                foreach (var mipData in imageInfo.MipMapData)
                    bw.Write(mipData);

                // Create entry
                var paddedSize = imageInfo.PadSize.Build(imageInfo.ImageSize);
                entries.Add(new ZtexEntry
                {
                    name = imageInfo.Name,
                    crc32 = crc32.ComputeValue(imageInfo.Name),
                    offset = dataPosition,
                    dataSize = (int)(output.Position - dataPosition),
                    width = (short)paddedSize.Width,
                    height = (short)paddedSize.Height,
                    mipCount = (byte)(imageInfo.MipMapCount + 1),
                    format = (byte)imageInfo.Format
                });

                dataPosition = (int)((output.Position + 0x7F) & ~0x7F);
            }

            // Write entries
            output.Position = entryOffset;
            foreach (var entry in entries)
            {
                if (_header.HasExtendedEntries)
                    bw.Write(0);
                bw.WriteType(entry);
            }

            if (_header.HasUnknownEntries)
                bw.WriteMultiple(_unkEntries);

            // Write header
            _header.imageCount = (short)imageInfos.Count;

            output.Position = 0;
            bw.WriteType(_header);

            if (_header.HasUnknownEntries)
                bw.Write(_unkEntries.Count);
        }
    }
}
