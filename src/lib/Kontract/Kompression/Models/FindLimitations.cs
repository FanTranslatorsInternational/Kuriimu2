namespace Kontract.Kompression.Models
{
    /// <summary>
    /// Contains information to limit the search of pattern matches.
    /// </summary>
    public class FindLimitations
    {
        /// <summary>
        /// The minimum length a pattern must have.
        /// </summary>
        public int MinLength { get; }

        /// <summary>
        /// The maximum length a pattern must have.
        /// </summary>
        public int MaxLength { get; }

        /// <summary>
        /// The minimum displacement a pattern must be found at.
        /// </summary>
        public int MinDisplacement { get; } = 1;

        /// <summary>
        /// The maximum displacement a pattern must be found at.
        /// </summary>
        public int MaxDisplacement { get; } = -1;

        /// <summary>
        /// Creates a new instance of <see cref="FindLimitations"/>.
        /// </summary>
        /// <param name="minLength">The minimum length a pattern must have.</param>
        /// <param name="maxLength">The maximum length a pattern must have.</param>
        public FindLimitations(int minLength, int maxLength)
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }

        /// <summary>
        /// Creates a new instance of <see cref="FindLimitations"/>.
        /// </summary>
        /// <param name="minLength">The minimum length a pattern must have.</param>
        /// <param name="maxLength">The maximum length a pattern must have.</param>
        /// <param name="minDisplacement">The minimum displacement a pattern must be found at.</param>
        /// <param name="maxDisplacement">The maximum displacement a pattern must be found at.</param>
        public FindLimitations(int minLength, int maxLength, int minDisplacement, int maxDisplacement) :
            this(minLength, maxLength)
        {
            MinDisplacement = minDisplacement;
            MaxDisplacement = maxDisplacement;
        }
    }
}
