using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_bandai_namco.Archives
{
    class Apk
    {
        private byte[] _headerIdent;
        private string _name;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read sections
            var sections = ApkSection.ReadAll(input);

            // Read pack header
            var packHeader = sections.FirstOrDefault(x => x.Type == ApkSection.PackHeader).As<ApkPackHeader>();

            _headerIdent = packHeader.headerIdent;

            // Read entries
            var entries = sections.FirstOrDefault(x => x.Type == ApkSection.PackToc).As<ApkToc>().entries;

            // Read strings
            using var stringBr = new BinaryReaderX(sections.FirstOrDefault(x => x.Type == ApkSection.StringTable).Data, true);

            var stringHeader = stringBr.ReadType<ApkStringHeader>();

            stringBr.BaseStream.Position = stringHeader.tableOffset;
            var stringOffsets = stringBr.ReadMultiple<int>(stringHeader.stringCount);

            var strings = new List<string>();
            foreach (var stringOffset in stringOffsets)
            {
                stringBr.BaseStream.Position = stringHeader.dataOffset + stringOffset;
                strings.Add(stringBr.ReadCStringASCII());
            }

            _name = strings[packHeader.stringIndex];

            return ApkSupport.EnumerateFiles(new List<Stream> { input }, entries[0], UPath.Root, new List<ApkPackHeader> { packHeader }, strings, entries).ToArray();
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            ApkSupport.Save(output, files, _name, _headerIdent);
        }
    }
}
