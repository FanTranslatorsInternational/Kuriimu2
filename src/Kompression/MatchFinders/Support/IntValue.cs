namespace Kompression.MatchFinders.Support
{
    /// <summary>
    /// The integer primitive boxed as a reference type.
    /// </summary>
    class IntValue
    {
        /// <summary>
        /// Gets or sets the boxed integer.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="IntValue"/>.
        /// </summary>
        /// <param name="value">The value to box.</param>
        public IntValue(int value)
        {
            Value = value;
        }
    }
}
