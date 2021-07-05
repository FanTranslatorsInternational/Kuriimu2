using System.Collections.Generic;
using System.Linq;

namespace Kontract.Models.Text.TextPager
{
    class DefaultTextPager : ITextPager
    {
        public IList<ProcessedText> Split(ProcessedText text)
        {
            return new List<ProcessedText>{text};
        }

        public ProcessedText Merge(IList<ProcessedText> texts)
        {
            return texts.FirstOrDefault();
        }
    }
}
