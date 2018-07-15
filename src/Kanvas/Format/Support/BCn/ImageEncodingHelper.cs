using System;
using System.Threading.Tasks;

namespace Kanvas.Support.BCn
{
    public static class ImageEncodingHelper
    {
        /// <summary>
        /// Encodes an image into a block-compressed bitmap.
        /// </summary>
        /// <typeparam name="T">
        /// The block type to encode to.
        /// </typeparam>
        /// <param name="destImage">
        /// The bitmap to encode into. Encoding
        /// will overwrite its contents.
        /// </param>
        /// <param name="encoderFactory">
        /// A factory function to create encoder objects.
        /// Encoders must be thread-safe (see remarks).
        /// </param>
        /// <param name="srcImage">
        /// The image to encode.
        /// </param>
        /// <param name="channelIndices">
        /// The image channels to pass to the encoder. If this
        /// parmeter is <c>null</c>, all of the image's channels
        /// will be passed directly. A channel index of <c>-1</c>
        /// selects a <c>null</c> channel (this can be used to pad
        /// the array for encoders that expect their input data
        /// to be at specific channel indices).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="destImage"/>, <paramref name="encoderFactory"/>,
        /// or <paramref name="srcImage"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="destImage"/> and <paramref name="srcImage"/> are
        /// not the same size.
        /// </exception>
        /// <remarks>
        /// Blocks may be encoded in parallel. This function will call
        /// <paramref name="encoderFactory"/> multiple times, creating
        /// a unique encoder for each thread. Individual encoders will
        /// only run on one thread at a time, but multiple encoder objects
        /// may execute at once. Take care with any shared encoder state.
        /// </remarks>
        public static void EncodeBlocks<T>(
            BCnImage<T> destImage,
            Func<DataEncoder<T>> encoderFactory,
            FloatImage srcImage, int[] channelIndices = null)
            where T : struct
        {
            if (encoderFactory == null)
                throw new ArgumentNullException("encoderFactory");
            if (destImage == null)
                throw new ArgumentNullException("destBitmap");
            if (srcImage == null)
                throw new ArgumentNullException("image");

            if (destImage.Width != srcImage.Width || destImage.Height != srcImage.Height)
                throw new ArgumentException("image and destBitmap must be the same size.");

            var imageWidth = srcImage.Width;
            var imageHeight = srcImage.Height;

            var blockWidth = BCnImage<T>.BlockWidth;
            var blockHeight = BCnImage<T>.BlockHeight;

            var nBlocksW = destImage.WidthInBlocks;
            var nBlocksH = destImage.HeightInBlocks;

            var nWholeBlocksW = nBlocksW;
            var nWholeBlocksH = nBlocksH;

            if (imageWidth < nBlocksW * blockWidth) nWholeBlocksW--;
            if (imageHeight < nBlocksH * blockHeight) nWholeBlocksH--;

            float[][] chanData = srcImage.GetChannelStorage(channelIndices);

            var targetData = destImage.GetBlockStorage();

            Parallel.For(0, nWholeBlocksW * nWholeBlocksH,
                 encoderFactory, (iBlock, loopState, encoder) =>
                 {
                     int y = iBlock / nWholeBlocksW;
                     int x = iBlock - y * nWholeBlocksW;

                     int srcIndex =
                         (y * blockHeight) * imageWidth +
                         (x * blockWidth);

                     targetData[y * nBlocksW + x] =
                         encoder(chanData, srcIndex, imageWidth);

                     return encoder;
                 }, _ => { });

            //the uncommon case: finish off any partial edge tiles on the main thread

            if (nWholeBlocksW != nBlocksW || nWholeBlocksH != nBlocksH)
            {
                var blockArea = blockWidth * blockHeight;

                var tmp = new float[chanData.Length][];
                for (int iChan = 0; iChan < tmp.Length; iChan++)
                    tmp[iChan] = chanData[iChan] != null ? new float[blockArea] : null;

                var encoder = encoderFactory();

                if (nWholeBlocksH < nBlocksH)
                {
                    //get the row across the bottom

                    for (int xBlock = 0; xBlock < nBlocksW; xBlock++)
                    {
                        LoadEdgeBlockData(tmp, blockWidth, blockHeight,
                            chanData, imageWidth, imageHeight,
                            xBlock, nWholeBlocksH);
                        targetData[nWholeBlocksH * nBlocksW + xBlock] =
                            encoder(tmp, 0, blockWidth);
                    }

                    //munge this so the next pass doesn't recompress the corner block
                    nBlocksH--;
                }

                if (nWholeBlocksW < nBlocksW)
                {
                    //get the column at the right

                    for (int yBlock = 0; yBlock < nBlocksH; yBlock++)
                    {
                        LoadEdgeBlockData(tmp, blockWidth, blockHeight,
                            chanData, imageWidth, imageHeight,
                            nWholeBlocksW, yBlock);
                        targetData[yBlock * nBlocksW + nWholeBlocksW] =
                            encoder(tmp, 0, blockWidth);
                    }
                }
            }
        }

        /// <summary>
        /// A block encoder function.
        /// </summary>
        /// <typeparam name="T">
        /// The output block type.
        /// </typeparam>
        /// <param name="chanData">
        /// The block data for each channel. The elements of this array
        /// correspond to the image data channels selected by the <c>channelIndices</c>
        /// parameter to <see cref="EncodeBlocks"/>.
        /// </param>
        /// <param name="index">
        /// The index in <paramref name="chanData"/> this block's data starts at.
        /// </param>
        /// <param name="pitch">
        /// The number of elements between subsequent rows of block data.
        /// </param>
        /// <returns>
        /// An encoded block structure representing the data.
        /// </returns>
        /// <remarks>
        /// Multiple instances of the encoder function may run at once,
        /// care must be taken with any shared state. See the remarks
        /// in <see cref="EncodeBlocks"/> for more.
        /// </remarks>
        public delegate T DataEncoder<T>(float[][] chanData, int index, int pitch)
            where T : struct;

        private static void LoadEdgeBlockData(
            float[][] dst, int blockWidth, int blockHeight,
            float[][] src, int imageWidth, int imageHeight,
            int blockX, int blockY)
        {
            blockX *= blockWidth;
            blockY *= blockHeight;

            for (int iChan = 0; iChan < dst.Length; iChan++)
            {
                var dstChan = dst[iChan];
                var srcChan = src[iChan];

                if (srcChan == null)
                    continue;

                int destIdx = 0;

                for (int y = 0; y < blockHeight; y++)
                {
                    int srcY = blockY + y;

                    if (srcY >= imageHeight)
                    {
                        Array.Clear(dstChan, destIdx,
                            dstChan.Length - destIdx);
                        break;
                    }

                    int yIdx = srcY * imageWidth;

                    for (int x = 0; x < 4; x++)
                    {
                        int srcX = blockX + x;
                        dstChan[destIdx++] = srcX < imageWidth ?
                            srcChan[yIdx + srcX] : 0;
                    }
                }
            }
        }
    }
}
