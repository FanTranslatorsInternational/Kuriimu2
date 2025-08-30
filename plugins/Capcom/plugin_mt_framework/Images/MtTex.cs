using Kanvas;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;
using ByteOrder = Komponent.Contract.Enums.ByteOrder;

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

        public IList<ImageFileInfo> Load(Stream input, MtTexPlatform platform)
        {
            _platform = platform;
            using var br = new BinaryReaderX(input);

            // Determine byte order
            if (br.PeekString(4) == "\0XET")
                br.ByteOrder = ByteOrder.BigEndian;

            // Read header
            _header = ReadHeader(br);

            input.Position = 0;
            _header87 = ReadHeader87(br);

            input.Position = 0;
            _mobileHeader = ReadMobileHeader(br);

            switch (platform)
            {
                case MtTexPlatform.Wii:
                    return [LoadWii(br)];

                case MtTexPlatform.N3DS:
                    return Load3ds(br);

                case MtTexPlatform.PS3:
                    return LoadPs3(br);

                case MtTexPlatform.Switch:
                    return [LoadSwitch(br)];

                case MtTexPlatform.Pc:
                case MtTexPlatform.Pc87:
                    return [LoadPc(br)];

                case MtTexPlatform.Mobile:
                    return LoadMobile(br);

                default:
                    throw new InvalidOperationException($"Unsupported platform {platform}.");
            }
        }

        public void Save(Stream output, IList<ImageFileInfo> imageInfos)
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

        private ImageFileInfo LoadWii(BinaryReaderX br)
        {
            throw new InvalidOperationException("MT TEX from Wii are not supported yet.");

            // TODO: Those TEX are just a container for the bres format by Nintendo (http://wiki.tockdom.com/wiki/BRRES_(File_Format))
        }

        private IList<ImageFileInfo> Load3ds(BinaryReaderX br)
        {
            var dataOffset = HeaderSize_;
            var bitDepth = MtTexSupport.CtrFormats[_header.format].BitDepth;

            // Skip unknown region (assume region to be 0x6C)
            if (_header.imageData.swizzle == 0x40)
            {
                _unkRegion = br.ReadBytes(0x6C);
                dataOffset += 0x6C;
            }

            // Skip mip offsets
            IList<int> mipOffsets = Array.Empty<int>();
            if (_header.imageData.version != 0xA4)
            {
                mipOffsets = ReadIntegers(br, _header.imageData.mipCount * _header.imgCount);
                dataOffset += _header.imageData.mipCount * _header.imgCount * 4;
            }

            // Read images
            var imageInfos = new List<ImageFileInfo>();
            for (var i = 0; i < _header.imgCount; i++)
            {
                // Read mips
                var mipData = new List<byte[]>();
                for (var m = 0; m < _header.imageData.mipCount; m++)
                {
                    var mipSize = (_header.imageData.width >> m) * (_header.imageData.height >> m) * bitDepth / 8;

                    if (_header.imageData.version != 0xA4)
                        br.BaseStream.Position = dataOffset + mipOffsets[i * _header.imageData.mipCount + m];

                    mipData.Add(br.ReadBytes(mipSize));
                }

                // Create image info
                var imageInfo = new ImageFileInfo
                {
                    BitDepth = MtTexSupport.GetBitDepth(_platform, _header.format),
                    ImageData = mipData[0],
                    ImageFormat = _header.format,
                    ImageSize = new Size(_header.imageData.width, _header.imageData.height),
                    RemapPixels = context => new CtrSwizzle(context)
                };

                if (_header.imageData.mipCount > 1)
                    imageInfo.MipMapData = mipData.Skip(1).ToArray();

                imageInfos.Add(imageInfo);
            }

            return imageInfos;
        }

        private IList<ImageFileInfo> LoadPs3(BinaryReaderX br)
        {
            var bitDepth = MtTexSupport.Ps3Formats[_header.format].BitDepth;
            var colorsPerValue = MtTexSupport.Ps3Formats[_header.format].ColorsPerValue;

            // Skip mip offsets
            var mipOffsets = ReadIntegers(br, _header.imageData.mipCount);

            // Read images
            var imageInfos = new List<ImageFileInfo>();
            for (var i = 0; i < _header.imgCount; i++)
            {
                // Read mips
                var mipData = new List<byte[]>();
                for (var m = 0; m < _header.imageData.mipCount; m++)
                {
                    var mipSize = (_header.imageData.width >> m) * (_header.imageData.height >> m) * bitDepth / 8;

                    br.BaseStream.Position = mipOffsets[i * _header.imageData.mipCount + m];
                    mipData.Add(br.ReadBytes(mipSize));
                }

                // Create image info
                var imageInfo = new ImageFileInfo
                {
                    BitDepth = MtTexSupport.GetBitDepth(_platform, _header.format),
                    ImageData = mipData[0],
                    ImageFormat = _header.format,
                    ImageSize = new Size(_header.imageData.width, _header.imageData.height)
                };

                if (_header.imageData.mipCount > 1)
                    imageInfo.MipMapData = mipData.Skip(1).ToArray();

                // TODO: Remove block swizzle with pre-swizzle implementation in Kanvas
                if (colorsPerValue > 1)
                    imageInfo.RemapPixels = context => new BcSwizzle(context);

                imageInfos.Add(imageInfo);
            }

            return imageInfos;
        }

        private ImageFileInfo LoadSwitch(BinaryReaderX br)
        {
            // Skip unknown data between header and mipOffsets
            var texSize = br.ReadUInt32();

            // Skip mip offsets
            var mipOffsets = ReadIntegers(br, _header.imageData.mipCount);

            // Read image data
            var dataSize = (mipOffsets.Length > 1 ? mipOffsets[1] : texSize) - mipOffsets[0];
            var imageData = br.ReadBytes((int)dataSize);

            // Read mips
            var mipData = new List<byte[]>();
            for (var i = 1; i < _header.imageData.mipCount; i++)
            {
                dataSize = (i + 1 < mipOffsets.Length ? mipOffsets[i + 1] : texSize) - mipOffsets[i];

                mipData.Add(br.ReadBytes((int)dataSize));
            }

            // Create image info
            var imageInfo = new ImageFileInfo
            {
                BitDepth = MtTexSupport.GetBitDepth(_platform, _header.format),
                ImageData = imageData,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.imageData.width, _header.imageData.height),
                RemapPixels = context => new NxSwizzle(context)
            };

            if (_header.imageData.mipCount > 1)
                imageInfo.MipMapData = mipData;

            return imageInfo;
        }

        private ImageFileInfo LoadPc(BinaryReaderX br)
        {
            // We can use the version of the normal header here, since the version of both normal and 87 header overlap
            var version = _header.imageData.version;
            var headerSize = version == 0x87 ? HeaderSize87_ : HeaderSize_;

            br.BaseStream.Position = headerSize;

            // Skip unknown data between header and mipOffsets
            if (version != 0x87)
                br.ReadUInt32();    // texOffset?

            // Skip mip offsets
            var mipCount = version == 0x87 ? _header87.imageData.mipCount : _header.imageData.mipCount;
            ReadIntegers(br, mipCount - 1);

            // Collect values
            var format = version == 0x87 ? _header87 is { format: 0x19, useDxt10: 0 } ? 0xFF : _header87.format : _header.format;
            var width = version == 0x87 ? _header87.imageData.width : _header.imageData.width;
            var height = version == 0x87 ? _header87.imageData.height : _header.imageData.height;
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
            var imageInfo = new ImageFileInfo
            {
                BitDepth = MtTexSupport.GetBitDepth(_platform, format),
                ImageData = imageData,
                ImageFormat = format,
                ImageSize = new Size(width, height)
            };

            if (mipCount > 1)
                imageInfo.MipMapData = mipData;

            if (colorsPerValue > 1)
                imageInfo.RemapPixels = context => new BcSwizzle(context);

            return imageInfo;
        }

        private IList<ImageFileInfo> LoadMobile(BinaryReaderX br)
        {
            var infos = new List<ImageFileInfo>();

            // Determine GPU independent mobile format specification
            _isGpuDependent = _mobileHeader.format == 0xC;

            // HINT: For GPU dependent
            if (_mobileHeader.format != 0xC)
            {
                var bitDepth = MtTexSupport.MobileFormats[_mobileHeader.format].BitDepth;
                var expectedLength = HeaderSize_;
                for (var i = 0; i < _mobileHeader.imageData.mipCount; i++)
                    expectedLength += (_mobileHeader.imageData.width >> i) * (_mobileHeader.imageData.height >> i) * bitDepth / 8;

                _isGpuDependent = expectedLength != br.BaseStream.Length;
            }

            // Specially handle gpu dependent tex, which include specially encoded images based on the used GPU of the mobile platform
            if (_isGpuDependent)
            {
                var texOffsets = ReadIntegers(br, 3);
                var texSizes = ReadIntegers(br, 3);

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
                    var dataSize = _mobileHeader.imageData.width * _mobileHeader.imageData.height * bitDepth / 8;

                    br.BaseStream.Position = texOffsets[i];
                    var imageData = br.ReadBytes(dataSize);

                    // Read mips
                    var mipData = new List<byte[]>();
                    for (var j = 1; j < _mobileHeader.imageData.mipCount; j++)
                    {
                        var mipSize = (_mobileHeader.imageData.width >> j) * (_mobileHeader.imageData.height >> j) * bitDepth / 8;
                        mipData.Add(br.ReadBytes(mipSize));
                    }

                    // Add image info
                    var imageInfo = new ImageFileInfo
                    {
                        BitDepth = MtTexSupport.GetBitDepth(_platform, formats[i]),
                        ImageData = imageData,
                        ImageFormat = formats[i],
                        ImageSize = new Size(_mobileHeader.imageData.width, _mobileHeader.imageData.height)
                    };

                    if (_mobileHeader.imageData.mipCount > 1)
                        imageInfo.MipMapData = mipData;

                    // TODO: Remove block swizzle with pre-swizzle implementation in Kanvas
                    if (colorsPerValue > 1)
                        imageInfo.RemapPixels = context => new BcSwizzle(context);

                    infos.Add(imageInfo);
                }
            }
            else
            {
                // Read image data
                var bitDepth = MtTexSupport.MobileFormats[_mobileHeader.format].BitDepth;
                var colorsPerValue = MtTexSupport.MobileFormats[_mobileHeader.format].ColorsPerValue;
                var dataSize = _mobileHeader.imageData.width * _mobileHeader.imageData.height * bitDepth / 8;
                var imageData = br.ReadBytes(dataSize);

                // Read mips
                var mipData = new List<byte[]>();
                for (var j = 1; j < _mobileHeader.imageData.mipCount; j++)
                {
                    var mipSize = (_mobileHeader.imageData.width >> j) * (_mobileHeader.imageData.height >> j) * bitDepth / 8;
                    mipData.Add(br.ReadBytes(mipSize));
                }

                // Add image info
                var imageInfo = new ImageFileInfo
                {
                    BitDepth = MtTexSupport.GetBitDepth(_platform, _mobileHeader.format),
                    ImageData = imageData,
                    ImageFormat = _mobileHeader.format,
                    ImageSize = new Size(_mobileHeader.imageData.width, _mobileHeader.imageData.height)
                };

                if (_mobileHeader.imageData.mipCount > 1)
                    imageInfo.MipMapData = mipData;

                // TODO: Remove block swizzle with pre-swizzle implementation in Kanvas
                if (colorsPerValue > 1)
                    imageInfo.RemapPixels = context => new BcSwizzle(context);

                infos.Add(imageInfo);
            }

            return infos;
        }

        private MtTexHeader ReadHeader(BinaryReaderX reader)
        {
            return new MtTexHeader
            {
                magic = reader.ReadString(4),
                imageData = BinaryTypeReader.Read<MtTexHeaderImageData>(reader)!,
                imgCount = reader.ReadByte(),
                format = reader.ReadByte(),
                unk3 = reader.ReadUInt16()
            };
        }

        private MtTexHeader87 ReadHeader87(BinaryReaderX reader)
        {
            return new MtTexHeader87
            {
                magic = reader.ReadString(4),
                version = reader.ReadByte(),
                useDxt10 = reader.ReadByte(),
                reserved1 = reader.ReadInt16(),
                imageData = BinaryTypeReader.Read<MtTexHeader87ImageData>(reader)!,
                format = reader.ReadInt32()
            };
        }

        private MobileMtTexHeader ReadMobileHeader(BinaryReaderX reader)
        {
            return new MobileMtTexHeader
            {
                magic = reader.ReadString(4),
                version = reader.ReadUInt16(),
                format = reader.ReadByte(),
                unk1 = reader.ReadByte(),
                imageData = BinaryTypeReader.Read<MobileMtTexHeaderImageData>(reader)!
            };
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        #endregion

        #region Save

        private void Save3ds(BinaryWriterX bw, IList<ImageFileInfo> imageInfos)
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
            if (_header.imageData.version != 0xA4)
            {
                var mipPosition = 0;
                foreach (var imageInfo in imageInfos)
                {
                    bw.Write(mipPosition);
                    mipPosition += imageInfo.ImageData.Length;

                    if ((imageInfo.MipMapData?.Count ?? 0) <= 0)
                        continue;

                    foreach (var mipData in imageInfo.MipMapData!)
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

                if (imageInfo.MipMapData is not null)
                    foreach (var mipData in imageInfo.MipMapData)
                        bw.Write(mipData);
            }

            // Update header
            _header.format = (byte)imageInfos[0].ImageFormat;
            _header.imageData.width = (short)imageInfos[0].ImageSize.Width;
            _header.imageData.height = (short)imageInfos[0].ImageSize.Height;
            _header.imageData.mipCount = (byte)((imageInfos[0].MipMapData?.Count ?? 0) + 1);

            // Write header
            bw.BaseStream.Position = 0;
            WriteHeader(_header, bw);
        }

        private void SavePs3(BinaryWriterX bw, IList<ImageFileInfo> imageInfos)
        {
            // Check for image information being equal
            if (imageInfos.Select(x => x.ImageFormat).Distinct().Count() > 1)
                throw new InvalidOperationException("All images have to be in the same image encoding.");
            if (imageInfos.Select(x => x.ImageSize).Distinct().Count() > 1)
                throw new InvalidOperationException("All images have to have the same dimensions.");

            bw.BaseStream.Position = HeaderSize_;

            // Write mip offsets
            var mipPosition = HeaderSize_ + (imageInfos.Count + imageInfos.Sum(x => x.MipMapData?.Count ?? 0)) * 4;
            foreach (var imageInfo in imageInfos)
            {
                bw.Write(mipPosition);
                mipPosition += imageInfo.ImageData.Length;

                if ((imageInfo.MipMapData?.Count ?? 0) <= 0)
                    continue;

                foreach (var mipData in imageInfo.MipMapData!)
                {
                    bw.Write(mipPosition);
                    mipPosition += mipData.Length;
                }
            }

            // Write image data
            foreach (var imageInfo in imageInfos)
            {
                bw.Write(imageInfo.ImageData);

                if (imageInfo.MipMapData is not null)
                    foreach (var mipData in imageInfo.MipMapData)
                        bw.Write(mipData);
            }

            // Update header
            _header.format = (byte)imageInfos[0].ImageFormat;
            _header.imageData.width = (short)imageInfos[0].ImageSize.Width;
            _header.imageData.height = (short)imageInfos[0].ImageSize.Height;
            _header.imageData.mipCount = (byte)((imageInfos[0].MipMapData?.Count ?? 0) + 1);

            // Write header
            bw.BaseStream.Position = 0;
            WriteHeader(_header, bw);
        }

        private void SaveSwitch(BinaryWriterX bw, ImageFileInfo imageInfo)
        {
            bw.BaseStream.Position = HeaderSize_;

            // Write total tex size
            bw.Write(imageInfo.ImageData.Length + (imageInfo.MipMapData?.Sum(m => m.Length) ?? 0));

            // Write mip offsets
            var mipPosition = 0;

            bw.Write(mipPosition);
            mipPosition += imageInfo.ImageData.Length;

            if ((imageInfo.MipMapData?.Count ?? 0) > 0)
            {
                foreach (var mipData in imageInfo.MipMapData!)
                {
                    bw.Write(mipPosition);
                    mipPosition += mipData.Length;
                }
            }

            // Write image data
            bw.Write(imageInfo.ImageData);

            if (imageInfo.MipMapData is not null)
                foreach (var mipData in imageInfo.MipMapData)
                    bw.Write(mipData);

            // Update header
            _header.format = (byte)imageInfo.ImageFormat;
            _header.imageData.width = (short)imageInfo.ImageSize.Width;
            _header.imageData.height = (short)imageInfo.ImageSize.Height;
            _header.imageData.mipCount = (byte)((imageInfo.MipMapData?.Count ?? 0) + 1);

            // Write header
            bw.BaseStream.Position = 0;
            WriteHeader(_header, bw);
        }

        private void SavePc(BinaryWriterX bw, ImageFileInfo imageInfo)
        {
            var version = _header.imageData.version;

            // Write data offsets
            int dataOffset;
            long offsetPosition;
            switch (version)
            {
                case 0x87:
                    dataOffset = HeaderSize87_ + ((imageInfo.MipMapData?.Count ?? 0) + 1) * 4;
                    offsetPosition = HeaderSize87_;
                    break;

                case 0x9d:
                    dataOffset = HeaderSize_ + ((imageInfo.MipMapData?.Count ?? 0) + 1) * 4;
                    offsetPosition = HeaderSize_;
                    break;

                case 0xa3:
                    dataOffset = HeaderSize_ + 4 + ((imageInfo.MipMapData?.Count ?? 0) + 1) * 4;
                    offsetPosition = HeaderSize_;
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported PC version {version}.");
            }

            bw.BaseStream.Position = offsetPosition;
            bw.Write(dataOffset);

            // Write mip offsets
            dataOffset += imageInfo.ImageData.Length;

            if ((imageInfo.MipMapData?.Count ?? 0) > 0)
            {
                foreach (var mipData in imageInfo.MipMapData!)
                {
                    bw.Write(dataOffset);
                    dataOffset += mipData.Length;
                }
            }

            if (version is 0xa3)
                bw.Write(0);

            // Write image data
            bw.Write(imageInfo.ImageData);

            if (imageInfo.MipMapData is not null)
                foreach (var mipData in imageInfo.MipMapData)
                    bw.Write(mipData);

            // Update header
            if (version == 0x87)
            {
                _header87.format = (byte)(imageInfo.ImageFormat == 0xFF ? 0x19 : imageInfo.ImageFormat);
                _header87.imageData.width = (short)imageInfo.ImageSize.Width;
                _header87.imageData.height = (short)imageInfo.ImageSize.Height;
                _header87.imageData.mipCount = (byte)((imageInfo.MipMapData?.Count ?? 0) + 1);
            }
            else
            {
                _header.format = (byte)imageInfo.ImageFormat;
                _header.imageData.width = (short)imageInfo.ImageSize.Width;
                _header.imageData.height = (short)imageInfo.ImageSize.Height;
                _header.imageData.mipCount = (byte)((imageInfo.MipMapData?.Count ?? 0) + 1);
            }

            // Write header
            bw.BaseStream.Position = 0;
            if (version == 0x87)
                WriteHeader87(_header87, bw);
            else
                WriteHeader(_header, bw);
        }

        private void SaveMobile(BinaryWriterX bw, IList<ImageFileInfo> imageInfos)
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
                    if ((imageInfo.MipMapData?.Count ?? 0) > 0)
                        foreach (var mipData in imageInfo.MipMapData!)
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
                WriteIntegers(texOffsets, bw);
                WriteIntegers(texSizes, bw);

                // Update header format
                _mobileHeader.format = imageInfos[0].ImageFormat == 0xFD ? (byte)0x0C : (byte)imageInfos[0].ImageFormat;
            }
            else
            {
                // Write image data
                bw.BaseStream.Position = HeaderSize_;
                bw.Write(imageInfos[0].ImageData);

                // Write mip data
                if ((imageInfos[0].MipMapData?.Count ?? 0) > 0)
                    foreach (var mipData in imageInfos[0].MipMapData!)
                        bw.Write(mipData);

                // Update header format
                _mobileHeader.format = (byte)imageInfos[0].ImageFormat;
            }

            // Update header
            _mobileHeader.imageData.width = (short)imageInfos[0].ImageSize.Width;
            _mobileHeader.imageData.height = (short)imageInfos[0].ImageSize.Height;
            _mobileHeader.imageData.mipCount = (byte)((imageInfos[0].MipMapData?.Count ?? 0) + 1);

            // Write header
            bw.BaseStream.Position = 0;
            WriteMobileHeader(_mobileHeader, bw);
        }

        private void WriteHeader(MtTexHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            BinaryTypeWriter.Write(header.imageData, writer);
            writer.Write(header.imgCount);
            writer.Write(header.format);
            writer.Write(header.unk3);
        }

        private void WriteHeader87(MtTexHeader87 header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.useDxt10);
            writer.Write(header.reserved1);
            BinaryTypeWriter.Write(header.imageData, writer);
            writer.Write(header.format);
        }

        private void WriteMobileHeader(MobileMtTexHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.format);
            writer.Write(header.unk1);
            BinaryTypeWriter.Write(header.imageData, writer);
        }

        private void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }

        #endregion
    }
}
