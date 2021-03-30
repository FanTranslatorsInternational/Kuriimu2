using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_level5.Switch.Images
{
    class Nxtch
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(NxtchHeader));

        private NxtchHeader _header;
        private byte[] _unkData;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<NxtchHeader>();

            // Read mip offsets
            var mipOffsets = br.ReadMultiple<int>(_header.mipMapCount);

            // Read unknown data
            _unkData = br.ReadBytes(0x100 - (int)input.Position);

            // Read image data
            var baseOffset = input.Position;

            input.Position = baseOffset + mipOffsets[0];
            var dataSize = mipOffsets.Count > 1 ? mipOffsets[1] - mipOffsets[0] : input.Length - baseOffset;
            var imageData = br.ReadBytes((int)dataSize);

            // Read mip data
            var mipData = new List<byte[]>();
            for (var i = 1; i < _header.mipMapCount; i++)
            {
                input.Position = mipOffsets[i];
                var mipSize = i + 1 >= _header.mipMapCount ? input.Length - baseOffset : mipOffsets[i + 1] - mipOffsets[i];
                mipData.Add(br.ReadBytes((int)mipSize));
            }

            // Create image info
            var imageInfo = new ImageInfo(imageData, _header.format, new Size(_header.width, _header.height))
            {
                MipMapData = mipData
            };

            imageInfo.PadSize.Height.ToPowerOfTwo().Width.ToMultiple(16);
            imageInfo.RemapPixels.With(context => new NxSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            var mipOffset = HeaderSize;
            var dataOffset = 0x100;

            // Write image and mip data
            var mipOffsets = new List<int> { dataOffset };

            var dataPosition = dataOffset;
            output.Position = dataPosition;
            bw.Write(imageInfo.ImageData);
            dataPosition += imageInfo.ImageData.Length;

            if (imageInfo.MipMapCount > 0)
                foreach (var mipData in imageInfo.MipMapData)
                {
                    mipOffsets.Add(dataPosition);
                    bw.Write(mipData);
                    dataPosition += mipData.Length;
                }

            // Write mip offsets
            output.Position = mipOffset;
            bw.WriteMultiple(mipOffsets.Select(x => x - dataOffset));

            // Write unknown data
            bw.Write(_unkData);

            // Write header
            _header.mipMapCount = imageInfo.MipMapCount;
            _header.format = imageInfo.ImageFormat;
            _header.width = imageInfo.ImageSize.Width;
            _header.height = imageInfo.ImageSize.Height;
            _header.textureDataSize = (int)(output.Length - dataOffset);
            _header.textureDataSize2 = (int)(output.Length - dataOffset);

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
