using System;

namespace Kanvas.Encoding.BlockCompressions.BCn
{
    [Serializable]
    public class BCnImage<T>
        where T : struct
    {
        private int width, height;
        private T[] blocks;

        /// <summary>
        /// Gets the image's width in pixels.
        /// </summary>
        public int Width { get { return width; } }
        /// <summary>
        /// Gets the image's height in pixels.
        /// </summary>
        public int Height { get { return height; } }

        /// <summary>
        /// Gets the image's width in terms of horizontal blocks.
        /// </summary>
        /// <remarks>
        /// Note that the image size is rounded up to the nearest block when
        /// computing this value.
        /// </remarks>
        public int WidthInBlocks { get { return (width + blockWidth - 1) / blockWidth; } }

        /// <summary>
        /// Gets the image's height in terms of vertical blocks.
        /// </summary>
        /// <remarks>
        /// Note that the image size is rounded up to the nearest block when
        /// computing this value.
        /// </remarks>
        public int HeightInBlocks { get { return (height + blockHeight - 1) / blockHeight; } }

        /// <summary>
        /// Gets the number of blocks in the image.
        /// </summary>
        public int BlockCount { get { return blocks.Length; } }

        /// <summary>
        /// Initializes a new BCnImage.
        /// </summary>
        /// <param name="width">The image's width in pixels.</param>
        /// <param name="height">The image's height in pixels.</param>
        public BCnImage(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height");

            this.width = width;
            this.height = height;

            blocks = new T[WidthInBlocks * HeightInBlocks];
        }

        /// <summary>
        /// Gets the underlying storage for the image's blocks.
        /// </summary>
        public T[] GetBlockStorage()
        {
            return blocks;
        }

        /// <summary>
        /// Gets or sets an individual block.
        /// </summary>
        /// <param name="xBlock">The block's x coordinate.</param>
        /// <param name="yBlock">The block's y coordinate.</param>
        /// <remarks>
        /// The coordinates are in blocks, not pixels.
        /// </remarks>
        public T this[int xBlock, int yBlock]
        {
            get
            {
                if (xBlock < 0 || xBlock >= WidthInBlocks)
                    throw new ArgumentOutOfRangeException("xBlock");
                if (yBlock < 0 || yBlock >= HeightInBlocks)
                    throw new ArgumentOutOfRangeException("yBlock");

                return blocks[yBlock * WidthInBlocks + xBlock];
            }

            set
            {
                if (xBlock < 0 || xBlock >= WidthInBlocks)
                    throw new ArgumentOutOfRangeException("xBlock");
                if (yBlock < 0 || yBlock >= HeightInBlocks)
                    throw new ArgumentOutOfRangeException("yBlock");

                blocks[yBlock * WidthInBlocks + xBlock] = value;
            }
        }

        private static readonly int blockWidth, blockHeight;

        /// <summary>
        /// Gets the width of a single block in pixels.
        /// </summary>
        public static int BlockWidth { get { return blockWidth; } }
        /// <summary>
        /// Gets the height of a single block in pixels.
        /// </summary>
        public static int BlockHeight { get { return blockHeight; } }

        static BCnImage()
        {
            //handle other cases here as they arise
            blockWidth = blockHeight = 4;
        }
    }
}
