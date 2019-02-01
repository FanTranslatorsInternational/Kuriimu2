using System;
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
        /// The table that stores the CPK header data.
        /// </summary>
        public CpkTable HeaderTable { get; }

        /// <summary>
        /// Gets and sets the alignment value to be used when saving the <see cref="CPK"/>.
        /// </summary>
        public short Alignment { get; set; }

        /// <summary>
        /// The relative offset from which file offsets are based.
        /// </summary>
        public long FileOffsetBase { get; }

        /// <summary>
        /// The table that stores the TOC data.
        /// </summary>
        public CpkTable TocTable { get; }

        /// <summary>
        /// The table that stored extended TOC data.
        /// </summary>
        public CpkTable ETocTable { get; }

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
                Alignment = (short)(ushort)HeaderTable.Rows.First().Values["Align"].Value;

                // Retrieve the offsets for the other tables.
                var tocOffset = (long)(ulong)HeaderTable.Rows.First().Values["TocOffset"].Value;
                var etocOffset = (long)(ulong)HeaderTable.Rows.First().Values["EtocOffset"].Value;

                // Set the file offset base value
                FileOffsetBase = tocOffset;

                // Read in the TOC table.
                if (tocOffset > 0)
                {
                    input.Position = tocOffset;
                    TocTable = new CpkTable(input);
                }

                // Read in the ETOC table.
                if (etocOffset > 0)
                {
                    input.Position = etocOffset;
                    ETocTable = new CpkTable(input);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public bool Save(Stream output)
        {
            throw new NotImplementedException();
        }
    }
}
