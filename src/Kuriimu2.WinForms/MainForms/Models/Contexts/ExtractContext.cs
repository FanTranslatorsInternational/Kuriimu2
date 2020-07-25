using Kontract.Models.IO;

namespace Kuriimu2.WinForms.MainForms.Models.Contexts
{
    class ExtractContext
    {
        public bool IsSuccessful { get; set; } = true;

        public string Error { get; set; }

        public UPath ExtractPath { get; set; }

        public int CurrentCount { get; set; }

        public int MaxCount { get; set; }
    }
}
