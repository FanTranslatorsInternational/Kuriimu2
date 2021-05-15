using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_level5._3DS.Images
{
    class Aif
    {
        private IList<MainSection> _mainSections;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read sections
            _mainSections = new List<MainSection>();
            do
            {
                _mainSections.Add(MainSection.Read(br));
            } while (_mainSections.Last().Header.nextSectionOffset != 0);

            // Read image info
            var imgSection = _mainSections.First(x => x.Header.magic == " FIA").GetSection("Xgmi");

            var format = imgSection.Data[0x10];
            var width = BinaryPrimitives.ReadInt16LittleEndian(imgSection.Data[0x18..0x1A]);
            var height = BinaryPrimitives.ReadInt16LittleEndian(imgSection.Data[0x1A..0x1C]);
            var dataSize = BinaryPrimitives.ReadInt32LittleEndian(imgSection.Data[0x2C..0x30]);

            // Create image info
            var imageData = _mainSections.First(x => x.Header.magic == " FMA").Data[..dataSize];
            var imageInfo = new ImageInfo(imageData, format, new Size(width, height));
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context, CtrTransformation.YFlip));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            // Update information
            var imgSection = _mainSections.First(x => x.Header.magic == " FIA").GetSection("Xgmi");

            imgSection.Data[0x10] = (byte)imageInfo.ImageFormat;
            BinaryPrimitives.WriteInt16LittleEndian(imgSection.Data[0x18..0x1A], (short)imageInfo.ImageSize.Width);
            BinaryPrimitives.WriteInt16LittleEndian(imgSection.Data[0x1A..0x1C], (short)imageInfo.ImageSize.Height);
            BinaryPrimitives.WriteInt32LittleEndian(imgSection.Data[0x2C..0x30], imageInfo.ImageData.Length);

            var buffSection = _mainSections.First(x => x.Header.magic == " FMA").GetSection("ffub");
            BinaryPrimitives.WriteInt32LittleEndian(buffSection.Data[..0x04], imageInfo.ImageData.Length);

            var addrSection = _mainSections.First(x => x.Header.magic == " FMA").GetSection("rdda");
            BinaryPrimitives.WriteInt32LittleEndian(addrSection.Data[0x10..0x14], imageInfo.ImageData.Length);

            // Update image data
            _mainSections.First(x => x.Header.magic == " FMA").Data = imageInfo.ImageData;

            // Write sections
            using var bw = new BinaryWriterX(output);

            for (var i = 0; i < _mainSections.Count; i++)
            {
                var mainSection = _mainSections[i];

                var nextSectionOffset = 0;
                if (i + 1 != _mainSections.Count)
                    nextSectionOffset = mainSection.GetLength();
                mainSection.Header.nextSectionOffset = nextSectionOffset;

                mainSection.Write(bw);
            }
        }
    }
}
