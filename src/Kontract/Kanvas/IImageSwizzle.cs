using System.Drawing;

namespace Kontract.Kanvas
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
        /// Transforms a given point according to the swizzle.
        /// </summary>
        /// <param name="point">Point to transform.</param>
        /// <returns>Transformed point.</returns>
        Point Transform(Point point);
    }
}
