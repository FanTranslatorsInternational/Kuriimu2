namespace Kontract.Kompression.Model.PatternMatch
{
    /// <summary>
    /// The pattern match containing all its information.
    /// </summary>
    public class Match
    {
        /// <summary>
        /// The position at which the match was found.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// Gets the length the pattern match has.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the displacement from the position at which the match begins.
        /// </summary>
        public int Displacement { get; }

        /// <summary>
        /// Creates a new instance of <see cref="Match"/>.
        /// </summary>
        /// <param name="position">The position at which the match was found.</param>
        /// <param name="displacement">The length the pattern match has.</param>
        /// <param name="length">The displacement from the position at which the match begins.</param>
        public Match(int position, int displacement, int length)
        {
            Position = position;
            Displacement = displacement;
            Length = length;
        }

        /// <summary>
        /// Resets the position to a bew value.
        /// </summary>
        /// <param name="newPosition">The new position value.</param>
        public void SetPosition(int newPosition)
        {
            Position = newPosition;
        }
    }
}
