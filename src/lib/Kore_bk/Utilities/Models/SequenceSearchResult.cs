using System.IO;

namespace Kore.Utilities.Models
{
    public class SequenceSearchResult
    {
        public string FileName { get; }

        public int Offset { get; }

        public SequenceSearchResult(string fileName, int offset)
        {
            FileName = fileName;
            Offset = offset;
        }

        public override string ToString()
        {
            return $"{Path.GetFileName(FileName)} - {Offset} (0x{Offset:X2})";
        }
    }
}
