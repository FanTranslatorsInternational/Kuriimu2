using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_gust.Images
{
    class G1t
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(G1tHeader));

        private G1tPlatform _platform;

        private G1tHeader _header;
        private IList<int> _unkRegion;

        public IList<ImageInfo> Load(Stream input, G1tPlatform platform)
        {
            _platform = platform;

            using var br = new BinaryReaderX(input);

            // Set endianess
            var magic = br.ReadString(4);
            br.ByteOrder = magic == "GT1G" ? ByteOrder.LittleEndian : ByteOrder.BigEndian;

            // Read header
            input.Position = 0;
            _header = br.ReadType<G1tHeader>();

            // Read unknown region
            _unkRegion = br.ReadMultiple<int>(_header.texCount);

            // Read offsets
            input.Position = _header.dataOffset;
            var offsets = br.ReadMultiple<int>(_header.texCount);

            // Read images
            var result = new List<ImageInfo>();
            foreach (var offset in offsets)
            {
                // Read entry
                input.Position = _header.dataOffset + offset;
                var entry = br.ReadType<G1tEntry>();

                // Read image data
                var dataSize = entry.Width * entry.Height * G1tSupport.GetBitDepth(entry.format, platform) / 8;
                var imageData = br.ReadBytes(dataSize);

                // Read mips
                var mips = new List<byte[]>();
                for (var i = 1; i < entry.MipCount; i++)
                {
                    dataSize = (entry.Width >> i) * (entry.Height >> i) * G1tSupport.GetBitDepth(entry.format, platform) / 8;
                    mips.Add(br.ReadBytes(dataSize));
                }

                // Create image info
                var imageInfo = new G1tImageInfo(imageData, entry.format, new Size(entry.Width, entry.Height), entry)
                {
                    MipMapData = mips
                };

                imageInfo.RemapPixels.With(context => G1tSupport.GetSwizzle(context, entry.format, platform));
                imageInfo.PadSize.ToPowerOfTwo();

                result.Add(imageInfo);
            }

            return result;
        }

        public void Save(Stream output, IList<ImageInfo> imageInfos)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var unkRegionOffset = HeaderSize;
            var offsetsOffset = unkRegionOffset + imageInfos.Count * 4;
            var dataOffset = offsetsOffset + imageInfos.Count * 4;

            // Write image data
            var offsets = new List<int>();

            output.Position = dataOffset;
            foreach (var imageInfo in imageInfos.Cast<G1tImageInfo>())
            {
                offsets.Add((int)(output.Position - offsetsOffset));

                // Update entry
                imageInfo.Entry.Width = imageInfo.ImageSize.Width;
                imageInfo.Entry.Height = imageInfo.ImageSize.Height;
                imageInfo.Entry.format = (byte)imageInfo.ImageFormat;

                // Write entry
                bw.WriteType(imageInfo.Entry);

                // Write image data
                bw.Write(imageInfo.ImageData);

                // Write mips
                if (imageInfo.MipMapCount > 0)
                    foreach (var mip in imageInfo.MipMapData)
                        bw.Write(mip);
            }

            // Write offsets
            output.Position = offsetsOffset;
            bw.WriteMultiple(offsets);

            // Write unknown region
            output.Position = unkRegionOffset;
            bw.WriteMultiple(_unkRegion);

            // Write header
            _header.dataOffset = offsetsOffset;
            _header.texCount = imageInfos.Count;
            _header.fileSize = (int)output.Length;

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
