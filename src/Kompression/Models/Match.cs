namespace Kompression.Models
{
    /// <summary>
    /// The pattern match containing all its information.
    /// </summary>
    public class Match
    {
        /// <summary>
        /// Gets the position at which the match is occuring.
        /// </summary>
        public long Position { get; set; }

        /// <summary>
        /// Gets the length the pattern match has.
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Gets the displacement from the <see cref="Position"/> at which the match begins.
        /// </summary>
        public long Displacement { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="Match"/>.
        /// </summary>
        /// <param name="position">The position at which the match is occuring.</param>
        /// <param name="displacement">The length the pattern match has.</param>
        /// <param name="length">The displacement from the <see cref="Position"/> at which the match begins.</param>
        public Match(long position, long displacement, long length)
        {
            Position = position;
            Displacement = displacement;
            Length = length;
        }
    }
}
