using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas;
using Kanvas.Models;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Komponent.IO;
using Kontract.FileSystem;
using Kontract.Models.Image;
using ByteOrder = Komponent.IO.ByteOrder;

namespace plugin_mt_framework.TEX
{
    public sealed partial class TEX
    {
        public List<BitmapInfo> Bitmaps = new List<BitmapInfo>();
        private const int MinHeight = 8;

        //int Version;

        private FileHeader Header;
        public FileHeaderInfo HeaderInfo { get; set; }
        private int HeaderLength = 0x10;
        public ImageSettings Settings;
        private ByteOrder ByteOrder = ByteOrder.LittleEndian;

        //There are variants of Switch versions with a strange block of unidentified data between header and mipmap sizes
        //Additionally there is also way too much overflowing data after the "last" mipmap, decoding that are seemingly copies of the same textures
        //Those 2 variables are to store these unknown and overflowing data to ensure a 1:1 saving at first
        //This behaviour is subject to change at a later point
        private byte[] SwitchUnknownData = null;
        private byte[] SwitchOverflowingData = null;

        public TEX(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                // Set endianess
                if (br.PeekString() == "\0XET")
                    br.ByteOrder = ByteOrder = ByteOrder.BigEndian;

                // Header
                Header = br.ReadType<FileHeader>();
                HeaderInfo = new FileHeaderInfo
                {
                    // Block 1
                    Version = (Version)(Header.Block1 & 0xFFF),
                    Unknown1 = (int)((Header.Block1 >> 12) & 0xFFF),
                    Unused1 = (int)((Header.Block1 >> 24) & 0xF),
                    AlphaChannelFlags = (AlphaChannelFlags)((Header.Block1 >> 28) & 0xF),
                    // Block 2
                    MipMapCount = (int)(Header.Block2 & 0x3F),
                    Width = (int)((Header.Block2 >> 6) & 0x1FFF),
                    Height = Math.Max((int)((Header.Block2 >> 19) & 0x1FFF), MinHeight),
                    // Block 3
                    Unknown2 = (int)(Header.Block3 & 0xFF),
                    Format = (byte)((Header.Block3 >> 8) & 0xFF),
                    Unknown3 = (int)((Header.Block3 >> 16) & 0xFFFF)
                };

                if (HeaderInfo.Version == Version._Switchv1 && !SwitchFormats.ContainsKey(HeaderInfo.Format))
                    throw new InvalidOperationException($"Switch texture format 0x{HeaderInfo.Format.ToString("X2")} is not implemented.");
                else if (!Formats.ContainsKey(HeaderInfo.Format))
                    throw new InvalidOperationException($"Texture format 0x{HeaderInfo.Format.ToString("X2")} is not implemented.");

                // TODO: Consider whether the following settings make more sense if conditioned by the ByteOrder (or Platform)
                //var format = HeaderInfo.Format.ToString().StartsWith("DXT1") ? Format.DXT1 : HeaderInfo.Format.ToString().StartsWith("DXT5") ? Format.DXT5 : HeaderInfo.Format;
                var encoding = (HeaderInfo.Version == Version._Switchv1) ? SwitchFormats[HeaderInfo.Format] : Formats[HeaderInfo.Format];

                List<int> mipMaps = null;
                if (HeaderInfo.Version == Version._Switchv1)
                {
                    var texOverallSize = br.ReadInt32();
                    if (texOverallSize > br.BaseStream.Length)
                    {
                        br.BaseStream.Position -= 4;
                        SwitchUnknownData = br.ReadBytes(0x6C);
                        texOverallSize = br.ReadInt32();
                        HeaderInfo.MipMapCount++;
                    }
                    mipMaps = br.ReadMultiple<int>(HeaderInfo.MipMapCount);
                }
                else if (HeaderInfo.Version != Version._3DSv1)
                    mipMaps = br.ReadMultiple<int>(HeaderInfo.MipMapCount);

                for (var i = 0; i < mipMaps.Count; i++)
                {
                    var texDataSize = 0;
                    if (SwitchUnknownData != null)
                    {
                        if (i + 1 == HeaderInfo.MipMapCount) continue;
                        texDataSize = mipMaps[i + 1] - mipMaps[i];
                    }
                    else if (HeaderInfo.Version != Version._3DSv1)
                        texDataSize = (i + 1 < HeaderInfo.MipMapCount ? mipMaps[i + 1] : (int)br.BaseStream.Length) - mipMaps[i];
                    else
                        texDataSize = Formats[HeaderInfo.Format].BitDepth * (HeaderInfo.Width >> i) * (HeaderInfo.Height >> i) / 8;

                    Settings = new ImageSettings(encoding, Math.Max(HeaderInfo.Width >> i, 2), Math.Max(HeaderInfo.Height >> i, 2));

                    //Set possible Swizzles
                    if (HeaderInfo.Version == Version._3DSv1 || HeaderInfo.Version == Version._3DSv2 || HeaderInfo.Version == Version._3DSv3)
                        Settings.Swizzle = new CTRSwizzle(Settings.Width, Settings.Height, CtrTransformation.None, true);
                    else if (HeaderInfo.Version == Version._Switchv1)
                        Settings.Swizzle = new SwitchSwizzle(Settings.Width, Settings.Height, Settings.Encoding.BitDepth, GetSwitchSwizzleFormat(Settings.Encoding.FormatName), true);
                    else if (Settings.Encoding.FormatName.Contains("DXT"))
                        Settings.Swizzle = new BCSwizzle(Settings.Width, Settings.Height);

                    //Set possible pixel shaders
                    if ((Format)HeaderInfo.Format == Format.DXT5_B)
                        Settings.PixelShader = ToNoAlpha;
                    else if ((Format)HeaderInfo.Format == Format.DXT5_YCbCr)
                        Settings.PixelShader = ToProperColors;

                    EncodingInfo info = null;
                    if (HeaderInfo.Version == Version._3DSv1 || HeaderInfo.Version == Version._3DSv2 ||
                        HeaderInfo.Version == Version._3DSv3)
                        info = EncodingInfos.First(x => x.EncodingIndex == HeaderInfo.Format);
                    else if (HeaderInfo.Version == Version._Switchv1)
                        info = SwitchEncodingInfos.First(x => x.EncodingIndex == HeaderInfo.Format);

                    if (i == 0)
                        Bitmaps.Add(new BitmapInfo(Kolors.Load(br.ReadBytes(texDataSize), Settings), info));
                    else
                        Bitmaps[0].MipMaps.Add(Kolors.Load(br.ReadBytes(texDataSize), Settings));
                }

                if (SwitchUnknownData != null)
                    SwitchOverflowingData = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
            }
        }

        SwitchFormat GetSwitchSwizzleFormat(string formatName)
        {
            switch (formatName)
            {
                case "DXT1":
                    return SwitchFormat.DXT1;
                case "DXT5":
                    return SwitchFormat.DXT5;
                case "ATI1L":
                case "ATI1A":
                    return SwitchFormat.ATI1;
                case "ATI2":
                    return SwitchFormat.ATI2;
                case "RGBA8888":
                    return SwitchFormat.RGBA8888;
                default:
                    return SwitchFormat.Empty;
            }
        }

        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, ByteOrder))
            {
                if (SwitchUnknownData != null)
                    HeaderInfo.MipMapCount--;
                Header.Block1 = (uint)((int)HeaderInfo.Version | (HeaderInfo.Unknown1 << 12) | (HeaderInfo.Unused1 << 24) | ((int)HeaderInfo.AlphaChannelFlags << 28));
                Header.Block2 = (uint)(HeaderInfo.MipMapCount | (HeaderInfo.Width << 6) | (HeaderInfo.Height << 19));
                Header.Block3 = (uint)(HeaderInfo.Unknown2 | ((int)HeaderInfo.Format << 8) | (HeaderInfo.Unknown3 << 16));
                bw.WriteType(Header);
                if (HeaderInfo.Version == Version._Switchv1 && SwitchUnknownData != null)
                    bw.Write(SwitchUnknownData);

                //var format = HeaderInfo.Format.ToString().StartsWith("DXT1") ? Format.DXT1 : HeaderInfo.Format.ToString().StartsWith("DXT5") ? Format.DXT5 : HeaderInfo.Format;
                var encoding = (HeaderInfo.Version == Version._Switchv1) ? SwitchFormats[HeaderInfo.Format] : Formats[HeaderInfo.Format];

                // Mipmap Downsampling
                if (Bitmaps.Count > 1 && HeaderInfo.MipMapCount > 1)
                {
                    var firstBitmap = Bitmaps[0].Image;
                    var width = firstBitmap.Width;
                    var height = firstBitmap.Height;
                    for (var i = 0; i < HeaderInfo.MipMapCount - 1; i++)
                    {
                        var bmp = new Bitmap(width / 2, height / 2);
                        var gfx = Graphics.FromImage(bmp);
                        gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        gfx.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        gfx.DrawImage(firstBitmap, new Rectangle(0, 0, bmp.Width, bmp.Height));
                        Bitmaps[0].MipMaps[i] = bmp;
                        width = bmp.Width;
                        height = bmp.Height;
                    }
                }

                var bitmaps = new List<byte[]>();

                Settings = new ImageSettings(encoding, Bitmaps[0].Image.Width, Bitmaps[0].Image.Height);

                //Set possible Swizzles
                if (HeaderInfo.Version == Version._3DSv1 || HeaderInfo.Version == Version._3DSv2 || HeaderInfo.Version == Version._3DSv3)
                    Settings.Swizzle = new CTRSwizzle(Bitmaps[0].Image.Width, Bitmaps[0].Image.Height, CtrTransformation.None, true);
                else if (HeaderInfo.Version == Version._Switchv1)
                    Settings.Swizzle = new SwitchSwizzle(Bitmaps[0].Image.Width, Bitmaps[0].Image.Height, Settings.Encoding.BitDepth, GetSwitchSwizzleFormat(Settings.Encoding.FormatName), true);    //Switch Swizzle
                else if (Settings.Encoding.FormatName.Contains("DXT"))
                    Settings.Swizzle = new BCSwizzle(Bitmaps[0].Image.Width, Bitmaps[0].Image.Height);

                if ((Format)HeaderInfo.Format == Format.DXT5_B)
                    Settings.PixelShader = ToNoAlpha;
                else if ((Format)HeaderInfo.Format == Format.DXT5_YCbCr)
                    Settings.PixelShader = ToOptimisedColors;

                bitmaps.Add(Kolors.Save(Bitmaps[0].Image, Settings));

                foreach (var mipmap in Bitmaps[0].MipMaps)
                {
                    Settings = new ImageSettings(encoding, Bitmaps[0].Image.Width, Bitmaps[0].Image.Height);

                    //Set possible Swizzles
                    if (HeaderInfo.Version == Version._3DSv1 || HeaderInfo.Version == Version._3DSv2 || HeaderInfo.Version == Version._3DSv3)
                        Settings.Swizzle = new CTRSwizzle(mipmap.Width, mipmap.Height, CtrTransformation.None, true);
                    else if (HeaderInfo.Version == Version._Switchv1)
                        Settings.Swizzle = new SwitchSwizzle(mipmap.Width, mipmap.Height, Settings.Encoding.BitDepth, GetSwitchSwizzleFormat(Settings.Encoding.FormatName), true);    //Switch Swizzle
                    else if (Settings.Encoding.FormatName.Contains("DXT"))
                        Settings.Swizzle = new BCSwizzle(mipmap.Width, mipmap.Height);

                    if ((Format)HeaderInfo.Format == Format.DXT5_B)
                        Settings.PixelShader = ToNoAlpha;
                    else if ((Format)HeaderInfo.Format == Format.DXT5_YCbCr)
                        Settings.PixelShader = ToOptimisedColors;

                    bitmaps.Add(Kolors.Save(mipmap, Settings));
                }

                if (HeaderInfo.Version == Version._Switchv1)
                {
                    if (SwitchUnknownData != null)
                    {
                        var listOffset = bw.BaseStream.Position;
                        bw.BaseStream.Position += 8 + HeaderInfo.MipMapCount * sizeof(int);

                        var offsets = new List<int>();
                        var relOffset = bw.BaseStream.Position;
                        foreach (var bitmap in bitmaps)
                        {
                            var data = new byte[(bitmap.Length + 0x1FF) & ~0x1FF];
                            Array.Copy(bitmap, data, bitmap.Length);
                            bw.Write(data);
                            offsets.Add((int)(bw.BaseStream.Position - relOffset));
                        }
                        offsets[offsets.Count - 1] = (offsets.Last() + 0x7FF) & ~0x7FF;
                        bw.BaseStream.Position = relOffset + offsets.Last();

                        bw.Write(SwitchOverflowingData);

                        var totalSize = bw.BaseStream.Position - 0x84 - HeaderInfo.MipMapCount * sizeof(int);

                        bw.BaseStream.Position = listOffset;
                        bw.Write((int)totalSize);
                        bw.BaseStream.Position += 4;
                        foreach (var offset in offsets)
                            bw.Write(offset);
                    }
                    else
                    {
                        bw.BaseStream.Position += 0x4 + HeaderInfo.MipMapCount * sizeof(int);

                        var offsets = new List<int>();
                        var relOffset = bw.BaseStream.Position;
                        foreach (var bitmap in bitmaps)
                        {
                            offsets.Add((int)(bw.BaseStream.Position - relOffset));
                            var data = new byte[(bitmap.Length + 0x1FF) & ~0x1FF];
                            Array.Copy(bitmap, data, bitmap.Length);
                            bw.Write(data);
                        }
                        var totalSize = bw.BaseStream.Position - (0x10 + 0x4 + HeaderInfo.MipMapCount * sizeof(int));

                        bw.BaseStream.Position = 0x10;
                        bw.Write((int)totalSize);
                        foreach (var offset in offsets)
                            bw.Write(offset);
                    }
                }
                else
                // Mipmaps, but not for Version 3DS v1
                if (HeaderInfo.Version != Version._3DSv1)
                {
                    var offset = HeaderInfo.Version == Version._PS3v1 ? HeaderInfo.MipMapCount * sizeof(int) + HeaderLength : 0;
                    foreach (var bitmap in bitmaps)
                    {
                        bw.Write(offset);
                        offset += bitmap.Length;
                    }
                }

                // Bitmaps
                if (SwitchUnknownData == null)
                    foreach (var bitmap in bitmaps)
                        bw.Write(bitmap);
            }
        }
    }
}
