using System;
using System.Collections.Generic;
using System.Drawing;

namespace Kanvas.Interface
{
    public interface IPathProvider
    {
        /// <summary>
        /// Retrieves the path throughout the image to determine the order in which pixels will be scanned.
        /// </summary>
        IList<Point> GetPointPath(Int32 width, Int32 height);
    }

    public enum PathProvider
    {
        Reversed,
        Serpentive,
        Standard
    }
}
