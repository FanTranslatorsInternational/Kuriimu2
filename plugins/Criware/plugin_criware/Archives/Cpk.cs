using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
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

        private CpkTable _gtocTable;

        private int _align;

        public List<IArchiveFile> Load(Stream input)
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
            var gtocOffset = headerRow.Get<long>("GtocOffset");

            var result = new List<IArchiveFile>();

            // Read tables
            if (etocOffset > 0)
                ReadEtocTable(br, etocOffset);

            if (gtocOffset > 0)
                ReadGtocTable(br, gtocOffset);

            if (tocOffset > 0)
                result.AddRange(ReadTocTable(br, tocOffset, contentOffset));

            if (itocOffset > 0)
                result.AddRange(ReadItocTable(br, itocOffset));

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            var headerTableSize = _header.CalculateSize();
            long tableOffset = headerTableSize;

            var fileOffset = tableOffset +
                CpkSupport.Align(_tocTable?.CalculateSize() ?? 0, _align) +
                CpkSupport.Align(_etocTable?.CalculateSize() ?? 0, _align) +
                CpkSupport.Align(_itocTable?.CalculateSize() ?? 0, _align) +
                CpkSupport.Align(_gtocTable?.CalculateSize() ?? 0, _align);

            // Write files and toc table
            if (_tocTable != null)
            {
                WriteTocTable(output, files, tableOffset, fileOffset);
                tableOffset = CpkSupport.Align(tableOffset, _align) + _tocTable.CalculateSize();
            }

            if (_itocTable != null)
            {
                WriteItocTable(output, files, tableOffset, fileOffset);
                tableOffset = CpkSupport.Align(tableOffset, _align) + _itocTable.CalculateSize();
            }

            if (_etocTable != null)
            {
                WriteEtocTable(output, tableOffset);
                tableOffset = CpkSupport.Align(tableOffset, _align) + _etocTable.CalculateSize();
            }

            if (_gtocTable != null)
            {
                WriteGtocTable(output, tableOffset);
                tableOffset = CpkSupport.Align(tableOffset, _align) + _gtocTable.CalculateSize();
            }

            // Write header
            _header.Write(output, 0, _align);
        }

        public void DeleteFile(IArchiveFile afi)
        {
            var fileInfo = afi as CpkArchiveFile;
            if (fileInfo == null)
                return;

            // TocTable
            _tocTable?.Rows.Remove(fileInfo.Row);

            // ItocTable
            switch (_itocTable?.Name)
            {
                case "CpkItocInfo":
                    _dataLTable?.Rows.Remove(fileInfo.Row);
                    _dataHTable?.Rows.Remove(fileInfo.Row);
                    break;

                case "CpkExtendId":
                    _itocTable?.Rows.Remove(fileInfo.Row);
                    _tocTable?.Rows.Remove(_tocTable.Rows.FirstOrDefault(x => x.Get<int>("ID") == fileInfo.Row.Get<int>("ID")));
                    break;
            }

            // EtocTable
            _etocTable?.Rows.Remove(fileInfo.Row);

            // GtocTable
            _gtocTable?.Rows.Remove(fileInfo.Row);
        }

        public void DeleteAll()
        {
            // TocTable
            _tocTable?.Rows.Clear();

            // ItocTable
            _dataLTable?.Rows.Clear();
            _dataHTable?.Rows.Clear();
            _itocTable?.Rows.Clear();

            // EtocTable
            _etocTable?.Rows.Clear();

            // GtocTable
            _gtocTable?.Rows.Clear();
        }

        #region Read tables

        private IEnumerable<IArchiveFile> ReadTocTable(BinaryReaderX br, long tocOffset, long contentOffset)
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
                    yield return new CpkArchiveFile(new ArchiveFileInfo
                    {
                        FilePath = UPath.Combine(dir, name).FullName,
                        FileData = subStream
                    }, row);
                else
                    yield return new CpkArchiveFile(new CompressedArchiveFileInfo
                    {
                        FilePath = UPath.Combine(dir, name).FullName,
                        FileData = subStream,
                        Compression = Compressions.Crilayla.Build(),
                        DecompressedSize = decompSize
                    }, row);
            }
        }

        private IEnumerable<IArchiveFile> ReadItocTable(BinaryReaderX br, long itocOffset)
        {
            // Read toc table
            _itocTable = CpkTable.Create(br.BaseStream, itocOffset);

            if (_itocTable.Name != "CpkItocInfo")
                yield break;

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
                    yield return new CpkArchiveFile(new ArchiveFileInfo
                    {
                        FilePath = $"{id:00000}.bin",
                        FileData = subStream
                    }, row);
                else
                    yield return new CpkArchiveFile(new CompressedArchiveFileInfo
                    {
                        FilePath = $"{id:00000}.bin",
                        FileData = subStream,
                        Compression = Compressions.Crilayla.Build(),
                        DecompressedSize = decompSize
                    }, row);

                fileOffset = (fileOffset + compSize + (_align - 1)) & ~(_align - 1);
            }
        }

        private void ReadEtocTable(BinaryReaderX br, long etocOffset)
        {
            _etocTable = CpkTable.Create(br.BaseStream, etocOffset);
        }

        private void ReadGtocTable(BinaryReaderX br, long gtocOffset)
        {
            _gtocTable = CpkTable.Create(br.BaseStream, gtocOffset);
        }

        #endregion

        #region Write tables

        private void WriteTocTable(Stream output, IList<IArchiveFile> files, long tableOffset, long fileOffset)
        {
            // Update file information
            foreach (var file in files.Cast<CpkArchiveFile>())
            {
                file.Row.Set("DirName", file.FilePath.ToRelative().GetDirectory().FullName);
                file.Row.Set("FileName", file.FilePath.GetName());
                file.Row.Set("ExtractSize", (int)file.FileSize);
            }

            var tocTableSize = _tocTable.CalculateSize();
            var tocOffset = CpkSupport.Align(tableOffset, _align);
            fileOffset = CpkSupport.Align(fileOffset, _align);
            var filePosition = fileOffset;

            // Write files and update remaining file information
            foreach (var file in files.Cast<CpkArchiveFile>().OrderBy(x => x.Row.Get<int>("ID")))
            {
                // Update offset
                file.Row.Set("FileOffset", filePosition - tocOffset);

                // Write file
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output, true);

                while (output.Position % _align > 0)
                    output.WriteByte(0);

                filePosition = output.Position;

                // Update compressed size
                // HINT: This code allows for the scenario that the table size was calculated to have a const value for FileSize, instead of a row value,
                // since it works on previous data. Setting FileSize before calculating the table size however, would require
                // caching all compressed files either in memory or in a temporary file.
                // Since it is very unlikely that every compressed file has either the same size or every file is not compressed,
                // we only update the FileSize here. This gains memory efficiency over a very unlikely case of wrong table size.
                file.Row.Set("FileSize", (int)writtenSize);
            }

            // Write table
            _tocTable.Write(output, tocOffset, _align);

            // Update header
            _header.Rows[0].Set("TocOffset", (long)tocOffset);
            _header.Rows[0].Set("ContentOffset", fileOffset);
            _header.Rows[0].Set("TocSize", (long)tocTableSize);
            _header.Rows[0].Set("ContentSize", filePosition - fileOffset);
        }

        private void WriteItocTable(Stream output, IList<IArchiveFile> files, long tableOffset, long fileOffset)
        {
            switch (_itocTable.Name)
            {
                case "CpkItocInfo":
                    WriteItocFileTable(output, files, tableOffset, fileOffset);
                    break;

                case "CpkExtendId":
                    WriteItocExtendedIdTable(output, tableOffset);
                    break;
            }
        }

        private void WriteItocFileTable(Stream output, IList<IArchiveFile> files, long tableOffset, long fileOffset)
        {
            // Update file information
            foreach (var file in files.Cast<CpkArchiveFile>())
                file.Row.Set("ExtractSize", (int)file.FileSize);

            var tocTableSize = _itocTable.CalculateSize();
            var tocOffset = CpkSupport.Align(tableOffset, _align);
            var filePosition = fileOffset;

            // Write files and update remaining file information
            foreach (var file in files.Cast<CpkArchiveFile>().OrderBy(x => int.Parse(x.FilePath.GetNameWithoutExtension())))
            {
                // Write file
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output, true);

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
            _dataLTable.Write(dataLStream, 0, _align, false);
            WriteAlignment(dataLStream, 0x10);

            var dataHStream = new MemoryStream();
            _dataHTable.Write(dataHStream, 0, _align, false);
            WriteAlignment(dataHStream, 0x10);

            // Update and write table
            _itocTable.Rows[0].Set("DataL", dataLStream.ToArray());
            _itocTable.Rows[0].Set("DataH", dataHStream.ToArray());
            _itocTable.Rows[0].Set("FilesL", _dataLTable.Rows.Count);
            _itocTable.Rows[0].Set("FilesH", _dataHTable.Rows.Count);

            _itocTable.Write(output, tocOffset, _align);

            // Update header
            _header.Rows[0].Set("ItocOffset", (long)tocOffset);
            _header.Rows[0].Set("ContentOffset", fileOffset);
            _header.Rows[0].Set("ItocSize", (long)tocTableSize);
            _header.Rows[0].Set("ContentSize", filePosition - fileOffset);
        }

        private void WriteItocExtendedIdTable(Stream output, long tableOffset)
        {
            // Update extended ids
            for (var i = 0; i < _itocTable.Rows.Count; i++)
            {
                var row = _itocTable.Rows[i];

                row.Set("ID", i);
                row.Set("TocIndex", _tocTable.Rows.IndexOf(_tocTable.Rows.FirstOrDefault(x => x.Get<int>("ID") == i)));
            }

            WriteTable(output, _itocTable, tableOffset, "ItocOffset", "ItocSize");
        }

        private void WriteEtocTable(Stream output, long tableOffset)
        {
            WriteTable(output, _etocTable, tableOffset, "EtocOffset", "EtocSize");
        }

        private void WriteGtocTable(Stream output, long tableOffset)
        {
            WriteTable(output, _gtocTable, tableOffset, "GtocOffset", "GtocSize");
        }

        private void WriteTable(Stream output, CpkTable table, long tableOffset, string offsetName, string sizeName)
        {
            var tocTableSize = table.CalculateSize();
            var tocOffset = CpkSupport.Align(tableOffset, _align);

            table.Write(output, tocOffset, _align);
            WriteAlignment(output, 0x10);

            _header.Rows[0].Set(offsetName, tocOffset);
            _header.Rows[0].Set(sizeName, (long)tocTableSize);
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
