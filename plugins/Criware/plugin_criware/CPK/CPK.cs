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
        /// Instantiates a new <see cref="CPK"/> from an input <see cref="Stream"/>.
        /// </summary>
        /// <param name="input"></param>
        public CPK(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Read in the CPK table.
                HeaderTable = new CpkTable(input);

                var tocOffset = (ulong)HeaderTable.Rows.First().Values["TocOffset"].Value;
                var contentOffset = HeaderTable.Rows.First().Values["ContentOffset"].Value;

                // Read in the TOC table.
                input.Position = (long)tocOffset;
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
