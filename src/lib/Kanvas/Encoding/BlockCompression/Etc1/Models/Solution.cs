namespace Kanvas.Encoding.BlockCompression.Etc1.Models
{
    internal class Solution
    {
        public int Error { get; set; }
        public Rgb BlockColor { get; set; }
        public int[]? IntenTable { get; set; }
        public int SelectorMsb { get; set; }
        public int SelectorLsb { get; set; }
    }
}
