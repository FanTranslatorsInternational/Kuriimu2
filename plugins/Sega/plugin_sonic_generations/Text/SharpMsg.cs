using Komponent.IO;
using Kontract.Models.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace plugin_sonic_generations.Text
{
    public class SharpMsg
    {
        private SharpMsgHeader _header;
        private List<SharpMsgEntry> _entries;

        public IList<TextEntry> Load(Stream stream)
        {
            var reader = new BinaryReaderX(stream);

            _entries = new List<SharpMsgEntry>();

            _header = reader.ReadType<SharpMsgHeader>();
            var entryInfos = reader.ReadMultiple<SharpMsgEntryInfo>(_header.entryCount);

            foreach (var info in entryInfos)
            {
                var entry = new SharpMsgEntry();
                entry.entryInfo = info;

                reader.BaseStream.Position = info.dataOffset;
                entry.entryDataInfo = reader.ReadType<SharpMsgEntryDataInfo>();

                reader.BaseStream.Position = entry.entryDataInfo.pString;
                entry.message = reader.ReadCStringUTF16();

                reader.BaseStream.Position = info.labelOffset;
                entry.label = reader.ReadASCIIStringUntil(0);

                _entries.Add(entry);
            }

            return _entries.Select(e => new TextEntry(e.message) { Name = e.label }).ToList();
        }

        public void Save(IList<TextEntry> texts, Stream stream)
        {
            // Update all messages
            foreach (var text in texts)
            {
                foreach (var entry in _entries)
                {
                    if (text.Name == entry.label)
                    {
                        entry.message = text.GetText();
                        break;
                    }
                }
            }

            _header.entryCount = _entries.Count;

            var writer = new BinaryWriterX(stream);
            var startPosition = writer.BaseStream.Position;

            // Write header
            writer.WriteType(_header);

            // Write entry infos for holding places
            writer.WriteMultiple(_entries.Select(e => e.entryInfo));

            // Write all _entries
            foreach (var e in _entries)
            {
                e.entryInfo.dataOffset = (int)writer.BaseStream.Position;
                e.entryDataInfo.ppString = e.entryInfo.dataOffset + 0x10;
                e.entryDataInfo.pString = e.entryDataInfo.ppString + 0x10;

                writer.WriteType(e.entryDataInfo);
                writer.WriteString(e.message, Encoding.Unicode, false);
                writer.WriteAlignment();

                e.entryInfo.labelOffset = (int)writer.BaseStream.Position;
                writer.WriteString(e.label, Encoding.ASCII, false); // ASCII or UTF8?
            }

            // Write real entry infos
            writer.BaseStream.Position = startPosition + 0x10;
            writer.WriteMultiple(_entries.Select(e => e.entryInfo));
        }
    }
}
