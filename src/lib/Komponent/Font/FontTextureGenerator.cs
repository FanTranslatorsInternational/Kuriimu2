﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Komponent.Font
{
    /// <summary>
    /// Generates textures out of a given list of glyphs.
    /// </summary>
    public class FontTextureGenerator
    {
        private readonly BinPacker _binPacker;

        /// <summary>
        /// The size of the canvas to draw on.
        /// </summary>
        public Size CanvasSize { get; }

        /// <summary>
        /// Creates a new instance of <see cref="FontTextureGenerator"/>.
        /// </summary>
        /// <param name="canvasSize">The size of the canvas to draw on.</param>
        /// <param name="margin">The margin to the top and left side of each texture.</param>
        public FontTextureGenerator(Size canvasSize, int margin = 1)
        {
            CanvasSize = canvasSize;
            _binPacker = new BinPacker(canvasSize, margin);
        }

        /// <summary>
        /// Generate font textures for the given glyphs.
        /// </summary>
        /// <param name="adjustedGlyphs">The enumeration of adjusted glyphs.</param>
        /// <param name="textureCount">The maximum texture count.</param>
        /// <returns>The generated textures.</returns>
        public IList<FontTextureInfo> GenerateFontTextures(IList<AdjustedGlyph> adjustedGlyphs, int textureCount = -1)
        {
            var fontTextures = new List<FontTextureInfo>(textureCount >= 0 ? textureCount : 0);

            var remainingAdjustedGlyphs = adjustedGlyphs;
            while (remainingAdjustedGlyphs.Count > 0)
            {
                // Stop if the texture limit is reached
                if (textureCount > 0 && fontTextures.Count >= textureCount)
                    break;

                // Create new font texture to draw on.
                var fontTexture = new Bitmap(CanvasSize.Width, CanvasSize.Height);
                var fontGraphics = Graphics.FromImage(fontTexture);

                // Draw each positioned glyph on the font texture
                var handledGlyphs = new List<(AdjustedGlyph, Point)>(remainingAdjustedGlyphs.Count);
                foreach (var positionedGlyph in _binPacker.Pack(remainingAdjustedGlyphs))
                {
                    DrawGlyph(fontGraphics, positionedGlyph);
                    handledGlyphs.Add(positionedGlyph);
                }

                fontTextures.Add(new FontTextureInfo(fontTexture, handledGlyphs.Select(x => (x.Item1.Glyph, x.Item2)).ToList()));

                // Remove every handled glyph
                remainingAdjustedGlyphs = remainingAdjustedGlyphs.Except(handledGlyphs.Select(x => x.Item1)).ToList();
            }

            return fontTextures;
        }

        /// <summary>
        /// Draws a glpyh onto the font texture.
        /// </summary>
        /// <param name="fontGraphics">The font texture to draw on.</param>
        /// <param name="positionedGlyph">The adjusted glyph positioned in relation to the texture.</param>
        private void DrawGlyph(Graphics fontGraphics, (AdjustedGlyph adjustedGlyph, Point position) positionedGlyph)
        {
            var adjustedGlyph = positionedGlyph.adjustedGlyph;
            var destPoints = new[]
            {
                new PointF(positionedGlyph.position.X, positionedGlyph.position.Y),
                new PointF(
                    positionedGlyph.position.X +
                    (adjustedGlyph.WhiteSpaceAdjustment?.GlyphSize.Width ?? adjustedGlyph.Glyph.Width),
                    positionedGlyph.position.Y),
                new PointF(positionedGlyph.position.X,
                    positionedGlyph.position.Y +
                    (adjustedGlyph.WhiteSpaceAdjustment?.GlyphSize.Height ?? adjustedGlyph.Glyph.Height)),
            };

            var sourceRect = new RectangleF(
                adjustedGlyph.WhiteSpaceAdjustment?.GlyphPosition ?? Point.Empty,
                adjustedGlyph.WhiteSpaceAdjustment?.GlyphSize ?? adjustedGlyph.Glyph.Size);

            fontGraphics.DrawImage(adjustedGlyph.Glyph, destPoints, sourceRect, GraphicsUnit.Pixel);
        }
    }
}
