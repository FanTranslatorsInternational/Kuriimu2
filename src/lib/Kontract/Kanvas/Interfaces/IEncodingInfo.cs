namespace Kontract.Kanvas.Interfaces
{
    /// <summary>
    /// And interface to define meta information for an encoding to use in the Kanvas library.
    /// </summary>
    public interface IEncodingInfo
    {
        /// <summary>
        /// The number of bits one pixel takes in the format specification.
        /// </summary>
        int BitDepth { get; }

        /// <summary>
        /// The number of bits per read/written value.
        /// </summary>
        int BitsPerValue { get; }

        /// <summary>
        /// The number of colors per read/written value.
        /// </summary>
        int ColorsPerValue { get; }

        /// <summary>
        /// The name to display for this encoding.
        /// </summary>
        string FormatName { get; }
    }
}
