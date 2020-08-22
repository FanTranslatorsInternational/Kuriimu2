using System;
using Kontract.Kompression.Model;

namespace Kontract.Kompression.Configuration
{
    public interface IMatchLimitations
    {
        /// <summary>
        /// Set limitations for a previously set match finder.
        /// </summary>
        /// <param name="limitationFactory">The factory to declare limitations in which to search patterns in the previously set match finder.</param>
        /// <returns>The option object.</returns>
        IMatchAdditionalFinders WithinLimitations(Func<FindLimitations> limitationFactory);
    }
}
