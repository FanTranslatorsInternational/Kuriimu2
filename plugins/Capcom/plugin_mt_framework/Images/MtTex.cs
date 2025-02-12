using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kanvas.Swizzle;
using Komponent.IO;
using SixLabors.ImageSharp;
using ByteOrder = Kontract.Models.IO.ByteOrder;
using ImageInfo = Kontract.Models.Image.ImageInfo;

namespace plugin_mt_framework.Images
{
    // TODO: Platofrm.Mobile may only expose one image; For 0x0C expose DXT5 image and internally use KanvasImage for transcoding to ATC and PVRTC
    class MtTex
    {
        private const int HeaderSize_ = 0x10;
        private const int HeaderSize87_ = 0x14;

        private MtTexPlatform _platform;
        private MtTexHeader _header;
        private MtTexHeader87 _header87;
        private MobileMtTexHeader _mobileHeader;

        private byte[] _unkRegion;
        private bool _isGpuDependent;

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
            _header87 = br.ReadType<MtTexHeader87>();

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
                case MtTexPlatform.Pc87:
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
                case MtTexPlatform.Pc87:
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
            var format = _header.version == 0x98 && _header.format == 0x14 ? 0xFF : _header.format;

            var bitDepth = MtTexSupport.Ps3Formats[format].BitDepth;
            var colorsPerValue = MtTexSupport.Ps3Formats[format].ColorsPerValue;

            // Skip mip offsets
            var mipOffsets = br.ReadMultiple<int>(_header.mipCount);

            // Read images
            var imageInfos = new List<ImageInfo>();
            for (var i = 0; i < _header.imgCount; i++)
            {
                // Read mips
                var mipData = new List<byte[]>();
                for (var m = 0; m < _header.mipCount; m++)
                {
                    var mipSize = (_header.width >> m) * (_header.height >> m) * bitDepth / 8;

                    br.BaseStream.Position = mipOffsets[i * _header.mipCount + m];
                    mipData.Add(br.ReadBytes(mipSize));
                }

                // Create image info
                var imageInfo = new ImageInfo(mipData[0], format, new Size(_header.width, _header.height));

                if (_header.mipCount > 1)
                    imageInfo.MipMapData = mipData.Skip(1).ToArray();
                
                if (colorsPerValue > 1)
                    imageInfo.RemapPixels.With(context => new BcSwizzle(context));
                else
                    imageInfo.RemapPixels.With(context => new VitaSwizzle(context));

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
            // We can use the version of the normal header here, since the version of both normal and 87 header overlap
            var version = _header.version;
            var headerSize = version == 0x87 ? HeaderSize87_ : HeaderSize_;

            br.BaseStream.Position = headerSize;

            // Skip unknown data between header and mipOffsets
            if (version != 0x87)
                br.ReadUInt32();    // texOffset?

            // Skip mip offsets
            var mipCount = version == 0x87 ? _header87.mipCount : _header.mipCount;
            br.ReadMultiple<int>(mipCount);

            // Collect values
            var format = version == 0x87 ? _header87.format == 0x19 && _header87.useDxt10 == 0 ? 0xFF : _header87.format : _header.format;
            var width = version == 0x87 ? _header87.width : _header.width;
            var height = version == 0x87 ? _header87.height : _header.height;
            var encodings = version == 0x87 ? MtTexSupport.Pc87Formats : MtTexSupport.PcFormats;

            // Read image data
            // HINT: Calculating dataSize by bitsPerValue and colorsPerValue, since bitDepth can be 0 or some float due to ASTC
            var bitsPerValue = encodings[format].BitsPerValue;
            var colorsPerValue = encodings[format].ColorsPerValue;

            var dataSize = width * height / colorsPerValue * bitsPerValue / 8;
            var imageData = br.ReadBytes(dataSize);

            // Read mips
            var mipData = new List<byte[]>();
            for (var i = 1; i < mipCount; i++)
            {
                var mipSize = (width >> i) * (height >> i) / colorsPerValue * bitsPerValue / 8;
                mipData.Add(br.ReadBytes(mipSize));
            }

            // Create image info
            var imageInfo = new ImageInfo(imageData, format, new Size(width, height));

            if (mipCount > 1)
                imageInfo.MipMapData = mipData;

            if (colorsPerValue > 1)
                imageInfo.RemapPixels.With(context => new BcSwizzle(context));

            return imageInfo;
        }

        private IList<ImageInfo> LoadMobile(BinaryReaderX br)
        {
            var infos = new List<ImageInfo>();

            // Determine GPU independent mobile format specification
            _isGpuDependent = _mobileHeader.format == 0xC;

            // HINT: For GPU dependent
            if (_mobileHeader.format != 0xC)
            {
                var bitDepth = MtTexSupport.MobileFormats[_mobileHeader.format].BitDepth;
                var expectedLength = HeaderSize_;
                for (var i = 0; i < _mobileHeader.mipCount; i++)
                    expectedLength += (_mobileHeader.width >> i) * (_mobileHeader.height >> i) * bitDepth / 8;

                _isGpuDependent = expectedLength != br.BaseStream.Length;
            }

            // Specially handle gpu dependent tex, which include specially encoded images based on the used GPU of the mobile platform
            if (_isGpuDependent)
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
            }
            else
            {
                // Read image data
                var bitDepth = MtTexSupport.MobileFormats[_mobileHeader.format].BitDepth;
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
            var version = _header.version;
            var headerSize = version == 0x87 ? HeaderSize87_ : HeaderSize_;

            // Write data offset
            var dataOffset = headerSize + (version == 0x87 ? 0 : 4) + (imageInfo.MipMapCount + 1) * 4;

            bw.BaseStream.Position = headerSize;
            if (version != 0x87)
                bw.Write(dataOffset);

            // Write mip offsets
            var mipPosition = version == 0x87 ? dataOffset : 0;
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
            if (version == 0x87)
            {
                _header87.format = (byte)(imageInfo.ImageFormat == 0xFF ? 0x19 : imageInfo.ImageFormat);
                _header87.width = (short)imageInfo.ImageSize.Width;
                _header87.height = (short)imageInfo.ImageSize.Height;
                _header87.mipCount = (byte)(imageInfo.MipMapCount + 1);
            }
            else
            {
                _header.format = (byte)imageInfo.ImageFormat;
                _header.width = (short)imageInfo.ImageSize.Width;
                _header.height = (short)imageInfo.ImageSize.Height;
                _header.mipCount = (byte)(imageInfo.MipMapCount + 1);
            }

            // Write header
            bw.BaseStream.Position = 0;
            if (version == 0x87)
                bw.WriteType(_header87);
            else
                bw.WriteType(_header);
        }

        private void SaveMobile(BinaryWriterX bw, IList<ImageInfo> imageInfos)
        {
            if (_isGpuDependent)
            {
                var texOffsets = new List<int>();
                var texSizes = new List<int>();

                // Write image data
                bw.BaseStream.Position = HeaderSize_ + 0x18;

                var texBase = bw.BaseStream.Position;
                foreach (var imageInfo in imageInfos)
                {
                    // HINT: If the special format 0xC is not used, all lengths are 0, and the offset is the same for all 3 GPU entries
                    var texOffset = imageInfo.ImageFormat >= 0xFD ? bw.BaseStream.Position : texBase;
                    var texSize = imageInfo.ImageFormat >= 0xFD ? imageInfo.ImageData.Length : 0;

                    bw.Write(imageInfo.ImageData);

                    // Write mip data
                    if (imageInfo.MipMapCount > 1)
                        foreach (var mipData in imageInfo.MipMapData)
                            bw.Write(mipData);

                    texOffsets.Add((int)texOffset);
                    texSizes.Add(texSize);
                }

                // Pad offset and size lists
                while (texOffsets.Count < 3)
                    texOffsets.Add((int)texBase);
                while (texSizes.Count < 3)
                    texSizes.Add(0);

                // Write offsets and sizes
                bw.BaseStream.Position = HeaderSize_;
                bw.WriteMultiple(texOffsets);
                bw.WriteMultiple(texSizes);

                // Update header format
                _mobileHeader.format = imageInfos[0].ImageFormat == 0xFD ? (byte)0x0C : (byte)imageInfos[0].ImageFormat;
            }
            else
            {
                // Write image data
                bw.BaseStream.Position = HeaderSize_;
                bw.Write(imageInfos[0].ImageData);

                // Write mip data
                if (imageInfos[0].MipMapCount > 1)
                    foreach (var mipData in imageInfos[0].MipMapData)
                        bw.Write(mipData);

                // Update header format
                _mobileHeader.format = (byte)imageInfos[0].ImageFormat;
            }

            // Update header
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
