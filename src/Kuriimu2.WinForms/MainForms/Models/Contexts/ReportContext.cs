using System;
using System.Collections.Generic;
using System.Text;

namespace Kuriimu2.WinForms.MainForms.Models.Contexts
{
    class ReportContext : CountContext
    {
        public bool IsSuccessful { get; set; } = true;

        public string Error { get; set; }
    }
}
