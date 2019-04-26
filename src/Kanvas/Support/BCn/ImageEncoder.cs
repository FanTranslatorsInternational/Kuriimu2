using System;

namespace Kanvas.Support.BCn
{
    /// <summary>
    /// A wrapper around the individual block encoders
    /// that handles an entire image at a time.
    /// </summary>
    public class ImageEncoder
    {
        /// <summary>
        /// Gets or sets whether RGB dithering is enabled.
        /// </summary>
        public bool DitherRgb { get; set; }
        /// <summary>
        /// Gets or sets whether alpha dithering is enabled.
        /// </summary>
        public bool DitherAlpha { get; set; }

        public ImageEncoder()
        {
            DitherRgb = true;
            DitherAlpha = true;
        }

        #region BC1

        /// <summary>
        /// Encodes an image into the BC1 format.
        /// </summary>
        /// <param name="image">The image to encode.</param>
        /// <param name="rChan">The channel to pull red values from.</param>
        /// <param name="gChan">The channel to pull green values from.</param>
        /// <param name="bChan">The channel to pull blue values from.</param>
        /// <returns>The encoded BC1 image data.</returns>
        public BCnImage<BC1Block> EncodeBC1(FloatImage image,
            int rChan = 0, int gChan = 1, int bChan = 2)
        {
            return InternalEncodeBC1(image, rChan, gChan, bChan, -1);
        }

        /// <summary>
        /// Encodes an image into the BC1 format.
        /// </summary>
        /// <param name="image">The image to encode.</param>
        /// <param name="rChan">The channel to pull red values from.</param>
        /// <param name="gChan">The channel to pull green values from.</param>
        /// <param name="bChan">The channel to pull blue values from.</param>
        /// <param name="aChan">The channel to pull alpha values from.</param>
        /// <returns>The encoded BC1 image data.</returns>
        public BCnImage<BC1Block> EncodeBC1A(FloatImage image,
            int rChan = 0, int gChan = 1, int bChan = 2, int aChan = 3)
        {
            if (aChan == -1)
                throw new ArgumentOutOfRangeException("aChan");
            //rest of the cases caught in InternalEncodeBC1

            return InternalEncodeBC1(image, rChan, gChan, bChan, aChan);
        }

        private BCnImage<BC1Block> InternalEncodeBC1(FloatImage image,
            int rChan, int gChan, int bChan, int aChan)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (rChan < 0 || rChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("rChan");
            if (gChan < 0 || gChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("gChan");
            if (bChan < 0 || bChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("bChan");
            if (aChan != -1 && (aChan < 0 || aChan >= image.ChannelCount))
                throw new ArgumentOutOfRangeException("aChan");

            var ret = new BCnImage<BC1Block>(image.Width, image.Height);

            ImageEncodingHelper.EncodeBlocks(ret,
                () =>
                {
                    var encBC1 = new BC1BlockEncoder();

                    encBC1.DitherRgb = DitherRgb;
                    encBC1.DitherAlpha = DitherAlpha;

                    return (chanData, index, pitch) =>
                        {
                            encBC1.LoadBlock(
                                chanData[0], index,
                                chanData[1], index,
                                chanData[2], index,
                                pitch);

                            if (chanData[3] != null)
                                encBC1.LoadAlphaMask(chanData[3], index, 0.5F, pitch);

                            return encBC1.Encode();
                        };
                },
                image, new int[] { rChan, gChan, bChan, aChan });

            return ret;
        }

        #endregion

        #region BC2

        /// <summary>
        /// Encodes an image into the BC2 format.
        /// </summary>
        /// <param name="image">The image to encode.</param>
        /// <param name="rChan">The channel to pull red values from.</param>
        /// <param name="gChan">The channel to pull green values from.</param>
        /// <param name="bChan">The channel to pull blue values from.</param>
        /// <param name="aChan">The channel to pull alpha values from.</param>
        /// <returns>The encoded BC2 image data.</returns>
        public BCnImage<BC2Block> EncodeBC2(FloatImage image,
            int rChan = 0, int gChan = 1, int bChan = 2, int aChan = 3)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (rChan < 0 || rChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("rChan");
            if (gChan < 0 || gChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("gChan");
            if (bChan < 0 || bChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("bChan");
            if (aChan < 0 || aChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("aChan");

            var ret = new BCnImage<BC2Block>(image.Width, image.Height);

            ImageEncodingHelper.EncodeBlocks(ret,
                () =>
                {
                    var encBC1 = new BC1BlockEncoder();
                    var encBC2A = new BC2ABlockEncoder();

                    encBC1.DitherRgb = DitherRgb;
                    encBC2A.Dither = DitherAlpha;

                    return (chanData, index, pitch) =>
                    {
                        encBC1.LoadBlock(
                            chanData[0], index,
                            chanData[1], index,
                            chanData[2], index,
                            pitch);
                        encBC2A.LoadBlock(chanData[3], index, pitch);

                        BC2Block block;

                        block.Rgb = encBC1.Encode();
                        block.A = encBC2A.Encode();

                        return block;
                    };
                },
                image, new int[] { rChan, gChan, bChan, aChan });

            return ret;
        }

        #endregion

        #region BC3

        /// <summary>
        /// Encodes an image into the BC3 format.
        /// </summary>
        /// <param name="image">The image to encode.</param>
        /// <param name="rChan">The channel to pull red values from.</param>
        /// <param name="gChan">The channel to pull green values from.</param>
        /// <param name="bChan">The channel to pull blue values from.</param>
        /// <param name="aChan">The channel to pull alpha values from.</param>
        /// <returns>The encoded BC3 image data.</returns>
        public BCnImage<BC3Block> EncodeBC3(FloatImage image,
            int rChan = 0, int gChan = 1, int bChan = 2, int aChan = 3)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (rChan < 0 || rChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("rChan");
            if (gChan < 0 || gChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("gChan");
            if (bChan < 0 || bChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("bChan");
            if (aChan < 0 || aChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("aChan");

            var ret = new BCnImage<BC3Block>(image.Width, image.Height);

            ImageEncodingHelper.EncodeBlocks(ret,
                () =>
                {
                    var encBC1 = new BC1BlockEncoder();
                    var encBC4 = new BC4BlockEncoder();

                    encBC1.DitherRgb = DitherRgb;

                    return (chanData, index, pitch) =>
                    {
                        encBC1.LoadBlock(
                            chanData[0], index,
                            chanData[1], index,
                            chanData[2], index,
                            pitch);
                        encBC4.LoadBlock(chanData[3], index, pitch);

                        BC3Block block;

                        block.Rgb = encBC1.Encode();
                        block.A = encBC4.EncodeUnsigned();

                        return block;
                    };
                },
                image, new int[] { rChan, gChan, bChan, aChan });

            return ret;
        }

        #endregion

        #region BC4

        /// <summary>
        /// Encodes an image into the unsigned BC4 format.
        /// </summary>
        /// <param name="image">The image to encode.</param>
        /// <param name="rChan">The channel to pull red values from.</param>
        /// <param name="gChan">The channel to pull green values from.</param>
        /// <returns>The encoded unsigned BC4 image data.</returns>
        public BCnImage<BC4UBlock> EncodeBC4U(FloatImage image,
            int rChan = 0, int gChan = 1)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (rChan < 0 || rChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("rChan");
            if (gChan < 0 || gChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("gChan");

            var ret = new BCnImage<BC4UBlock>(image.Width, image.Height);

            ImageEncodingHelper.EncodeBlocks(ret,
                () =>
                {
                    var encBC4 = new BC4BlockEncoder();

                    return (chanData, index, pitch) =>
                    {
                        encBC4.LoadBlock(chanData[0], index, pitch);
                        return encBC4.EncodeUnsigned();
                    };
                },
                image, new int[] { rChan, gChan });

            return ret;
        }

        /// <summary>
        /// Encodes an image into the signed BC5 format.
        /// </summary>
        /// <param name="image">The image to encode.</param>
        /// <param name="rChan">The channel to pull red values from.</param>
        /// <param name="gChan">The channel to pull green values from.</param>
        /// <returns>The encoded signed BC5 image data.</returns>
        public BCnImage<BC4SBlock> EncodeBC4S(FloatImage image,
            int rChan = 0, int gChan = 1)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (rChan < 0 || rChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("rChan");
            if (gChan < 0 || gChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("gChan");

            var ret = new BCnImage<BC4SBlock>(image.Width, image.Height);

            ImageEncodingHelper.EncodeBlocks(ret,
                () =>
                {
                    var encBC4 = new BC4BlockEncoder();

                    return (chanData, index, pitch) =>
                    {
                        encBC4.LoadBlock(chanData[0], index, pitch);
                        return encBC4.EncodeSigned();
                    };
                },
                image, new int[] { rChan, gChan });

            return ret;
        }

        #endregion

        #region BC5

        /// <summary>
        /// Encodes an image into the unsigned BC5 format.
        /// </summary>
        /// <param name="image">The image to encode.</param>
        /// <param name="rChan">The channel to pull red values from.</param>
        /// <param name="gChan">The channel to pull green values from.</param>
        /// <returns>The encoded unsigned BC5 image data.</returns>
        public BCnImage<BC5UBlock> EncodeBC5U(FloatImage image,
            int rChan = 0, int gChan = 1)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (rChan < 0 || rChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("rChan");
            if (gChan < 0 || gChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("gChan");

            var ret = new BCnImage<BC5UBlock>(image.Width, image.Height);

            ImageEncodingHelper.EncodeBlocks(ret,
                () =>
                {
                    var encBC4 = new BC4BlockEncoder();

                    return (chanData, index, pitch) =>
                    {
                        BC5UBlock block;

                        encBC4.LoadBlock(chanData[0], index, pitch);
                        block.R = encBC4.EncodeUnsigned();

                        encBC4.LoadBlock(chanData[1], index, pitch);
                        block.G = encBC4.EncodeUnsigned();

                        return block;
                    };
                },
                image, new int[] { rChan, gChan });

            return ret;
        }

        /// <summary>
        /// Encodes an image into the signed BC5 format.
        /// </summary>
        /// <param name="image">The image to encode.</param>
        /// <param name="rChan">The channel to pull red values from.</param>
        /// <param name="gChan">The channel to pull green values from.</param>
        /// <returns>The encoded signed BC5 image data.</returns>
        public BCnImage<BC5SBlock> EncodeBC5S(FloatImage image,
            int rChan = 0, int gChan = 1)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (rChan < 0 || rChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("rChan");
            if (gChan < 0 || gChan >= image.ChannelCount)
                throw new ArgumentOutOfRangeException("gChan");

            var ret = new BCnImage<BC5SBlock>(image.Width, image.Height);

            ImageEncodingHelper.EncodeBlocks(ret,
                () =>
                {
                    var encBC4 = new BC4BlockEncoder();

                    return (chanData, index, pitch) =>
                    {
                        BC5SBlock block;

                        encBC4.LoadBlock(chanData[0], index, pitch);
                        block.R = encBC4.EncodeSigned();

                        encBC4.LoadBlock(chanData[1], index, pitch);
                        block.G = encBC4.EncodeSigned();

                        return block;
                    };
                },
                image, new int[] { rChan, gChan });

            return ret;
        }

        #endregion
    }
}
