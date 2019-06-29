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

        public EncodingInfo(int encodingIndex, string encodingName, bool isIndexed) : this(encodingIndex, encodingName)
        {
            IsIndexed = isIndexed;
        }

        /// <summary>
        /// The unique index into a format list, specific to the adapter
        /// </summary>
        public int EncodingIndex { get; }

        /// <summary>
        /// The name of the format used
        /// </summary>
        /// <remarks>Doesn't need to be unique.</remarks>
        public string EncodingName { get; }

        /// <summary>
        /// Indicates if described encoding is an index based one.
        /// </summary>
        public bool IsIndexed { get; }
    }
}
