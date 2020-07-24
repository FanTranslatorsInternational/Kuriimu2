using System;
using System.Collections.Generic;
using System.Text;
using Kontract.Models.IO;

namespace Kuriimu2.WinForms.MainForms.Models
{
    class ReplaceContext
    {
        public bool IsSuccessful { get; set; } = true;

        public string Error { get; set; }

        public UPath ReplacePath { get; set; }

        public UPath DirectoryPath { get; set; }
    }
}
