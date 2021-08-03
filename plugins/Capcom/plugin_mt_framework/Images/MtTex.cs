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

        private byte[] _unkRegion;
        private bool _isGpuIndependent;

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
                case MtTexPlatform.Wii:
                    return new[] { LoadWii(br) };

                case MtTexPlatform.N3DS:
                    return Load3ds(br);

                case MtTexPlatform.PS3:
                    return LoadPs3(br);

                case MtTexPlatform.Switch:
                    return new[] { LoadSwitch(br) };

                case MtTexPlatform.Pc:
                    return new[] { LoadPc(br) };

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
                    Save3ds(bw, imageInfos);
                    break;

                case MtTexPlatform.PS3:
                    SavePs3(bw, imageInfos);
                    break;

                case MtTexPlatform.Switch:
                    SaveSwitch(bw, imageInfos[0]);
                    break;

                case MtTexPlatform.Pc:
                    SavePc(bw, imageInfos[0]);
                    break;

                case MtTexPlatform.Mobile:
                    SaveMobile(bw, imageInfos);
                    break;
            }
        }

        #region Load

        private ImageInfo LoadWii(BinaryReaderX br)
        {
            throw new InvalidOperationException("MT TEX from Wii are not supported yet.");

            // TODO: Those TEX are just a container for the bres format by Nintendo (http://wiki.tockdom.com/wiki/BRRES_(File_Format))
        }

        private IList<ImageInfo> Load3ds(BinaryReaderX br)
        {
            var dataOffset = HeaderSize_;
            var bitDepth = MtTexSupport.CtrFormats[_header.format].BitDepth;

            // Skip unknown region (assume region to be 0x6C)
            if (_header.swizzle == 0x40)
            {
                _unkRegion = br.ReadBytes(0x6C);
                dataOffset += 0x6C;
            }

            // Skip mip offsets
            IList<int> mipOffsets = Array.Empty<int>();
            if (_header.version != 0xA4)
            {
                mipOffsets = br.ReadMultiple<int>(_header.mipCount * _header.imgCount);
                dataOffset += _header.mipCount * _header.imgCount * 4;
            }

            // Read images
            var imageInfos = new List<ImageInfo>();
            for (var i = 0; i < _header.imgCount; i++)
            {
                // Read mips
                var mipData = new List<byte[]>();
                for (var m = 0; m < _header.mipCount; m++)
                {
                    var mipSize = (_header.width >> m) * (_header.height >> m) * bitDepth / 8;

                    if (_header.version != 0xA4)
                        br.BaseStream.Position = dataOffset + mipOffsets[i * _header.mipCount + m];

                    mipData.Add(br.ReadBytes(mipSize));
                }

                // Create image info
                var imageInfo = new ImageInfo(mipData[0], _header.format, new Size(_header.width, _header.height));

                if (_header.mipCount > 1)
                    imageInfo.MipMapData = mipData.Skip(1).ToArray();

                imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

                imageInfos.Add(imageInfo);
            }

            return imageInfos;
        }

        private IList<ImageInfo> LoadPs3(BinaryReaderX br)
        {
            var bitDepth = MtTexSupport.Ps3Formats[_header.format].BitDepth;
            var colorsPerValue = MtTexSupport.Ps3Formats[_header.format].ColorsPerValue;

            // Skip mip offsets
            var mipOffsets = br.ReadMultiple<int>(_header.mipCount);

            // Read images
            var imageInfos = new List<ImageInfo>();
            for (var i = 0; i < _header.imgCount; i++)
            {
                // Read mips
                var mipData = new List<byte[]>();
                for (var m = 1; m < _header.mipCount; m++)
                {
                    var mipSize = (_header.width >> m) * (_header.height >> m) * bitDepth / 8;

                    br.BaseStream.Position = mipOffsets[i * _header.mipCount + m];
                    mipData.Add(br.ReadBytes(mipSize));
                }

                // Create image info
                var imageInfo = new ImageInfo(mipData[0], _header.format, new Size(_header.width, _header.height));

                if (_header.mipCount > 1)
                    imageInfo.MipMapData = mipData.Skip(1).ToArray();

                // TODO: Remove block swizzle with pre-swizzle implementation in Kanvas
                if (colorsPerValue > 1)
                    imageInfo.RemapPixels.With(context => new BcSwizzle(context));

                imageInfos.Add(imageInfo);
            }

            return imageInfos;
        }

        private ImageInfo LoadSwitch(BinaryReaderX br)
        {
            // Skip unknown data between header and mipOffsets
            var texSize = br.ReadUInt32();

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

        private ImageInfo LoadPc(BinaryReaderX br)
        {
            // Skip unknown data between header and mipOffsets
            var texOffset = br.ReadUInt32();

            // Skip mip offsets
            br.ReadMultiple<int>(_header.mipCount);

            // Read image data
            // HINT: Calculating dataSize by bitsPerValue and colorsPerValue, since bitDepth can be 0 or some float due to ASTC
            var bitsPerValue = MtTexSupport.PcFormats[_header.format].BitsPerValue;
            var colorsPerValue = MtTexSupport.PcFormats[_header.format].ColorsPerValue;
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

            if (colorsPerValue > 1)
                imageInfo.RemapPixels.With(context => new BcSwizzle(context));

            return imageInfo;
        }

        private IList<ImageInfo> LoadMobile(BinaryReaderX br)
        {
            var infos = new List<ImageInfo>();

            // Determine GPU independent mobile format specification
            var bitDepth = MtTexSupport.MobileFormats[_mobileHeader.format].BitDepth;
            var expectedLength = HeaderSize_;
            for (var i = 0; i < _mobileHeader.mipCount; i++)
                expectedLength += (_mobileHeader.width >> i) * (_mobileHeader.height >> i) * bitDepth / 8;

            _isGpuIndependent = expectedLength == br.BaseStream.Length;

            // Specially handle gpu dependent tex, which include specially encoded images based on the used GPU of the mobile platform
            if (!_isGpuIndependent)
            {
                var texOffsets = br.ReadMultiple<int>(3);
                var texSizes = br.ReadMultiple<int>(3);

                var formats = _mobileHeader.format == 0xC
                    ? new byte[] { 0xFD, 0xFE, 0xFF }
                    : new[] { _mobileHeader.format, _mobileHeader.format, _mobileHeader.format };

                for (var i = 0; i < 3; i++)
                {
                    if (i > 0 && texOffsets[i] == texOffsets[0])
                        continue;

                    // Read image data
                    bitDepth = MtTexSupport.MobileFormats[formats[i]].BitDepth;
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
                    var imageInfo = new ImageInfo(imageData, formats[i],
                        new Size(_mobileHeader.width, _mobileHeader.height));

                    if (_mobileHeader.mipCount > 1)
                        imageInfo.MipMapData = mipData;

                    // TODO: Remove block swizzle with pre-swizzle implementation in Kanvas
                    if (colorsPerValue > 1)
                        imageInfo.RemapPixels.With(context => new BcSwizzle(context));

                    infos.Add(imageInfo);
                }
            }
            else
            {
                // Read image data
                bitDepth = MtTexSupport.MobileFormats[_mobileHeader.format].BitDepth;
                var colorsPerValue = MtTexSupport.MobileFormats[_mobileHeader.format].ColorsPerValue;
                var dataSize = _mobileHeader.width * _mobileHeader.height * bitDepth / 8;
                var imageData = br.ReadBytes(dataSize);

                // Read mips
                var mipData = new List<byte[]>();
                for (var j = 1; j < _mobileHeader.mipCount; j++)
                {
                    var mipSize = (_mobileHeader.width >> j) * (_mobileHeader.height >> j) * bitDepth / 8;
                    mipData.Add(br.ReadBytes(mipSize));
                }

                // Add image info
                var imageInfo = new ImageInfo(imageData, _mobileHeader.format, new Size(_mobileHeader.width, _mobileHeader.height));

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

        private void Save3ds(BinaryWriterX bw, IList<ImageInfo> imageInfos)
        {
            // Check for image information being equal
            if (imageInfos.Select(x => x.ImageFormat).Distinct().Count() > 1)
                throw new InvalidOperationException("All images have to be in the same image encoding.");
            if (imageInfos.Select(x => x.ImageSize).Distinct().Count() > 1)
                throw new InvalidOperationException("All images have to have the same dimensions.");

            bw.BaseStream.Position = HeaderSize_;

            // Write unknown region
            if (_unkRegion != null)
                bw.Write(_unkRegion);

            // Write mip offsets
            if (_header.version != 0xA4)
            {
                var mipPosition = 0;
                foreach (var imageInfo in imageInfos)
                {
                    bw.Write(mipPosition);
                    mipPosition += imageInfo.ImageData.Length;

                    if (imageInfo.MipMapCount <= 0)
                        continue;

                    foreach (var mipData in imageInfo.MipMapData)
                    {
                        bw.Write(mipPosition);
                        mipPosition += mipData.Length;
                    }
                }
            }

            // Write image data
            foreach (var imageInfo in imageInfos)
            {
                bw.Write(imageInfo.ImageData);

                foreach (var mipData in imageInfo.MipMapData)
                    bw.Write(mipData);
            }

            // Update header
            _header.format = (byte)imageInfos[0].ImageFormat;
            _header.width = (short)imageInfos[0].ImageSize.Width;
            _header.height = (short)imageInfos[0].ImageSize.Height;
            _header.mipCount = (byte)(imageInfos[0].MipMapCount + 1);

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private void SavePs3(BinaryWriterX bw, IList<ImageInfo> imageInfos)
        {
            // Check for image information being equal
            if (imageInfos.Select(x => x.ImageFormat).Distinct().Count() > 1)
                throw new InvalidOperationException("All images have to be in the same image encoding.");
            if (imageInfos.Select(x => x.ImageSize).Distinct().Count() > 1)
                throw new InvalidOperationException("All images have to have the same dimensions.");

            bw.BaseStream.Position = HeaderSize_;

            // Write mip offsets
            var mipPosition = HeaderSize_ + (imageInfos.Count + imageInfos.Sum(x => x.MipMapCount)) * 4;
            foreach (var imageInfo in imageInfos)
            {
                bw.Write(mipPosition);
                mipPosition += imageInfo.ImageData.Length;

                if (imageInfo.MipMapCount <= 0)
                    continue;

                foreach (var mipData in imageInfo.MipMapData)
                {
                    bw.Write(mipPosition);
                    mipPosition += mipData.Length;
                }
            }

            // Write image data
            foreach (var imageInfo in imageInfos)
            {
                bw.Write(imageInfo.ImageData);

                foreach (var mipData in imageInfo.MipMapData)
                    bw.Write(mipData);
            }

            // Update header
            _header.format = (byte)imageInfos[0].ImageFormat;
            _header.width = (short)imageInfos[0].ImageSize.Width;
            _header.height = (short)imageInfos[0].ImageSize.Height;
            _header.mipCount = (byte)(imageInfos[0].MipMapCount + 1);

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

        private void SavePc(BinaryWriterX bw, ImageInfo imageInfo)
        {
            bw.BaseStream.Position = HeaderSize_;

            // Write data offset
            bw.Write(HeaderSize_ + 4 + (imageInfo.MipMapCount + 1) * 4);

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
