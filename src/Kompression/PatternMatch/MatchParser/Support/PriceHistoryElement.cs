namespace Kompression.PatternMatch.MatchParser.Support
{
    /// <summary>
    /// The element model used for <see cref="OptimalParser"/> to store price and connects it to a displacement and length
    /// </summary>
    struct PriceHistoryElement
    {
        /// <summary>
        /// Determines if an element represents a literal.
        /// </summary>
        public bool IsLiteral { get; set; }

        /// <summary>
        /// The price for this element.
        /// </summary>
        public int Price { get; set; }

        /// <summary>
        /// The displacement for this element.
        /// </summary>
        public int Displacement { get; set; }

        /// <summary>
        /// The length for this element.
        /// </summary>
        public int Length { get; set; }
    }
}
