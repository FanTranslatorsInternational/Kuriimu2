namespace Kompression.Contract.DataClasses.Encoder.LempelZiv
{
    /// <summary>
    /// The pattern match containing all its information.
    /// </summary>
    public class LempelZivMatch(int position, int displacement, int length)
    {
        /// <summary>
        /// The position at which the match was found.
        /// </summary>
        public int Position { get; private set; } = position;

        /// <summary>
        /// Gets the length the pattern match has.
        /// </summary>
        public int Length { get; } = length;

        /// <summary>
        /// Gets the displacement from the position at which the match begins.
        /// </summary>
        public int Displacement { get; } = displacement;

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
