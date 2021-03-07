using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_mt_framework.Images
{
    // TODO: Platofrm.Mobile may only expose one image; For 0x0C expose DXT5 image and internally use KanvasImage for transcoding to ATC and PVRTC
    class MtTex
    {
        private const int HeaderSize_ = 0x10;

        private MtTexPlatform _platform;
        private MtTexHeader _header;
        private MobileMtTexHeader _mobileHeader;

        public IList<ImageInfo> Load(Stream input, MtTexPlatform platform)
        {
            _platform = platform;
            using var br = new BinaryReaderX(input);

            // Determine byte order
            if (br.PeekString() == "\0XET")
                br.ByteOrder = ByteOrder.BigEndian;

            // Read header
            _header = br.ReadType<MtTexHeader>();

            input.Position = 0;
            _mobileHeader = br.ReadType<MobileMtTexHeader>();

            switch (platform)
            {
                case MtTexPlatform.N3DS:
                    return new[] { Load3ds(br) };

                case MtTexPlatform.PS3:
                    return new[] { LoadPs3(br) };

                case MtTexPlatform.Switch:
                    return new[] { LoadSwitch(br) };

                case MtTexPlatform.Mobile:
                    return LoadMobile(br);

                default:
                    throw new InvalidOperationException();
            }
        }

        public void Save(Stream output, IList<ImageInfo> imageInfos)
        {
            var byteOrder = _header.magic == "\0XET" ? ByteOrder.BigEndian : ByteOrder.LittleEndian;
            using var bw = new BinaryWriterX(output, byteOrder);

            switch (_platform)
            {
                case MtTexPlatform.N3DS:
                    Save3ds(bw, imageInfos[0]);
                    break;

                case MtTexPlatform.PS3:
                    SavePs3(bw, imageInfos[0]);
                    break;

                case MtTexPlatform.Switch:
                    SaveSwitch(bw, imageInfos[0]);
                    break;

                case MtTexPlatform.Mobile:
                    SaveMobile(bw, imageInfos);
                    break;
            }
        }

        #region Load

        private ImageInfo Load3ds(BinaryReaderX br)
        {
            // Skip mip offsets
            IList<int> mipOffsets = Array.Empty<int>();
            if (_header.version != 0xA4)
                mipOffsets = br.ReadMultiple<int>(_header.mipCount);

            // Read image data
            var bitDepth = MtTexSupport.CtrFormats[_header.format].BitDepth;
            var dataSize = _header.width * _header.height * bitDepth / 8;

            if (_header.version != 0xA4) br.BaseStream.Position = HeaderSize_ + _header.mipCount * 4 + mipOffsets[0];
            var imageData = br.ReadBytes(dataSize);

            // Read mips
            var mipData = new List<byte[]>();
            for (var i = 1; i < _header.mipCount; i++)
            {
                var mipSize = (_header.width >> i) * (_header.height >> i) * bitDepth / 8;

                if (_header.version != 0xA4) br.BaseStream.Position = HeaderSize_ + _header.mipCount * 4 + mipOffsets[i];
                mipData.Add(br.ReadBytes(mipSize));
            }

            // Create image info
            var imageInfo = new ImageInfo(imageData, _header.format, new Size(_header.width, _header.height));

            if (_header.mipCount > 1)
                imageInfo.MipMapData = mipData;

            imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

            return imageInfo;
        }

        private ImageInfo LoadPs3(BinaryReaderX br)
        {
            // Skip mip offsets
            var mipOffsets = br.ReadMultiple<int>(_header.mipCount);

            // Read image data
            var bitDepth = MtTexSupport.Ps3Formats[_header.format].BitDepth;
            var colorsPerValue = MtTexSupport.Ps3Formats[_header.format].ColorsPerValue;
            var dataSize = _header.width * _header.height * bitDepth / 8;

            br.BaseStream.Position = mipOffsets[0];
            var imageData = br.ReadBytes(dataSize);

            // Read mips
            var mipData = new List<byte[]>();
            for (var i = 1; i < _header.mipCount; i++)
            {
                var mipSize = (_header.width >> i) * (_header.height >> i) * bitDepth / 8;

                br.BaseStream.Position = mipOffsets[i];
                mipData.Add(br.ReadBytes(mipSize));
            }

            // Create image info
            var imageInfo = new ImageInfo(imageData, _header.format, new Size(_header.width, _header.height));

            if (_header.mipCount > 1)
                imageInfo.MipMapData = mipData;

            // TODO: Remove block swizzle with pre-swizzle implementation in Kanvas
            if (colorsPerValue > 1)
                imageInfo.RemapPixels.With(context => new BcSwizzle(context));

            return imageInfo;
        }

        private ImageInfo LoadSwitch(BinaryReaderX br)
        {
            // TODO: Find samples for this behaviour
            // Skip unknown data between header and mipOffsets
            var texSize = br.ReadUInt32();
            //if (texSize > br.BaseStream.Length)
            //{
            //    br.BaseStream.Position -= 4;
            //    _switchUnkData1 = br.ReadBytes(0x6C);

            //    texSize = br.ReadUInt32();
            //}

            // Skip mip offsets
            br.ReadMultiple<int>(_header.mipCount);

            // Read image data
            // HINT: Calculating dataSize by bitsPerValue and colorsPerValue, since bitDepth can be 0 or some float due to ASTC
            var bitsPerValue = MtTexSupport.SwitchFormats[_header.format].BitsPerValue;
            var colorsPerValue = MtTexSupport.SwitchFormats[_header.format].ColorsPerValue;
            var dataSize = _header.width * _header.height / colorsPerValue * bitsPerValue / 8;
            var imageData = br.ReadBytes(dataSize);

            // Read mips
            var mipData = new List<byte[]>();
            for (var i = 1; i < _header.mipCount; i++)
            {
                var mipSize = (_header.width >> i) * (_header.height >> i) / colorsPerValue * bitsPerValue / 8;
                mipData.Add(br.ReadBytes(mipSize));
            }

            // Create image info
            var imageInfo = new ImageInfo(imageData, _header.format, new Size(_header.width, _header.height));

            if (_header.mipCount > 1)
                imageInfo.MipMapData = mipData;

            imageInfo.RemapPixels.With(context => new NxSwizzle(context));

            return imageInfo;
        }

        private IList<ImageInfo> LoadMobile(BinaryReaderX br)
        {
            var texOffsets = br.ReadMultiple<int>(3);
            var texSizes = br.ReadMultiple<int>(3);

            var formats = _mobileHeader.format == 0x0C ?
                new byte[] { 0xFD, 0xFE, 0xFF } :
                new[] { _mobileHeader.format, _mobileHeader.format, _mobileHeader.format };

            var infos = new List<ImageInfo>();
            for (var i = 0; i < 3; i++)
            {
                if (i > 0 && texOffsets[i] == texOffsets[0])
                    continue;

                // Read image data
                var bitDepth = MtTexSupport.MobileFormats[formats[i]].BitDepth;
                var colorsPerValue = MtTexSupport.MobileFormats[formats[i]].ColorsPerValue;
                var dataSize = _mobileHeader.width * _mobileHeader.height * bitDepth / 8;

                br.BaseStream.Position = texOffsets[i];
                var imageData = br.ReadBytes(dataSize);

                // Read mips
                var mipData = new List<byte[]>();
                for (var j = 1; j < _mobileHeader.mipCount; j++)
                {
                    var mipSize = (_mobileHeader.width >> j) * (_mobileHeader.height >> j) * bitDepth / 8;
                    mipData.Add(br.ReadBytes(mipSize));
                }

                // Add image info
                var imageInfo = new ImageInfo(imageData, formats[i], new Size(_mobileHeader.width, _mobileHeader.height));

                if (_mobileHeader.mipCount > 1)
                    imageInfo.MipMapData = mipData;

                // TODO: Remove block swizzle with pre-swizzle implementation in Kanvas
                if (colorsPerValue > 1)
                    imageInfo.RemapPixels.With(context => new BcSwizzle(context));

                infos.Add(imageInfo);
            }

            return infos;
        }

        #endregion

        #region Save

        private void Save3ds(BinaryWriterX bw, ImageInfo imageInfo)
        {
            bw.BaseStream.Position = HeaderSize_;

            // Write mip offsets
            if (_header.version != 0xA4)
            {
                var mipPosition = 0;
                bw.Write(mipPosition);
                mipPosition += imageInfo.ImageData.Length;

                if (imageInfo.MipMapCount > 1)
                    foreach (var mipData in imageInfo.MipMapData)
                    {
                        bw.Write(mipPosition);
                        mipPosition += mipData.Length;
                    }
            }

            // Write image data
            bw.Write(imageInfo.ImageData);

            foreach (var mipData in imageInfo.MipMapData)
                bw.Write(mipData);

            // Update header
            _header.format = (byte)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;
            _header.mipCount = (byte)(imageInfo.MipMapCount + 1);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private void SavePs3(BinaryWriterX bw, ImageInfo imageInfo)
        {
            bw.BaseStream.Position = HeaderSize_;

            // Write mip offsets
            var mipPosition = HeaderSize_ + (imageInfo.MipMapCount + 1) * 4;
            bw.Write(mipPosition);
            mipPosition += imageInfo.ImageData.Length;

            if (imageInfo.MipMapCount > 1)
                foreach (var mipData in imageInfo.MipMapData)
                {
                    bw.Write(mipPosition);
                    mipPosition += mipData.Length;
                }

            // Write image data
            bw.Write(imageInfo.ImageData);

            foreach (var mipData in imageInfo.MipMapData)
                bw.Write(mipData);

            // Update header
            _header.format = (byte)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;
            _header.mipCount = (byte)(imageInfo.MipMapCount + 1);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private void SaveSwitch(BinaryWriterX bw, ImageInfo imageInfo)
        {
            bw.BaseStream.Position = HeaderSize_;

            // Write total tex size
            bw.Write(imageInfo.ImageData.Length + imageInfo.MipMapData.Sum(m => m.Length));

            // Write mip offsets
            var mipPosition = 0;
            bw.Write(mipPosition);
            mipPosition += imageInfo.ImageData.Length;

            if (imageInfo.MipMapCount > 1)
                foreach (var mipData in imageInfo.MipMapData)
                {
                    bw.Write(mipPosition);
                    mipPosition += mipData.Length;
                }

            // Write image data
            bw.Write(imageInfo.ImageData);

            foreach (var mipData in imageInfo.MipMapData)
                bw.Write(mipData);

            // Update header
            _header.format = (byte)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;
            _header.mipCount = (byte)(imageInfo.MipMapCount + 1);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private void SaveMobile(BinaryWriterX bw, IList<ImageInfo> imageInfos)
        {
            var texOffsets = new List<int>();
            var texSizes = new List<int>();

            // Write image data
            bw.BaseStream.Position = HeaderSize_ + 0x18;

            for (var i = 0; i < imageInfos.Count; i++)
            {
                var imageInfo = imageInfos[i];

                if (i > 0 && imageInfo.ImageFormat < 0xFD)
                {
                    texOffsets.Add(texOffsets[0]);
                    texSizes.Add(0);
                }

                var texOffset = (int)bw.BaseStream.Position;
                texOffsets.Add(texOffset);

                bw.Write(imageInfo.ImageData);

                if (imageInfo.MipMapCount > 1)
                    foreach (var mipData in imageInfo.MipMapData)
                        bw.Write(mipData);

                if (imageInfo.ImageFormat >= 0xFD)
                    texSizes.Add((int)bw.BaseStream.Length - texOffset);
                else texSizes.Add(0);
            }

            // Pad tex lists
            while (texOffsets.Count < 3)
                texOffsets.Add(texOffsets[0]);
            while (texSizes.Count < 3)
                texSizes.Add(0);

            // Write tex offsets and sizes
            bw.BaseStream.Position = HeaderSize_;
            bw.WriteMultiple(texOffsets);
            bw.WriteMultiple(texSizes);

            // Update header
            _mobileHeader.format = imageInfos[0].ImageFormat == 0xFD ? (byte)0x0C : (byte)imageInfos[0].ImageFormat;
            _mobileHeader.width = (short)imageInfos[0].ImageSize.Width;
            _mobileHeader.height = (short)imageInfos[0].ImageSize.Height;
            _mobileHeader.mipCount = (byte)(imageInfos[0].MipMapCount + 1);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_mobileHeader);
        }

        #endregion
    }
}
