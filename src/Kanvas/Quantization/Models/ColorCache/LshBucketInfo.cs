using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Models.ColorCache
{
    class LshBucketInfo
    {
        public SortedDictionary<int, Color> Colors;

        /// <summary>
        /// Initializes a new instance of the <see cref="LshBucketInfo"/> class.
        /// </summary>
        public LshBucketInfo()
        {
            Colors = new SortedDictionary<int, Color>();
        }

        /// <summary>
        /// Adds the color to the bucket information.
        /// </summary>
        /// <param name="paletteIndex">Index of the palette.</param>
        /// <param name="color">The color.</param>
        public void AddColor(int paletteIndex, Color color)
        {
            Colors.Add(paletteIndex, color);
        }
    }
}
