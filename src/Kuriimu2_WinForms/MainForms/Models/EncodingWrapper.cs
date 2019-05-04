using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Interfaces.Intermediate;

namespace Kuriimu2_WinForms.MainForms.Models
{
    internal class EncodingWrapper
    {
        public IColorEncodingAdapter EncodingAdapter { get; }

        public EncodingWrapper(IColorEncodingAdapter adapter)
        {
            EncodingAdapter = adapter;
        }

        public override string ToString()
        {
            return EncodingAdapter.Name;
        }
    }
}
