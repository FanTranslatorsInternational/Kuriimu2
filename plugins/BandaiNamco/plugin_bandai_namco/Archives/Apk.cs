using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_bandai_namco.Archives
{
    class Apk
    {
        private byte[] _headerIdent;
        private string _name;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read sections
            var sections = ApkSection.ReadAll(input);

            // Read pack header
            var packHeader = sections.FirstOrDefault(x => x.Type == ApkSection.PackHeader).ReadPackHeader();

            _headerIdent = packHeader.headerIdent;

            // Read entries
            var entries = sections.FirstOrDefault(x => x.Type == ApkSection.PackToc).ReadToc().entries;

            // Read strings
            using var stringBr = new BinaryReaderX(sections.FirstOrDefault(x => x.Type == ApkSection.StringTable).Data, true);

            var stringHeader = ApkSupport.ReadStringHeader(stringBr);

            stringBr.BaseStream.Position = stringHeader.tableOffset;
            var stringOffsets = ReadIntegers(stringBr, stringHeader.stringCount);

            var strings = new List<string>();
            foreach (var stringOffset in stringOffsets)
            {
                stringBr.BaseStream.Position = stringHeader.dataOffset + stringOffset;
                strings.Add(stringBr.ReadNullTerminatedString());
            }

            _name = strings[packHeader.stringIndex];

            return ApkSupport.EnumerateFiles(new List<Stream> { input }, entries[0], UPath.Root, new List<ApkPackHeader> { packHeader }, strings, entries).ToList();
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            ApkSupport.Save(output, files, _name, _headerIdent);
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }
    }
}
