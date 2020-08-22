using System.Drawing;

namespace Kontract.Interfaces.Plugins.State.Intermediate
{
    /// <summary>
    /// Provides methods to retrieve a swizzled point path
    /// </summary>
    public interface IImageSwizzleAdapter : IIntermediate
    {
        /// <summary>
        /// Transforms a given point.
        /// </summary>
        /// <param name="point">Point to be transformed.</param>
        /// <returns>Transformed Point.</returns>
        Point TransformPoint(Point point);
    }
}
