namespace Kontract.Models.Image
{
    /// <summary>
    /// The base class for format information
    /// </summary>
    public class EncodingInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodingIndex"></param>
        /// <param name="encodingName"></param>
        public EncodingInfo(int encodingIndex, string encodingName)
        {
            EncodingIndex = encodingIndex;
            EncodingName = encodingName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="encodingIndex"></param>
        /// <param name="encodingName"></param>
        public EncodingInfo(int encodingIndex, string encodingName, int variant)
        {
            EncodingIndex = encodingIndex;
            EncodingName = encodingName;
            Variant = variant;
        }

        /// <summary>
        /// The unique index into a format list, specific to the adapter
        /// </summary>
        public int EncodingIndex { get; }

        /// <summary>
        /// The name of the format used; Doesn't need to be unique
        /// </summary>
        public string EncodingName { get; }

        /// <summary>
        /// The variation value; Usable for distiguishing the purpose of this <see cref="EncodingInfo"/>.
        /// </summary>
        public int Variant { get; }
    }
}
