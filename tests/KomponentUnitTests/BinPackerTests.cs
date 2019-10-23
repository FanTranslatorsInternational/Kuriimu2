using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FluentAssertions;
using Komponent.Font;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KomponentUnitTests
{
    [TestClass]
    public class BinPackerTests
    {
        [TestMethod]
        public void Pack_AdjustedGlyphs_NotAll()
        {
            // Arrange
            var glyphs = new List<Bitmap>();
            glyphs.Add(new Bitmap(5, 5));
            glyphs[0].SetPixel(2, 2, Color.White);
            glyphs[0].SetPixel(2, 1, Color.White);
            glyphs.Add(new Bitmap(5, 5));
            glyphs[1].SetPixel(2, 1, Color.White);
            glyphs[1].SetPixel(2, 2, Color.White);
            glyphs[1].SetPixel(2, 3, Color.White);
            glyphs[1].SetPixel(1, 2, Color.White);
            glyphs[1].SetPixel(3, 2, Color.White);
            var adjustments = FontMeasurement.MeasureWhiteSpace(glyphs);
            var binPacker = new BinPacker(new Size(3, 4));

            // Act
            var boxes = binPacker.Pack(glyphs, adjustments).ToArray();

            // Assert
            boxes.Length.Should().Be(1);
            boxes[0].position.Should().Be(Point.Empty);
        }

        [TestMethod]
        public void Pack_AdjustedGlyphs_All()
        {
            // Arrange
            var glyphs = new List<Bitmap>();
            glyphs.Add(new Bitmap(5, 5));
            glyphs[0].SetPixel(2, 1, Color.White);
            glyphs[0].SetPixel(2, 2, Color.White);
            glyphs[0].SetPixel(2, 3, Color.White);
            glyphs[0].SetPixel(1, 2, Color.White);
            glyphs[0].SetPixel(3, 2, Color.White);
            glyphs.Add(new Bitmap(5, 5));
            glyphs[1].SetPixel(2, 2, Color.White);
            glyphs[1].SetPixel(2, 1, Color.White);
            glyphs[1].SetPixel(3, 2, Color.White);
            glyphs.Add(new Bitmap(2, 2));
            glyphs[2].SetPixel(0, 0, Color.White);
            glyphs[2].SetPixel(1, 0, Color.White);
            var adjustments = FontMeasurement.MeasureWhiteSpace(glyphs);
            var binPacker = new BinPacker(new Size(5, 3));

            // Act
            var boxes = binPacker.Pack(glyphs,adjustments).ToArray();

            // Assert
            boxes.Length.Should().Be(3);
            boxes[0].position.Should().Be(Point.Empty);
            boxes[1].position.Should().Be(new Point(3, 0));
            boxes[2].position.Should().Be(new Point(3, 2));
        }
    }
}
