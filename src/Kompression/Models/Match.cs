namespace Kompression.Models
{
    /// <summary>
    /// The pattern match containing all its information.
    /// </summary>
    public struct Match
    {
        /// <summary>
        /// Gets the position at which the match is occuring.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets the length the pattern match has.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Gets the displacement from the <see cref="Position"/> at which the match begins.
        /// </summary>
        public int Displacement { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="Match"/>.
        /// </summary>
        /// <param name="position">The position at which the match is occuring.</param>
        /// <param name="displacement">The length the pattern match has.</param>
        /// <param name="length">The displacement from the <see cref="Position"/> at which the match begins.</param>
        public Match(int position, int displacement, int length)
        {
            Position = position;
            Displacement = displacement;
            Length = length;
        }
    }
}
