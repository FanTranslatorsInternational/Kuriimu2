using System;
using Kontract.Kompression.Model;

namespace Kontract.Kompression.Configuration
{
    public interface IMatchLimitations : IInternalMatchOptions
    {
        /// <summary>
        /// Sets an additional boundary to find matches in.
        /// </summary>
        /// <param name="limitationFactory">The factory to create limitations.</param>
        /// <returns>The option object.</returns>
        IAdditionalMatchFinder WithinLimitations(Func<FindLimitations> limitationFactory);
    }
}
