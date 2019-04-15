namespace Kontract.Interfaces.Image
{
    /// <summary>
    /// The base class for format information
    /// </summary>
    public class FormatInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="formatIndex"></param>
        /// <param name="formatName"></param>
        public FormatInfo(int formatIndex, string formatName)
        {
            FormatIndex = formatIndex;
            FormatName = formatName;
        }

        /// <summary>
        /// The unique index into a format list, specific to the adapter
        /// </summary>
        public int FormatIndex { get; }

        /// <summary>
        /// The name of the format used; Doesn't need to be unique
        /// </summary>
        public string FormatName { get; }
    }
}
