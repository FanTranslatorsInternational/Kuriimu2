namespace Komponent.Utilities
{
    public static class Conversion
    {
        /// <summary>
        /// Upscale a value from <paramref name="fromBitDepth"/> to <paramref name="toBitDepth"/>.
        /// This method may not be used if <paramref name="fromBitDepth"/> higher <paramref name="toBitDepth"/>.
        /// </summary>
        /// <param name="value">The value to upscale.</param>
        /// <param name="fromBitDepth">The bit depth the value is presented in.</param>
        /// <param name="toBitDepth">The bit depth to upscale the value to.</param>
        /// <returns>The upscale value.</returns>
	    public static int UpscaleBitDepth(int value, int fromBitDepth, int toBitDepth)
	    {
		    var fromMaxRange = (1 << fromBitDepth) - 1;
		    var toMaxRange = (1 << toBitDepth) - 1;
		    return value == fromMaxRange ? toMaxRange : (value << fromBitDepth) / fromMaxRange;
	    }

        /// <summary>
        /// Upscale a value from <paramref name="fromBitDepth"/> to 8.
        /// This method may not be used for bit depths higher 8.
        /// </summary>
        /// <param name="value">The value to upscale.</param>
        /// <param name="fromBitDepth">The bit depth the value is presented in.</param>
        /// <returns>The upscale value.</returns>
	    public static int UpscaleBitDepth(int value, int fromBitDepth)
	    {
		    var fromMaxRange = (1 << fromBitDepth) - 1;
		    return value == fromMaxRange ? 255 : (value << 8) / fromMaxRange;
	    }

        /// <summary>
        /// Downscale a value from <paramref name="fromBitDepth"/> to <paramref name="toBitDepth"/>.
        /// This method may not be used if <paramref name="fromBitDepth"/> lower <paramref name="toBitDepth"/>.
        /// </summary>
        /// <param name="value">The value to downscale.</param>
        /// <param name="fromBitDepth">The bit depth the value is presented in.</param>
        /// <param name="toBitDepth">The bit depth to downscale the value to.</param>
        /// <returns>The downscale value.</returns>
	    public static int DownscaleBitDepth(int value, int fromBitDepth, int toBitDepth)
	    {
		    return value >> (fromBitDepth - toBitDepth);
	    }

        /// <summary>
        /// Downscale a value from 8 to <paramref name="toBitDepth"/>.
        /// This method may not be used for values presented with more than 8 bits.
        /// </summary>
        /// <param name="value">The value to downscale.</param>
        /// <param name="toBitDepth">The bit depth to downscale the value to.</param>
        /// <returns>The downscale value.</returns>
	    public static int DownscaleBitDepth(int value, int toBitDepth)
	    {
		    return value >> (8 - toBitDepth);
	    }
    }
}
