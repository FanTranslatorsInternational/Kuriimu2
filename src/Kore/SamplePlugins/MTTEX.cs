using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Kanvas;
using Kanvas.Swizzle;
using Komponent.IO;

namespace Kore.SamplePlugins
{
    public sealed partial class MTTEX
    {
        public List<Bitmap> Bitmaps = new List<Bitmap>();
        private const int MinHeight = 8;

        //int Version;

        private FileHeader Header;
        public FileHeaderInfo HeaderInfo { get; set; }
        private int HeaderLength = 0x10;
        public ImageSettings Settings = new ImageSettings();
        private ByteOrder ByteOrder = ByteOrder.LittleEndian;

        //There are variants of Switch versions with a strange block of unidentified data between header and mipmap sizes
        //Additionally there is also way too much overflowing data after the "last" mipmap, decoding that are seemingly copies of the same textures
        //Those 2 variables are to store these unknown and overflowing data to ensure a 1:1 saving at first
        //This behaviour is subject to change at a later point
        private byte[] SwitchUnknownData = null;
        private byte[] SwitchOverflowingData = null;

        public MTTEX(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                // Set endianess
                if (br.PeekString(4, Encoding.ASCII) == "\0XET")
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
                    throw new ImageFormatException($"Switch texture format 0x{HeaderInfo.Format.ToString("X2")} is not implemented.");
                else if (!Formats.ContainsKey(HeaderInfo.Format))
                    throw new ImageFormatException($"Texture format 0x{HeaderInfo.Format.ToString("X2")} is not implemented.");

                // TODO: Consider whether the following settings make more sense if conditioned by the ByteOrder (or Platform)
                //var format = HeaderInfo.Format.ToString().StartsWith("DXT1") ? Format.DXT1 : HeaderInfo.Format.ToString().StartsWith("DXT5") ? Format.DXT5 : HeaderInfo.Format;
                Settings.Format = (HeaderInfo.Version == Version._Switchv1) ? SwitchFormats[HeaderInfo.Format] : Formats[HeaderInfo.Format];

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

                    Settings.Width = Math.Max(HeaderInfo.Width >> i, 2);
                    Settings.Height = Math.Max(HeaderInfo.Height >> i, 2);

                    //Set possible Swizzles
                    if (HeaderInfo.Version == Version._3DSv1 || HeaderInfo.Version == Version._3DSv2 || HeaderInfo.Version == Version._3DSv3)
                        Settings.Swizzle = new CTRSwizzle(Settings.Width, Settings.Height);
                    else if (HeaderInfo.Version == Version._Switchv1)
                        Settings.Swizzle = new NXSwizzle(Settings.Width, Settings.Height, Settings.Format.BitDepth, GetSwitchSwizzleFormat(Settings.Format.FormatName));
                    else if (Settings.Format.FormatName.Contains("DXT"))
                        Settings.Swizzle = new BlockSwizzle(Settings.Width, Settings.Height);

                    //Set possible pixel shaders
                    if ((Format)HeaderInfo.Format == Format.DXT5_B)
                        Settings.PixelShader = ToNoAlpha;
                    else if ((Format)HeaderInfo.Format == Format.DXT5_YCbCr)
                        Settings.PixelShader = ToProperColors;

                    Bitmaps.Add(Common.Load(br.ReadBytes(texDataSize), Settings));
                }

                if (SwitchUnknownData != null)
                    SwitchOverflowingData = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
            }
        }

        NXSwizzle.Format GetSwitchSwizzleFormat(string formatName)
        {
            switch (formatName)
            {
                case "DXT1":
                    return NXSwizzle.Format.DXT1;
                case "DXT5":
                    return NXSwizzle.Format.DXT5;
                case "ATI1L":
                case "ATI1A":
                    return NXSwizzle.Format.ATI1;
                case "ATI2":
                    return NXSwizzle.Format.ATI2;
                case "RGBA8888":
                    return NXSwizzle.Format.RGBA8888;
                default:
                    return NXSwizzle.Format.Empty;
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
                Settings.Format = (HeaderInfo.Version == Version._Switchv1) ? SwitchFormats[HeaderInfo.Format] : Formats[HeaderInfo.Format];

                if ((Format)HeaderInfo.Format == Format.DXT5_B)
                    Settings.PixelShader = ToNoAlpha;
                else if ((Format)HeaderInfo.Format == Format.DXT5_YCbCr)
                    Settings.PixelShader = ToOptimisedColors;

                // Mipmap Downsampling
                if (Bitmaps.Count > 1 && HeaderInfo.MipMapCount > 1)
                {
                    var firstBitmap = Bitmaps[0];
                    var width = firstBitmap.Width;
                    var height = firstBitmap.Height;
                    for (var i = 1; i < HeaderInfo.MipMapCount; i++)
                    {
                        var bmp = new Bitmap(width / 2, height / 2);
                        var gfx = Graphics.FromImage(bmp);
                        gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        gfx.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        gfx.DrawImage(firstBitmap, new Rectangle(0, 0, bmp.Width, bmp.Height));
                        Bitmaps[i] = bmp;
                        width = bmp.Width;
                        height = bmp.Height;
                    }
                }

                var bitmaps = new List<byte[]>();
                foreach (var bmp in Bitmaps)
                {
                    //Set possible Swizzles
                    if (HeaderInfo.Version == Version._3DSv1 || HeaderInfo.Version == Version._3DSv2 || HeaderInfo.Version == Version._3DSv3)
                        Settings.Swizzle = new CTRSwizzle(bmp.Width, bmp.Height);
                    else if (HeaderInfo.Version == Version._Switchv1)
                        Settings.Swizzle = new NXSwizzle(bmp.Width, bmp.Height, Settings.Format.BitDepth, GetSwitchSwizzleFormat(Settings.Format.FormatName));    //Switch Swizzle
                    else if (Settings.Format.FormatName.Contains("DXT"))
                        Settings.Swizzle = new BlockSwizzle(bmp.Width, bmp.Height);

                    bitmaps.Add(Common.Save(bmp, Settings));
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
