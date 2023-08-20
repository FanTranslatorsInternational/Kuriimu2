using System.Collections.Generic;
using Kontract.Interfaces.Progress;

namespace Kanvas.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> AttachProgress<T>(this IEnumerable<T> input, ISetMaxProgressContext progress) =>
            input.AttachProgress(progress, string.Empty, 1);

        public static IEnumerable<T> AttachProgress<T>(this IEnumerable<T> input, ISetMaxProgressContext progress, string preText) =>
            input.AttachProgress(progress, preText, 1);

        public static IEnumerable<T> AttachProgress<T>(this IEnumerable<T> input, ISetMaxProgressContext progress, int step) =>
            input.AttachProgress(progress, string.Empty, step);

        public static IEnumerable<T> AttachProgress<T>(this IEnumerable<T> input, ISetMaxProgressContext progress, string preText, int step) =>
            progress == null ? input : input.AttachProcessInternal(progress, preText, step);

        private static IEnumerable<T> AttachProcessInternal<T>(this IEnumerable<T> input, ISetMaxProgressContext progress, string preText, int step)
        {
            preText = string.IsNullOrWhiteSpace(preText) ? string.Empty : preText + " ";

            var progressCount = step;
            foreach (var element in input)
            {
                yield return element;

                progress?.ReportProgress($"{preText}{progressCount}/{progress.MaxValue}", progressCount);
                progressCount += step;
            }
        }
    }
}
