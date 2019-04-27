namespace Kanvas.Encoding.Support.ETC1.Models
{
    internal class Solution
    {
        public int Error { get; set; }
        public RGB BlockColor { get; set; }
        public int[] IntenTable { get; set; }
        public int SelectorMSB { get; set; }
        public int SelectorLSB { get; set; }
    }
}
