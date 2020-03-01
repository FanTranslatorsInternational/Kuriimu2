using System.Collections.Generic;
using Kontract.Interfaces.Progress;

namespace Kontract.Extensions
{
    public static class ProgressExtensions
    {
        public static IList<IProgressContext> SplitIntoScopes(this IProgressContext progress, int div)
        {
            var divSize = (progress.MaxPercentage - progress.MinPercentage) / div;

            var result = new IProgressContext[div];
            for (var i = 0; i < div; i++)
            {
                result[i] = progress.CreateScope(progress.MinPercentage + i * divSize,
                    progress.MinPercentage + (i + 1) * divSize);
            }

            return result;
        }
    }
}
