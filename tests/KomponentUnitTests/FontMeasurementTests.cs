using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FluentAssertions;
using Komponent.Font;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KomponentUnitTests
{
    [TestClass]
    public class FontMeasurementTests
    {
        [TestMethod]
        public void Measure_WhiteSpace_Works()
        {
            // Arrange
            var glyphs = new List<Bitmap>();
            glyphs.Add(new Bitmap(5, 5));
            glyphs[0].SetPixel(2, 2, Color.White);
            glyphs.Add(new Bitmap(5, 5));
            glyphs[1].SetPixel(2, 2, Color.White);
            glyphs[1].SetPixel(2, 1, Color.White);

            // Act
            var adjustedGlyphs = FontMeasurement.MeasureWhiteSpace(glyphs).ToArray();

            // Assert
            adjustedGlyphs.Length.Should().Be(2);
            adjustedGlyphs[0].WhiteSpaceAdjustment.GlyphSize.Should().Be(new Size(1, 1));
            adjustedGlyphs[1].WhiteSpaceAdjustment.GlyphSize.Should().Be(new Size(1, 2));
            adjustedGlyphs[0].WhiteSpaceAdjustment.GlyphPosition.Should().Be(new Point(2, 2));
            adjustedGlyphs[1].WhiteSpaceAdjustment.GlyphPosition.Should().Be(new Point(2, 1));
        }
    }
}
