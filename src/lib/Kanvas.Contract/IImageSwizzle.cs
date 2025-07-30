using SixLabors.ImageSharp;

namespace Kanvas.Contract
{
    /// <summary>
    /// An interface for creating a swizzle mechanism to use in the Kanvas image library.
    /// </summary>
    public interface IImageSwizzle
    {
        /// <summary>
        /// The width of the image after the swizzle is applied.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the image after the swizzle is applied.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Width of the macro tile.
        /// </summary>
        public int MacroTileWidth { get; }

        /// <summary>
        /// Height of the macro tile.
        /// </summary>
        public int MacroTileHeight { get; }

        /// <summary>
        /// Transforms a given point according to the swizzle.
        /// </summary>
        /// <param name="point">Point to transform.</param>
        /// <returns>Transformed point.</returns>
        Point Transform(Point point);

        /// <summary>
        /// Transforms a given pointCount into a point
        /// </summary>
        /// <param name="pointCount">The overall pointCount to be transformed</param>
        /// <returns>The Point, which got calculated by given settings</returns>
        public Point Get(int pointCount);
    }
}
