using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Intermediate
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
