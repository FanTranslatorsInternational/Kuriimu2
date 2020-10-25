using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using plugin_criware.Archives.Support;

namespace plugin_criware.Archives
{
    class Cpk
    {
        private CpkTable _header;

        private CpkTable _tocTable;

        private CpkTable _itocTable;
        private CpkTable _dataLTable;
        private CpkTable _dataHTable;

        private CpkTable _etocTable;

        private int _align;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read CPK header table
            _header = CpkTable.Create(input, 0);
            var headerRow = _header.Rows[0];

            _align = headerRow.Get<int>("Align");

            // Retrieve the offsets for the other tables.
            var contentOffset = headerRow.Get<long>("ContentOffset");
            var tocOffset = headerRow.Get<long>("TocOffset");
            var itocOffset = headerRow.Get<long>("ItocOffset");
            var etocOffset = headerRow.Get<long>("EtocOffset");

            // Read tables
            if (etocOffset > 0)
                ReadEtocTable(br, etocOffset);

            if (tocOffset > 0)
                return ReadTocTable(br, tocOffset, contentOffset).ToList();

            if (itocOffset > 0)
                return ReadItocTable(br, itocOffset).ToList();

            return Array.Empty<IArchiveFileInfo>();
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var headerTableSize = _header.CalculateSize();
            long tableOffset = headerTableSize;

            // Write files and toc table
            if (_tocTable != null)
            {
                WriteTocTable(output, files, tableOffset);
                tableOffset = output.Length;
            }

            if (_itocTable != null)
            {
                WriteItocTable(output, files, tableOffset);
                tableOffset = output.Length;
            }

            if (_etocTable != null)
            {
                WriteEtocTable(output, tableOffset);
                tableOffset = output.Length;
            }

            // Write header
            _header.Write(output, 0);
        }

        #region Read tables

        private IEnumerable<IArchiveFileInfo> ReadTocTable(BinaryReaderX br, long tocOffset, long contentOffset)
        {
            // Read toc table
            _tocTable = CpkTable.Create(br.BaseStream, tocOffset);

            // Determine file offset
            var fileOffset = tocOffset;
            if (contentOffset >= 0)
                fileOffset = tocOffset < 0 ? contentOffset : Math.Min(contentOffset, tocOffset);

            // Populate files
            foreach (var row in _tocTable.Rows)
            {
                var dir = row.Get<string>("DirName");
                var name = row.Get<string>("FileName");
                var offset = fileOffset + row.Get<long>("FileOffset");
                var compSize = row.Get<int>("FileSize");
                var decompSize = row.Get<int>("ExtractSize");

                var subStream = new SubStream(br.BaseStream, offset, compSize);
                if (compSize == decompSize)
                    yield return new CpkArchiveFileInfo(subStream, UPath.Combine(dir, name).FullName, row);
                else
                    yield return new CpkArchiveFileInfo(subStream, UPath.Combine(dir, name).FullName, row, Kompression.Implementations.Compressions.Crilayla, decompSize);
            }
        }

        private IEnumerable<IArchiveFileInfo> ReadItocTable(BinaryReaderX br, long itocOffset)
        {
            // Read toc table
            _itocTable = CpkTable.Create(br.BaseStream, itocOffset);

            // Read data tables
            var itocRow = _itocTable.Rows[0];
            _dataLTable = CpkTable.Create(new MemoryStream(itocRow.Get<byte[]>("DataL")), "DataL");
            _dataHTable = CpkTable.Create(new MemoryStream(itocRow.Get<byte[]>("DataH")), "DataH");

            var fileOffset = _header.Rows[0].Get<long>("ContentOffset");

            // Populate files
            foreach (var row in _dataLTable.Rows.Concat(_dataHTable.Rows).OrderBy(x => x.Get<long>("ID")))
            {
                var id = row.Get<long>("ID");
                var compSize = row.Get<int>("FileSize");
                var decompSize = row.Get<int>("ExtractSize");

                var subStream = new SubStream(br.BaseStream, fileOffset, compSize);
                if (compSize == decompSize)
                    yield return new CpkArchiveFileInfo(subStream, $"{id:00000}.bin", row);
                else
                    yield return new CpkArchiveFileInfo(subStream, $"{id:00000}.bin", row, Kompression.Implementations.Compressions.Crilayla, decompSize);

                fileOffset = (fileOffset + compSize + (_align - 1)) & ~(_align - 1);
            }
        }

        private void ReadEtocTable(BinaryReaderX br, long etocOffset)
        {
            _etocTable = CpkTable.Create(br.BaseStream, etocOffset);
        }

        #endregion

        #region Write tables

        private void WriteTocTable(Stream output, IList<IArchiveFileInfo> files, long tableOffset)
        {
            // Update file information
            foreach (var file in files.Cast<CpkArchiveFileInfo>())
            {
                file.Row.Set("DirName", file.FilePath.ToRelative().GetDirectory().FullName);
                file.Row.Set("FileName", file.FilePath.GetName());
                file.Row.Set("ExtractSize", (int)file.FileSize);
            }

            var tocTableSize = _tocTable.CalculateSize();
            var tocOffset = CpkSupport.Align(tableOffset, _align);
            long fileOffset = CpkSupport.Align(tocOffset + tocTableSize, _align);
            long filePosition = fileOffset;

            // Write files and update remaining file information
            foreach (var file in files.Cast<CpkArchiveFileInfo>())
            {
                // Update offset
                file.Row.Set("FileOffset", filePosition - tocOffset);

                // Write file
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                while (output.Position % _align > 0)
                    output.WriteByte(0);

                filePosition = output.Position;

                // Update compressed size
                // HINT: This code allows for the scenario that the table size was calculated to have a const value for FileSize,
                // since it works on previous data. Setting FileSize before calculating the table size however, would require
                // caching all compressed files either in memory or in a temporary file.
                // Since it is very unlikely that every compressed file has either the same size or every file is not compressed,
                // we only update the FileSize here. This gains memory efficiency over a very unlikely case of wrong table size.
                file.Row.Set("FileSize", (int)writtenSize);
            }

            // Write table
            _tocTable.Write(output, tocOffset);

            // Update header
            _header.Rows[0].Set("TocOffset", (long)tocOffset);
            _header.Rows[0].Set("ContentOffset", fileOffset);
            _header.Rows[0].Set("TocSize", (long)tocTableSize);
            _header.Rows[0].Set("ContentSize", filePosition - fileOffset);
        }

        private void WriteItocTable(Stream output, IList<IArchiveFileInfo> files, long tableOffset)
        {
            // Update file information
            foreach (var file in files.Cast<CpkArchiveFileInfo>())
                file.Row.Set("ExtractSize", (int)file.FileSize);

            var tocTableSize = _itocTable.CalculateSize();
            var tocOffset = CpkSupport.Align(tableOffset, _align);
            long fileOffset = CpkSupport.Align(tocOffset + tocTableSize, _align);
            long filePosition = fileOffset;

            // Write files and update remaining file information
            foreach (var file in files.Cast<CpkArchiveFileInfo>().OrderBy(x => int.Parse(x.FilePath.GetNameWithoutExtension())))
            {
                // Write file
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                while (output.Position % _align > 0)
                    output.WriteByte(0);

                filePosition = output.Position;

                // Update compressed size
                // HINT: This code allows for the scenario that the table size was calculated to have a const value for FileSize,
                // since it works on previous data. Setting FileSize before calculating the table size however, would require
                // caching all compressed files either in memory or in a temporary file.
                // Since it is very unlikely that every compressed file has either the same size or every file is not compressed,
                // we only update the FileSize here. This gains memory efficiency over a very unlikely case of wrong table size.
                file.Row.Set("FileSize", (int)writtenSize);

                // Rearrange row into correct data table
                if (writtenSize <= 0xFFFF && _dataHTable.Rows.Contains(file.Row))
                {
                    _dataHTable.Rows.Remove(file.Row);
                    _dataLTable.Rows.Add(file.Row);
                }
                else if (writtenSize >= 0x10000 && _dataLTable.Rows.Contains(file.Row))
                {
                    _dataLTable.Rows.Remove(file.Row);
                    _dataHTable.Rows.Add(file.Row);
                }
            }

            // Store written data tables
            var dataLStream = new MemoryStream();
            _dataLTable.Write(dataLStream, 0, false);
            WriteAlignment(dataLStream, 0x10);

            var dataHStream = new MemoryStream();
            _dataHTable.Write(dataHStream, 0, false);
            WriteAlignment(dataHStream, 0x10);

            // Update and write table
            _itocTable.Rows[0].Set("DataL", dataLStream.ToArray());
            _itocTable.Rows[0].Set("DataH", dataHStream.ToArray());
            _itocTable.Rows[0].Set("FilesL", _dataLTable.Rows.Count);
            _itocTable.Rows[0].Set("FilesH", _dataHTable.Rows.Count);

            _itocTable.Write(output, tocOffset);

            // Update header
            _header.Rows[0].Set("ItocOffset", (long)tocOffset);
            _header.Rows[0].Set("ContentOffset", fileOffset);
            _header.Rows[0].Set("ItocSize", (long)tocTableSize);
            _header.Rows[0].Set("ContentSize", filePosition - fileOffset);
        }

        private void WriteEtocTable(Stream output, long tableOffset)
        {
            var tocTableSize = _etocTable.CalculateSize();
            var tocOffset = CpkSupport.Align(tableOffset, _align);

            _etocTable.Write(output, tocOffset);
            WriteAlignment(output, 0x10);

            _header.Rows[0].Set("EtocOffset", tocOffset);
            _header.Rows[0].Set("EtocSize", (long)tocTableSize);
        }

        private void WriteAlignment(Stream input, int alignment)
        {
            using var bw = new BinaryWriterX(input, true);
            input.Position = input.Length;
            bw.WriteAlignment(alignment);
        }

        #endregion
    }
}
