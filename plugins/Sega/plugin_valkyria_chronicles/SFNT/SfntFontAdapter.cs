using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;

namespace plugin_valkyria_chronicles.SFNT
{
    [Export(typeof(SfntFontAdapter))]
    [Export(typeof(IFontAdapter2))]
    [Export(typeof(IFontRenderer))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [PluginInfo("E18AD60F-A8C0-4A6E-A903-C165AFE417E1", "VC-SFNT Font", "SFNT", "IcySon55", "", "This is the SFNT font adapter for Kuriimu2.")]
    [PluginExtensionInfo("*.bf1")]
    public sealed class SfntFontAdapter : IFontRenderer, IIdentifyFiles, ILoadFiles
    {
        private SFNT _format;

        #region Font Properties

        public IEnumerable<FontCharacter2> Characters
        {
            get => _format.Characters;
            set => _format.Characters = value.ToList();
        }

        public float Baseline { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public float DescentLine { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion

        #region Font Rendering

        private readonly ImageAttributes _charAttributes = new ImageAttributes();

        public CharWidthInfo GetCharWidthInfo(char c)
        {
            return Characters.FirstOrDefault(chr => chr.Character == c)?.WidthInfo ?? Characters.FirstOrDefault(chr => chr.Character == '?')?.WidthInfo ?? new CharWidthInfo();
        }

        public float MeasureString(string text, char stopChar, float scale = 1)
        {
            if (text.Length == 0) return 0;

            var width = 0f;
            foreach (var c in text)
            {
                width += GetCharWidthInfo(c).GlyphWidth * scale;
                if (c == stopChar)
                    break;
            }

            return width;
        }

        public void SetColor(Color color)
        {
            _charAttributes.SetColorMatrix(new ColorMatrix(new[]
            {
                new[] { color.R / 255f, color.G / 255f, color.B / 255f, 1f, 0 },
                new[] { 0, 0f, 0, 0, 0 },
                new[] { 0, 0, 0f, 0, 0 },
                new[] { 0, 0, 0, 0f, 0 },
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

        public bool Identify(string filename)
        {
            try
            {
                using (var br = new BinaryReaderX(File.OpenRead(filename)))
                    return br.PeekString() == "SFNT";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Load(string filename)
        {
            if (File.Exists(filename))
            {
                _format = new SFNT(File.OpenRead(filename));
            }
        }

        public void Save(string filename, int versionIndex = 0)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}
