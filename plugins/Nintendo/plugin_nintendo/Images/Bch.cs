using System;
using System.Collections.Generic;
using System.IO;
using Kanvas.Configuration;
using Kanvas.Swizzle;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Image;
using plugin_nintendo.Images.PICA;

namespace plugin_nintendo.Images
{
    class Bch
    {
        private Stream _file;

        private BchHeader _header;
        private IList<PICACommandReader> _picaReaders;

        public IList<ImageInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(_file = input, true);

            // Read header
            _header = br.ReadType<BchHeader>();

            if (_header.dataSize == 0)
                return Array.Empty<ImageInfo>();

            // Read PICA commands
            _picaReaders = new List<PICACommandReader>();
            var gpuStream = new SubStream(input, _header.gpuCommandsOffset, _header.gpuCommandsSize);
            while (gpuStream.Position < gpuStream.Length)
                _picaReaders.Add(new PICACommandReader(gpuStream));

            // Add images
            var result = new List<ImageInfo>();
            foreach (var picaReader in _picaReaders)
            {
                var size = picaReader.getTexUnit0Size();
                if (size.Width == 0 || size.Height == 0)
                    continue;

                // Read main image
                var format = (int)picaReader.getTexUnit0Format();
                var bitDepth = BchSupport.GetEncodingDefinition().GetColorEncoding(format).BitDepth;

                input.Position = _header.dataOffset + picaReader.getTexUnit0Address();

                var dataLength = size.Width * size.Height * bitDepth / 8;
                var imageData = br.ReadBytes(dataLength);

                // Read mip maps
                var mipCount = picaReader.getTexUnit0LoD();
                var mipMaps = new byte[mipCount][];
                for (var i = 1; i <= mipCount; i++)
                    mipMaps[i - 1] = br.ReadBytes((size.Width >> i) * (size.Height >> i) * bitDepth / 8);

                result.Add(mipCount > 0
                    ? new ImageInfo(imageData, mipMaps, format, size)
                    : new ImageInfo(imageData, format, size));
                result[^1].Configuration = new ImageConfiguration().RemapPixelsWith(size => new CTRSwizzle(size.Width, size.Height));
            }

            return result;
        }

        public void Save(Stream output, IList<ImageInfo> images)
        {
            // Copy original file into output
            _file.Position = 0;
            _file.CopyTo(output);

            // Write new images
            //   They are locked, so they don't have any size related changes and can be placed without further manipulations
            var imageIndex = 0;
            foreach (var picaReader in _picaReaders)
            {
                var size = picaReader.getTexUnit0Size();
                if (size.Width == 0 || size.Height == 0)
                    continue;

                // Write main image
                output.Position = _header.dataOffset + picaReader.getTexUnit0Address();
                output.Write(images[imageIndex].ImageData);

                // Write mip levels
                var mipCount = picaReader.getTexUnit0LoD();
                for (var i = 0; i < mipCount; i++)
                    output.Write(images[imageIndex].MipMapData[i]);

                imageIndex++;
            }
        }
    }
}
