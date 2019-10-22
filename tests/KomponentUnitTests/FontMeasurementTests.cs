using System.Collections.Generic;
using System.Drawing;
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
            var adjustments = FontMeasurement.MeasureWhiteSpace(glyphs);

            // Assert
            adjustments.Count.Should().Be(2);
            adjustments[0].GlyphSize.Should().Be(new Size(1, 1));
            adjustments[1].GlyphSize.Should().Be(new Size(1, 2));
            adjustments[0].GlyphPosition.Should().Be(new Point(2, 2));
            adjustments[1].GlyphPosition.Should().Be(new Point(2, 1));
        }
    }
}
