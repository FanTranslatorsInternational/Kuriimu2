using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.FileSystem.Nodes.Physical;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;

// TODO: Add all image features

namespace plugin_mt_framework.GFDv1
{
    [Export(typeof(GFDv1FontAdapter))]
    [Export(typeof(IPlugin))]
    [PluginInfo("3C8827B8-D124-45D7-BD4C-2A98E049A20A", "MT Framework Font v1", "GFDv1", "IcySon55", "", "This is the GFDv1 font adapter for Kuriimu.")]
    [PluginExtensionInfo("*.gfd")]
    public sealed class GFDv1FontAdapter : IFontAdapter2, IFontRenderer, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        private GFDv1 _gfd;

        public enum Versions : uint
        {
            _3DSv1 = 0x10A05, // 68101 GFDv1
            _3DSv2 = 0x10C06, // 68614 GFDv1
            _PS3v1 = 0x10B05, // 68357 GFDv1
        }

        #region Font Properties

        [FormFieldIgnore]
        public IEnumerable<FontCharacter2> Characters
        {
            get => _gfd.Characters;
            set => _gfd.Characters = value.Select(fc => (GFDv1Character)fc).ToList();
        }

        [FormFieldIgnore]
        public List<Bitmap> Textures
        {
            get => _gfd.Textures;
            set => _gfd.Textures = value;
        }

        [FormField(typeof(int), "Font Size")]
        public int FontSize
        {
            get => _gfd.Header.FontSize;
            set => _gfd.Header.FontSize = value;
        }

        [FormField(typeof(float), "Base Line")]
        public float Baseline
        {
            get => _gfd.Header.Baseline;
            set => _gfd.Header.Baseline = value;
        }

        [FormField(typeof(float), "Descent Line")]
        public float DescentLine
        {
            get => _gfd.Header.DescentLine;
            set => _gfd.Header.DescentLine = value;
        }

        public bool LeaveOpen { get; set; }

        #endregion

        #region Font Rendering

        private readonly ImageAttributes _charAttributes = new ImageAttributes();

        public CharWidthInfo GetCharWidthInfo(char c)
        {
            return Characters.FirstOrDefault(chr => chr.Character == c)?.WidthInfo ?? Characters.FirstOrDefault(chr => chr.Character == '?')?.WidthInfo ?? new CharWidthInfo();
        }

        public float MeasureString(string text, char stopChar, float scale = 1)
        {
            throw new NotImplementedException();
        }

        public void SetColor(Color color)
        {
            _charAttributes.SetColorMatrix(new ColorMatrix(new[]
            {
                new[] { color.R / 255f, 0, 0, 0, 0 },
                new[] { 0, color.G / 255f, 0, 0, 0 },
                new[] { 0, 0, color.B / 255f, 0, 0 },
                new[] { 0, 0, 0, color.A / 255f, 0 },
                new[] { 0, 0, 0, 0, 1f }
            }));
        }

        public void Draw(char c, Graphics gfx, float x, float y, float scaleX, float scaleY)
        {
            var character = Characters.FirstOrDefault(chr => (char)chr.Character == c) ?? Characters.FirstOrDefault(chr => (char)chr.Character == '?');
            var widthInfo = character?.WidthInfo;

            if (character == null) return;
            if (gfx == null) return;

            if (widthInfo.GlyphWidth > 0)
                gfx.DrawImage(character.Glyph,
                    new[] {
                        new PointF(x + widthInfo.Left * scaleX, y),
                        new PointF(x + (widthInfo.Left + widthInfo.GlyphWidth) * scaleX, y),
                        new PointF(x + widthInfo.Left * scaleX, y + character.GlyphHeight * scaleY)
                    },
                    new RectangleF(0, 0, widthInfo.GlyphWidth, character.GlyphHeight),
                    GraphicsUnit.Pixel,
                    _charAttributes
                );
        }

        #endregion

        public bool Identify(StreamInfo streamInfo, BaseReadOnlyDirectoryNode fs)
        {
            var result = true;

            try
            {
                using (var br = new BinaryReaderX(streamInfo.FileData, true))
                {
                    if (br.BaseStream.Length < 8)
                        result = false;

                    if (result)
                    {
                        if (br.PeekString() == "\0DFG")
                        {
                            br.ByteOrder = ByteOrder.BigEndian;
                            br.BitOrder = BitOrder.LSBFirst;
                        }

                        var magic = br.ReadString(4);
                        if (!magic.StartsWith("GFD\0") && !magic.StartsWith("\0DFG"))
                            result = false;

                        var version = (Versions)br.ReadUInt32();
                        if (version != Versions._3DSv1 && version != Versions._3DSv2 && version != Versions._PS3v1)
                            result = false;
                    }
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        public void Load(StreamInfo input, BaseReadOnlyDirectoryNode fs)
        {
            _gfd = new GFDv1(input);
        }

        public void Save(StreamInfo output, PhysicalDirectoryNode fs, int versionIndex = 0)
        {
            _gfd.Save(output);
        }

        public void Dispose()
        {
            foreach (var tex in Textures)
                tex.Dispose();
        }
    }
}
