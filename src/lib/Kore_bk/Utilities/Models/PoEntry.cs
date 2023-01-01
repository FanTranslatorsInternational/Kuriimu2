using System.Collections.Generic;

namespace Kore.Utilities.Models
{
    public class PoEntry
    {
        public IList<string> ExtractedComments { get; private set; } = new List<string>();
        public IList<string> Comments { get; } = new List<string>();
        public IList<string> Flags { get; } = new List<string>();
        public IList<string> SourceReference { get; private set; } = new List<string>();

        public string Context { get; set; }
        public string OriginalText { get; set; }
        public string EditedText { get; set; }

        //public static explicit operator TextEntry(PoEntry e) => new TextEntry
        //{
        //    OriginalText = e.OriginalText,
        //    EditedText = e.EditedText,
        //    Notes = string.Join(Environment.NewLine, new[] { e.Context }.Concat(e.ExtractedComments)),
        //    Name = string.Join(",", e.SourceReference)
        //};

        //public static explicit operator PoEntry(TextEntry e) => new PoEntry
        //{
        //    OriginalText = e.OriginalText,
        //    EditedText = e.EditedText,
        //    Context = e.Notes.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(),
        //    ExtractedComments = e.Notes.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray(),
        //    SourceReference = e.Name.Split(',')
        //};
    }
}
