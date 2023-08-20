using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Progress;

namespace Kanvas.Extensions
{
    public static class ProgressExtensions
    {
        public static IList<IProgressContext> SplitIntoEvenScopes(this IProgressContext progress, int div)
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

        public static IList<IProgressContext> SplitIntoWeightedScopes(this IProgressContext progress,
            params long[] weights)
        {
            var totalWeight = (double)weights.Sum();

            var result = new IProgressContext[weights.Length];
            var startProgress = progress.MinPercentage;
            for (var i = 0; i < weights.Length; i++)
            {
                var percentage = weights[i] / totalWeight;
                var progressSize = (progress.MaxPercentage - progress.MinPercentage) * percentage;
                result[i] = progress.CreateScope(startProgress, startProgress + progressSize);

                startProgress += progressSize;
            }

            return result;
        }
    }
}
