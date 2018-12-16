using System.Drawing;
using System.IO;
using System.Linq;

namespace plugin_valkyria_chronicles.Game
{
    /// <summary>
    /// Dicer combines a bitmap and SLICE together and can draw the slices by name.
    /// </summary>
    internal sealed class Dicer
    {
        /// <summary>
        /// The source bitmap that contains slices.
        /// </summary>
        public Bitmap Bitmap { get; }

        /// <summary>
        /// The slice that is associated with the bitmap.
        /// </summary>
        public SLICE Slice { get; }

        /// <summary>
        /// Constructs a new Dicer with the given bitmap.
        /// </summary>
        /// <param name="imagePath"></param>
        public Dicer(string imagePath)
        {
            Bitmap = new Bitmap(imagePath);
            Slice = new SLICE(File.OpenRead(Path.ChangeExtension(imagePath, ".slice")));
        }

        /// <summary>
        /// Draws the specified slice onto the target graphics context.
        /// </summary>
        /// <param name="sliceName">The slice to be drawn.</param>
        /// <param name="gfx">Target graphics context.</param>
        /// <param name="x">X Position</param>
        /// <param name="y">Y Position</param>
        /// <param name="flipX">Determines if the slice is mirrored.</param>
        /// <param name="flipY">Determines if the slice is flipped.</param>
        /// <param name="stretchWidth"></param>
        /// <param name="stretchHeight"></param>
        public void DrawSlice(string sliceName, Graphics gfx, int x, int y, bool flipX = false, bool flipY = false, int stretchWidth = 0, int stretchHeight = 0)
        {
            var slice = Slice.Slices.FirstOrDefault(s => s.Name == sliceName);
            if (slice == null) return;
            var width = stretchWidth > 0 ? stretchWidth : slice.Width;
            var height = stretchHeight > 0 ? stretchHeight : slice.Height;
            gfx.DrawImage(Bitmap,
                new[] {
                    new Point(x + (flipX ? width : 0), y + (flipY ? height : 0)),
                    new Point(x + (flipX ? 0 : width), y + (flipY ? height : 0)),
                    new Point(x + (flipX ? width : 0), y + (flipY ? 0 : height))
                },
                slice.Rect,
                GraphicsUnit.Pixel
            );
        }
    }
}