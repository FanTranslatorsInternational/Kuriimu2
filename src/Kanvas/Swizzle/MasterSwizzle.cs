using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Kanvas.Interface;

namespace Kanvas.Swizzle
{
    /// <summary>
    /// The MasterSwizzle on which every other swizzle should be based on.
    /// </summary>
    public class MasterSwizzle
    {
        private readonly IEnumerable<(int, int)> _bitFieldCoords;
        private readonly IEnumerable<(int, int)> _initPointTransformOnY;

        private readonly int _widthInTiles;
        private readonly Point _init;

        /// <summary>
        /// Width of the macro tile.
        /// </summary>
        public int MacroTileWidth { get; }

        /// <summary>
        /// Height of the macro tile.
        /// </summary>
        public int MacroTileHeight { get; }

        /// <summary>
        /// Creates an instance of MasterSwizzle.
        /// </summary>
        /// <param name="imageStride">Pixel count of dimension in which should get aligned.</param>
        /// <param name="init">The initial point, where the swizzle begins.</param>
        /// <param name="bitFieldCoords">Array of coordinates, assigned to every bit in the macroTile.</param>
        /// <param name="initPointTransformOnY">Defines a transformation array of the initial point with changing Y.</param>
        public MasterSwizzle(int imageStride, Point init, (int, int)[] bitFieldCoords, (int, int)[] initPointTransformOnY = null)
        {
            _bitFieldCoords = bitFieldCoords;
            _initPointTransformOnY = initPointTransformOnY ?? new (int, int)[0];

            _init = init;

            MacroTileWidth = bitFieldCoords.Select(p => p.Item1).Aggregate(0, (x, y) => x | y) + 1;
            MacroTileHeight = bitFieldCoords.Select(p => p.Item2).Aggregate(0, (x, y) => x | y) + 1;
            _widthInTiles = (imageStride + MacroTileWidth - 1) / MacroTileWidth;
        }

        /// <summary>
        /// Transforms a given pointCount into a point
        /// </summary>
        /// <param name="pointCount">The overall pointCount to be transformed</param>
        /// <returns>The Point, which got calculated by given settings</returns>
        public Point Get(int pointCount)
        {
            var macroTileCount = pointCount / MacroTileWidth / MacroTileHeight;
            var (macroX, macroY) = (macroTileCount % _widthInTiles, macroTileCount / _widthInTiles);

            return new[] { (macroX * MacroTileWidth, macroY * MacroTileHeight) }
                .Concat(_bitFieldCoords.Where((v, j) => (pointCount >> j) % 2 == 1))
                .Concat(_initPointTransformOnY.Where((v, j) => (macroY >> j) % 2 == 1))
                .Aggregate(_init, (a, b) => new Point(a.X ^ b.Item1, a.Y ^ b.Item2));
        }
    }
}
