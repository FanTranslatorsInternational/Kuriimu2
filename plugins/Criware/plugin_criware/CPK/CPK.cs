using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Interfaces.Archive;

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
        /// 
        /// </summary>
        private byte[] _unknownPostHeaderData;

        /// <summary>
        /// The table that stores the TOC data.
        /// </summary>
        public CpkTable TocTable { get; }

        /// <summary>
        /// The table that stores extended TOC data.
        /// </summary>
        public CpkTable ETocTable { get; }

        /// <summary>
        /// The table that stores file index data.
        /// </summary>
        public CpkTable ITocTable { get; }

        /// <summary>
        /// Gets and sets the alignment value to be used when saving the <see cref="CPK"/>.
        /// </summary>
        public short Alignment { get; set; }

        /// <summary>
        /// The relative offset from which file offsets are based.
        /// </summary>
        public long FileOffsetBase { get; }

        /// <summary>
        /// The files available in the CPK.
        /// </summary>
        public List<ArchiveFileInfo> Files { get; } = new List<ArchiveFileInfo>();

        /// <summary>
        /// Instantiates a new <see cref="CPK"/> from an input <see cref="Stream"/>.
        /// </summary>
        /// <param name="input"></param>
        public CPK(Stream input)
        {
            using (var br = new BinaryReaderX(input, true))
            {
                // Read in the CPK table.
                HeaderTable = new CpkTable(br.BaseStream);

                // Check the Magic
                if (HeaderTable.Header.Magic != "CPK ")
                    throw new FormatException("The loaded file is not a CPK archive.");

                Alignment = (short)(ushort)HeaderTable.Rows.First().Values["Align"].Value;

                // Retrieve the offsets for the other tables.
                var tocOffset = (long)(ulong)HeaderTable.Rows.First().Values["TocOffset"].Value;
                var tocSize = (long)(ulong)HeaderTable.Rows.First().Values["TocSize"].Value;

                var etocOffset = (long)(ulong)HeaderTable.Rows.First().Values["EtocOffset"].Value;
                var etocSize = (long)(ulong)HeaderTable.Rows.First().Values["EtocSize"].Value;

                var itocOffset = (long)(ulong)HeaderTable.Rows.First().Values["ItocOffset"].Value;
                var itocSize = (long)(ulong)HeaderTable.Rows.First().Values["ItocSize"].Value;

                // Read in the TOC table.
                if (tocOffset > 0)
                {
                    // Fluff
                    br.BaseStream.Position = HeaderTable.Header.PacketSize + 0x10;
                    _unknownPostHeaderData = HeaderTable.UtfObfuscation ? UtfTools.XorUtf(br.ReadBytes((int)tocOffset - (int)br.BaseStream.Position)) : br.ReadBytes((int)tocOffset - (int)br.BaseStream.Position);

                    // Set the file offset base value
                    FileOffsetBase = tocOffset;

                    br.BaseStream.Position = tocOffset;
                    TocTable = new CpkTable(new SubStream(br.BaseStream, tocOffset, tocSize));

                    // Read in the ETOC table.
                    if (etocOffset > 0)
                    {
                        br.BaseStream.Position = etocOffset;
                        ETocTable = new CpkTable(new SubStream(br.BaseStream, etocOffset, etocSize));
                    }

                    // Populate files
                    foreach (var row in TocTable.Rows)
                    {
                        var dir = ((string)row["DirName"].Value).Replace("/", "\\");
                        var name = (string)row["FileName"].Value;
                        var offset = FileOffsetBase + Convert.ToInt64(row["FileOffset"].Value);
                        var compressedSize = Convert.ToInt32(row["FileSize"].Value);
                        var fileSize = Convert.ToInt32(row["ExtractSize"].Value);

                        Files.Add(new CpkFileInfo(new SubStream(br.BaseStream, offset, compressedSize), TocTable.UtfObfuscation, fileSize, compressedSize)
                        {
                            FileName = Path.Combine(dir, name),
                            State = ArchiveFileState.Archived
                        });
                    }
                }
                // Read in the ITOC table.
                else if (itocOffset > 0)
                {
                    // Fluff
                    br.BaseStream.Position = HeaderTable.Header.PacketSize + 0x10;
                    _unknownPostHeaderData = HeaderTable.UtfObfuscation ? UtfTools.XorUtf(br.ReadBytes((int)itocOffset - (int)br.BaseStream.Position)) : br.ReadBytes((int)itocOffset - (int)br.BaseStream.Position);

                    // Set the file offset base value
                    FileOffsetBase = itocOffset;

                    br.BaseStream.Position = itocOffset;
                    ITocTable = new CpkTable(new SubStream(br.BaseStream, itocOffset, itocSize));
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
            using (var bw = new BinaryWriterX(output, true))
            {
                HeaderTable.UtfObfuscation = false;
                HeaderTable.Save(bw.BaseStream);

                if (TocTable != null)
                {
                    bw.Write(_unknownPostHeaderData);

                    //output.Position = (long)(ulong)HeaderTable.Rows.First().Values["TocOffset"].Value;
                    TocTable.UtfObfuscation = false;
                    TocTable.Save(bw.BaseStream);
                }
                else if (ITocTable != null)
                {
                    bw.Write(_unknownPostHeaderData);

                    ITocTable.UtfObfuscation = false;
                    ITocTable.Save(bw.BaseStream);
                }
            }

            return true;
        }
    }
}
