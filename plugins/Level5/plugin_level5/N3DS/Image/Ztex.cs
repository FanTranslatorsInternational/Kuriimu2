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
        private const int EntrySize_ = 28;
        private const int UnkEntrySize_ = 8;

        private ZtexHeader _header;
        private IList<ZtexUnkEnrty> _unkEntries;

        public IList<ImageFileInfo> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input);

            // Read header
            _header = typeReader.Read<ZtexHeader>(br);

            var unkCount = 0;
            if (_header.HasUnknownEntries)
                unkCount = br.ReadInt32();

            // Read entries
            var entries = new ZtexEntry[_header.imageCount];
            for (var i = 0; i < _header.imageCount; i++)
            {
                if (_header.HasExtendedEntries)
                    input.Position += 4;
                entries[i] = typeReader.Read<ZtexEntry>(br);
            }

            // Read unknown entries
            if (_header.HasUnknownEntries)
                _unkEntries = typeReader.ReadMany<ZtexUnkEnrty>(br, unkCount);

            // Add images
            var result = new List<ImageFileInfo>();
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

            var typeWriter = new BinaryTypeWriter();
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
                    name = imageInfo.Name,
                    crc32 = crc32.ComputeValue(imageInfo.Name),
                    offset = dataPosition,
                    dataSize = (int)(output.Position - dataPosition),
                    width = (short)SizePadding.PowerOfTwo(imageInfo.ImageSize.Width),
                    height = (short)SizePadding.PowerOfTwo(imageInfo.ImageSize.Height),
                    mipCount = (byte)((imageInfo.MipMapData?.Count ?? 0) + 1),
                    format = (byte)imageInfo.ImageFormat
                });

                dataPosition = (int)((output.Position + 0x7F) & ~0x7F);
            }

            // Write entries
            output.Position = entryOffset;
            foreach (var entry in entries)
            {
                if (_header.HasExtendedEntries)
                    bw.Write(0);
                typeWriter.Write(entry, bw);
            }

            if (_header.HasUnknownEntries)
                typeWriter.WriteMany(_unkEntries, bw);

            // Write header
            _header.imageCount = (short)imageInfos.Count;

            output.Position = 0;
            typeWriter.Write(_header, bw);

            if (_header.HasUnknownEntries)
                bw.Write(_unkEntries.Count);
        }
    }
}
