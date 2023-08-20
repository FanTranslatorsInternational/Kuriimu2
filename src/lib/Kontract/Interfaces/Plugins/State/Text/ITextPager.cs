using System.Collections.Generic;
using Kontract.Models.Plugins.State.Text;

namespace Kontract.Interfaces.Plugins.State.Text
{
    /// <summary>
    /// The base interface for paging processed texts.
    /// </summary>
    public interface ITextPager
    {
        /// <summary>
        /// Splits a processed text by certain conditions into multiple processed texts.
        /// </summary>
        /// <param name="text">The text to split.</param>
        /// <returns>The split text.</returns>
        IList<ProcessedText> Split(ProcessedText text);

        /// <summary>
        /// Merges multiple processed texts by certain conditions.
        /// </summary>
        /// <param name="texts">The texts to merge.</param>
        /// <returns>The merged text.</returns>
        ProcessedText Merge(IList<ProcessedText> texts);
    }
}
