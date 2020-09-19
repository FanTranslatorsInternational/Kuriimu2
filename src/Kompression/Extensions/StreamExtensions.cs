using System.IO;

namespace Kompression.Extensions
{
    /// <summary>
    /// Extensions for <see cref="Stream"/>s.
    /// </summary>
    internal static class StreamExtensions
    {
        /// <summary>
        /// Convert a whole <see cref="Stream"/> to a <see cref="T:byte[]"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to convert.</param>
        /// <returns>The <see cref="T:byte[]"/> representing the input.</returns>
        /// <remarks>Doesn't change the position in the stream.</remarks>
        public static byte[] ToArray(this Stream input)
        {
            if (input is MemoryStream ms)
                return ms.ToArray();

            var bkPos = input.Position;
            input.Position = 0;

            var buffer = new byte[input.Length];
            input.Read(buffer, 0, buffer.Length);

            input.Position = bkPos;

            return buffer;
        }
    }
}
