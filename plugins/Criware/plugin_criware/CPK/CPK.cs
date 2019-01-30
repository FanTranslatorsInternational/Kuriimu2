using System.IO;
using System.Linq;
using Komponent.IO;

namespace plugin_criware.CPK
{
    /// <summary>
    /// Format class that handles CPK archives.
    /// </summary>
    public class CPK
    {
        /// <summary>
        /// The table that stores the header data.
        /// </summary>
        public CpkTable HeaderTable { get; }

        /// <summary>
        /// The table that stores the TOC data.
        /// </summary>
        public CpkTable TocTable { get; }

        /// <summary>
        /// The relative offset from which file offsets are based.
        /// </summary>
        public long FileOffsetBase { get; }

        /// <summary>
        /// Instantiates a new <see cref="CPK"/> from an input <see cref="Stream"/>.
        /// </summary>
        /// <param name="input"></param>
        public CPK(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Read in the CPK table.
                HeaderTable = new CpkTable(input);

                // Retrieve the offset for the TOC table.
                var tocOffset = (long)(ulong)HeaderTable.Rows.First().Values["TocOffset"].Value;

                // Set the file offset base value
                FileOffsetBase = tocOffset;

                // Read in the TOC table.
                input.Position = tocOffset;
                TocTable = new CpkTable(input);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public bool Save(Stream output)
        {

            return false;
        }
    }
}
