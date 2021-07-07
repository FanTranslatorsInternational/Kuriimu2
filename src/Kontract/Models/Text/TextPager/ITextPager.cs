using System;
using System.Collections.Generic;
using System.Text;

namespace Kontract.Models.Text.TextPager
{
    public interface ITextPager
    {
        IList<ProcessedText> Split(ProcessedText text);

        ProcessedText Merge(IList<ProcessedText> texts);
    }
}
