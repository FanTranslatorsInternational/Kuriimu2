using System;

namespace Kanvas.Encoding.BlockCompressions.BCn
{
    /// <summary>
    /// Holds floating-point image data.
    /// </summary>
    [Serializable]
    public class FloatImage :
        System.Runtime.Serialization.ISerializable
    {
        private int width, height;

        /// <summary>
        /// Gets the image's width.
        /// </summary>
        public int Width { get { return width; } }
        /// <summary>
        /// Gets the image's height.
        /// </summary>
        public int Height { get { return height; } }

        /// <summary>
        /// Gets the number of channels in the image.
        /// </summary>
        public int ChannelCount { get { return data.Length; } }

        /// <summary>
        /// Gets the stride of each row in the image.
        /// </summary>
        public int Stride { get { return width; } }

        private float[][] data;

        /// <summary>
        /// Initializes a new <c>FloatImage</c>.
        /// </summary>
        /// <param name="width">The image's width.</param>
        /// <param name="height">The image's height.</param>
        /// <param name="channelCount">The number of channels in the image.</param>
        public FloatImage(int width, int height, int channelCount = 4)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height");
            if (channelCount <= 0)
                throw new ArgumentOutOfRangeException("channelCount");

            this.width = width;
            this.height = height;

            data = new float[channelCount][];
            for (int i = 0; i < data.Length; i++)
                data[i] = new float[width * height];
        }

        /// <summary>
        /// Gets the array which stores a given channel's data.
        /// </summary>
        /// <param name="channel">The index of the channel.</param>
        /// <returns>The raw array storing the channel's data.</returns>
        public float[] GetChannelStorage(int channel)
        {
            if (channel < 0 || channel >= data.Length)
                throw new ArgumentOutOfRangeException("channel");

            return data[channel];
        }

        /// <summary>
        /// Gets the backing arrays for a subset of the image's channels.
        /// </summary>
        /// <param name="channelIndices">
        /// The array of indices to select. Passing a <c>null</c> array will
        /// return an array containing each channel's backing storage. An
        /// index value of <c>-1</c> can be used to return a null element in
        /// the returned array.
        /// </param>
        public float[][] GetChannelStorage(int[] channelIndices = null)
        {
            float[][] ret;

            if (channelIndices == null)
            {
                ret = new float[data.Length][];
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = data[i];
            }
            else
            {
                ret = new float[channelIndices.Length][];
                for (int i = 0; i < ret.Length; i++)
                {
                    var iChan = channelIndices[i];
                    if (iChan < -1 || iChan >= data.Length)
                        throw new ArgumentOutOfRangeException("channelIndices");
                    ret[i] = iChan != -1 ? data[iChan] : null;
                }
            }

            return ret;
        }

        /// <summary>
        /// Gets or sets a particular data element in the image.
        /// </summary>
        /// <param name="x">The element's x coordinate.</param>
        /// <param name="y">The element's y coordinate.</param>
        /// <param name="channel">The channel to read or write.</param>
        public float this[int x, int y, int channel]
        {
            get
            {
                if (x < 0 || x >= width)
                    throw new ArgumentOutOfRangeException("x");
                if (y < 0 || y >= height)
                    throw new ArgumentOutOfRangeException("y");
                if (channel < 0 || channel >= data.Length)
                    throw new ArgumentOutOfRangeException("channel");

                return data[channel][y * width + x];
            }

            set
            {
                if (x < 0 || x >= width)
                    throw new ArgumentOutOfRangeException("x");
                if (y < 0 || y >= height)
                    throw new ArgumentOutOfRangeException("y");
                if (channel < 0 || channel >= data.Length)
                    throw new ArgumentOutOfRangeException("channel");

                data[channel][y * width + x] = value;
            }
        }

        #region ISerializable Members

        private FloatImage(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            width = info.GetInt32("width");
            data = (float[][])info.GetValue("data", typeof(float[][]));
            height = data[0].Length / width;
        }

        void System.Runtime.Serialization.ISerializable.GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            info.AddValue("width", width);
            info.AddValue("data", data);
        }

        #endregion
    }
}
