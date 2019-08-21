﻿namespace Kompression.LempelZiv.Parser.Models
{
    /// <summary>
    /// The element model used for <see cref="OptimalParser"/> to store price and connects it to a displacement and length
    /// </summary>
    class PriceHistoryElement
    {
        /// <summary>
        /// The price for this element.
        /// </summary>
        public long Price { get; set; } = -1;

        /// <summary>
        /// The match associated with this element.
        /// </summary>
        public IMatch Match { get; set; }

        ///// <summary>
        ///// The displacement for this element.
        ///// </summary>
        //public long Displacement { get; set; }

        ///// <summary>
        ///// The length for this element.
        ///// </summary>
        //public long Length { get; set; }
    }
}