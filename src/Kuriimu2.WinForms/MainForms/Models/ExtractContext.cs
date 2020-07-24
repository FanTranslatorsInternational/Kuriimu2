using System;
using System.Collections.Generic;
using System.Text;
using Kontract.Models.IO;

namespace Kuriimu2.WinForms.MainForms.Models
{
    class ExtractContext
    {
        public bool IsSuccessful { get; set; } = true;

        public string Error { get; set; }

        public UPath ExtractPath { get; set; }
    }
}
