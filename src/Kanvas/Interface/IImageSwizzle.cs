using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace Kanvas.Interface
{
    /// <summary>
    /// An interface for creating a swizzle mechanism to use in the Kanvas image library.
    /// </summary>
    public interface IImageSwizzle
    {
        /// <summary>
        /// The Width the swizzle has to work with.
        /// </summary>
        int Width { get; }
        /// <summary>
        /// The Height the swizzle has to work with.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Transforms a given point according to the swizzle.
        /// </summary>
        /// <param name="point">Point to transform.</param>
        /// <returns>Transformed point.</returns>
        Point Get(Point point);
    }
}
