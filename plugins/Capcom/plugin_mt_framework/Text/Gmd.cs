using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Models.IO;
using Kontract.Models.Text;

namespace plugin_mt_framework.Text
{
    class Gmd
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(GmdHeader));
        private static readonly int EntrySizeV1 = Tools.MeasureType(typeof(GmdEntryV1));
        private static readonly int EntrySizeV2 = Tools.MeasureType(typeof(GmdEntryV2));
        private static readonly int EntrySizeV2Mobile = Tools.MeasureType(typeof(GmdEntryV2Mobile));

        private ByteOrder _byteOrder;
        private GmdHeader _header;
        private string _name;

        public IList<TextEntry> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Determine byte order
            _byteOrder = br.ByteOrder = br.ReadString(4) == "GMD\0" ? ByteOrder.LittleEndian : ByteOrder.BigEndian;
            br.BaseStream.Position = 0;

            // Read header
            _header = br.ReadType<GmdHeader>();
            _name = br.ReadString(_header.nameSize, Encoding.ASCII);

            br.SeekAlignment(4);

            // Determine reading method
            switch (_header.version)
            {
                // Version 1
                //case 0x00010201:
                //    return ReadVersion1(br);

                // Version 2
                case 0x00010302:
                    return ReadVersion2(br);

                default:
                    throw new InvalidOperationException($"Unsupported version {_header.version:X8}.");
            }
        }

        #region Load

        private IList<TextEntry> ReadVersion1(BinaryReaderX br)
        {
            return Array.Empty<TextEntry>();
        }

        private IList<TextEntry> ReadVersion2(BinaryReaderX br)
        {
            // Determine mobile entries
            var mobileLength = HeaderSize + _header.labelSize + _header.sectionSize +
                               0x400 + ((_header.nameSize + 3) & ~3) + _header.labelCount * EntrySizeV2Mobile;

            // Read entry info
            (int, long)[] entryInfos;
            if (br.BaseStream.Length == mobileLength)
            {
                entryInfos = br.ReadMultiple<GmdEntryV2Mobile>(_header.labelCount)
                    .Select(e => (e.sectionId, e.labelOffset))
                    .ToArray();
            }
            else
            {
                entryInfos = br.ReadMultiple<GmdEntryV2>(_header.labelCount)
                    .Select(e => (e.sectionId, (long)e.labelOffset))
                    .ToArray();
            }

            // Skip bucket list
            br.BaseStream.Position += 0x400;

            // Skip labels
            var labelOffset = br.BaseStream.Position;
            br.BaseStream.Position += _header.labelSize;

            // Read section data
            var result = new List<TextEntry>();
            for (var i = 0; i < _header.labelCount; i++)
            {
                var sectionData = br.ReadBytesUntil(0);
                var label = string.Empty;

                // Get label
                var entryInfo = entryInfos.FirstOrDefault(x => x.Item1 == i);
                if (entryInfo != default)
                {
                    var bkPos = br.BaseStream.Position;
                    br.BaseStream.Position = labelOffset + entryInfo.Item2;
                    label = br.ReadCStringASCII();
                    br.BaseStream.Position = bkPos;
                }

                // Create entry
                var entry = string.IsNullOrEmpty(label) ? new TextEntry() : new TextEntry(label);
                entry.Encoding = Encoding.UTF8;
                entry.TextData = sectionData;
                entry.ControlCodeProcessor = new GmdControlCodeProcessor();
                entry.TextPager = new GmdTextPager();

                result.Add(entry);
            }

            return result;
        }

        #endregion
    }
}
