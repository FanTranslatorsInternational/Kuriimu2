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

                        Files.Add(new CpkFileInfo(new SubStream(br.BaseStream, offset, compressedSize), row, TocTable.UtfObfuscation, fileSize, compressedSize)
                        {
                            FileName = Path.Combine(dir, name),
                            State = ArchiveFileState.Archived
                        });
                    }

                    // Sort files by offset
                    Files.Sort((a, b) => Convert.ToInt32(((CpkFileInfo)a).Row["FileOffset"].Value).CompareTo(Convert.ToInt32(((CpkFileInfo)b).Row["FileOffset"].Value)));
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
        /// Writes the <see cref="CPK"/> data to the output stream.
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public bool Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output, true))
            {
                // TOC
                if (TocTable != null)
                {
                    // Files
                    var contentOffset = Convert.ToInt64(HeaderTable.Rows.First()["ContentOffset"].Value);
                    bw.BaseStream.Position = contentOffset;
                    foreach (var afi in Files)
                    {
                        var cfi = (CpkFileInfo)afi;

                        // Update the file offset.
                        cfi.Row["FileOffset"].Value = (ulong)(bw.BaseStream.Position - FileOffsetBase);

                        // Save the file.
                        cfi.SaveFile(bw.BaseStream);

                        // Update the sizes.
                        cfi.Row["FileSize"].Value = (uint)cfi.CompressedLength;
                        cfi.Row["ExtractSize"].Value = (uint)cfi.FileLength;

                        // Align for the next file (except on the last file).
                        if (afi != Files.Last())
                            bw.WriteAlignment(Alignment);
                    }

                    // Update content size
                    var remainder = Alignment - bw.BaseStream.Position % Alignment;
                    HeaderTable.Rows.First()["ContentSize"].Value = (ulong)(bw.BaseStream.Position + remainder - contentOffset);

                    // TOC
                    bw.BaseStream.Position = (long)(ulong)HeaderTable.Rows.First().Values["TocOffset"].Value;
                    TocTable.UtfObfuscation = false;
                    TocTable.Save(bw.BaseStream);

                    if (ETocTable != null)
                    {
                        // TODO: Support for ETOC saving.
                    }
                }
                else if (ITocTable != null)
                {
                    bw.Write(_unknownPostHeaderData);

                    ITocTable.UtfObfuscation = false;
                    ITocTable.Save(bw.BaseStream);
                }

                // Header
                bw.BaseStream.Position = 0;
                HeaderTable.UtfObfuscation = false;
                HeaderTable.Save(bw.BaseStream);

                // Unknown block
                // TODO: Figure out how to handle this block as some files obfuscate almost the whole thing
                bw.Write(_unknownPostHeaderData);
            }

            return true;
        }
    }
}
