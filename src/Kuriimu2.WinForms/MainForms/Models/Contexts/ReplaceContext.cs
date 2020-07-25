using Kontract.Models.IO;

namespace Kuriimu2.WinForms.MainForms.Models.Contexts
{
    class ReplaceContext : CountContext
    {
        public bool IsSuccessful { get; set; } = true;

        public string Error { get; set; }

        public UPath ReplacePath { get; set; }

        public UPath DirectoryPath { get; set; }

        public int MaxCount { get; set; }

        public int CurrentCount { get; set; }
    }
}
