namespace Kompression.PatternMatch.LempelZiv.Support
{
    /// <summary>
    /// The element model used for <see cref="NewOptimalParser"/> to store price and connects it to a displacement and length
    /// </summary>
    class PriceHistoryElement
    {
        /// <summary>
        /// Determines if an element represents a literal.
        /// </summary>
        public bool IsLiteral { get; set; }

        /// <summary>
        /// The price for this element.
        /// </summary>
        public long Price { get; set; } = -1;

        /// <summary>
        /// The displacement for this element.
        /// </summary>
        public long Displacement { get; set; }

        /// <summary>
        /// The length for this element.
        /// </summary>
        public long Length { get; set; }
    }
}
