using System.Collections.Generic;
using System.Linq;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Kuriimu2.ImGui.TextParsing.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Kuriimu2.ImGui.TextParsing
{
    internal class TextRenderer
    {
        private readonly IReadOnlyList<CharacterInfo> _characters;
        private readonly RenderOptions _options;

        public TextRenderer(IReadOnlyList<CharacterInfo> characters, RenderOptions options)
        {
            _characters = characters;
            _options = options;
        }

        public void Render(Image<Rgba32> image, TextLayoutData layout)
        {
            RenderLines(image, layout.Lines);

            if (_options.DrawBoundingBoxes)
                image.Mutate(x => x.Draw(Color.Red, 1f, layout.BoundingBox));
        }

        private void RenderLines(Image<Rgba32> image, IReadOnlyList<TextLayoutLineData> lines)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                bool isLineVisible = _options.VisibleLines <= 0 || lines.Count - i <= _options.VisibleLines;

                RenderLine(image, lines[i], isLineVisible);
            }
        }

        private void RenderLine(Image<Rgba32> image, TextLayoutLineData line, bool isLineVisible)
        {
            if (_options.TextOutlineColor != Color.Transparent)
                foreach (TextLayoutCharacterData character in line.Characters)
                    RenderCharacterOutline(image, character, isLineVisible);

            foreach (TextLayoutCharacterData character in line.Characters)
            {
                RenderCharacter(image, character, isLineVisible);

                if (_options.DrawBoundingBoxes)
                    image.Mutate(x => x.Draw(Color.RebeccaPurple, 1f, character.BoundingBox));
            }

            if (_options.DrawBoundingBoxes)
                image.Mutate(x => x.Draw(Color.PaleVioletRed, 1f, line.BoundingBox));
        }

        private void RenderCharacterOutline(Image<Rgba32> image, TextLayoutCharacterData character, bool isLineVisible)
        {
            if (_options.OutlineRadius <= 0 || character.GlyphBoundingBox.Width == 0 || character.GlyphBoundingBox.Height == 0)
                return;

            DrawCharacterOutline(image, character, isLineVisible);
        }

        private void RenderCharacter(Image<Rgba32> image, TextLayoutCharacterData character, bool isLineVisible)
        {
            if (character.GlyphBoundingBox.Width == 0 || character.GlyphBoundingBox.Height == 0)
                return;

            DrawCharacter(image, character, isLineVisible);
        }

        protected virtual void DrawCharacter(Image<Rgba32> image, TextLayoutCharacterData character, bool isLineVisible)
        {
            Color textColor = _options.TextColor;

            switch (character.Character)
            {
                case FontCharacterData fontCharacter:
                    CharacterInfo? glyph = _characters.FirstOrDefault(c => c.CodePoint == fontCharacter.Character);
                    if (glyph?.Glyph == null)
                        break;

                    Image<Rgba32> resizedGlyph = glyph.Glyph.Clone(context => context.Resize(character.GlyphBoundingBox.Size));
                    image.Mutate(x => x.DrawImage(resizedGlyph, character.GlyphBoundingBox.Location, isLineVisible ? 1f : .25f));

                    break;
            }
        }

        private void DrawCharacterOutline(Image<Rgba32> image, TextLayoutCharacterData character, bool isLineVisible)
        {
            switch (character.Character)
            {
                case FontCharacterData fontCharacter:
                    CharacterInfo? glyph = _characters.FirstOrDefault(c => c.CodePoint == fontCharacter.Character);
                    if (glyph?.Glyph == null)
                        break;

                    Color centerColor = _options.TextOutlineColor;
                    if (!isLineVisible)
                        centerColor = centerColor.WithAlpha(.25f);

                    for (var y = 0; y < glyph.Glyph.Height; y++)
                    {
                        for (var x = 0; x < glyph.Glyph.Width; x++)
                        {
                            PointF centerLocation = new(character.GlyphBoundingBox.X + x, character.GlyphBoundingBox.Y + y);
                            PointF[] ellipse = new EllipsePolygon(centerLocation, _options.OutlineRadius).Points.ToArray();

                            if (glyph.Glyph[x, y].A > 0)
                                image.Mutate(z => z.FillPolygon(centerColor, ellipse));
                        }
                    }

                    break;
            }
        }
    }
}
