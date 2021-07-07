using Komponent.IO;
using Kontract.Models.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace plugin_sega.Text
{
    public class SharpMsg
    {
        private SharpMsgHeader _header;
        private List<SharpMsgEntry> entries;

        public IList<TextEntry> Load(Stream stream)
        {
            entries = new List<SharpMsgEntry>();

            BinaryReaderX reader = new BinaryReaderX(stream, Kontract.Models.IO.ByteOrder.LittleEndian);
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

                entries.Add(entry);
            }

            return entries.Select(e => new TextEntry(e.label) { TextData = Encoding.Unicode.GetBytes(e.message), Encoding = Encoding.Unicode }).ToList();
        }

        public void Save(IList<TextEntry> texts, Stream stream)
        {
            // Update all messages
            foreach (var text in texts)
            {
                foreach (var entry in entries)
                {
                    if (text.Name == entry.label)
                    {
                        entry.message = text.GetText().Serialize();
                        break;
                    }
                }
            }

            _header.entryCount = entries.Count;

            BinaryWriterX writer = new BinaryWriterX(stream, Kontract.Models.IO.ByteOrder.LittleEndian);
            var startPosition = writer.BaseStream.Position;

            // Wirite header
            writer.WriteType(_header);

            // Write entry infos for holding places
            writer.WriteMultiple(entries.Select(e => e.entryInfo));

            // Write all entries
            foreach (var e in entries)
            {
                e.entryInfo.dataOffset = (int)writer.BaseStream.Position;
                e.entryDataInfo.ppString = e.entryInfo.dataOffset + 0x10;
                e.entryDataInfo.pString = e.entryDataInfo.ppString + 0x10;

                writer.WriteType(e.entryDataInfo);
                writer.WriteString(e.message, Encoding.Unicode, leadingCount: false);
                writer.WriteAlignment(16, 0);

                e.entryInfo.labelOffset = (int)writer.BaseStream.Position;
                writer.WriteString(e.label, Encoding.ASCII, leadingCount: false); // ASCII or UTF8?
            }

            // Write real entry infos
            writer.BaseStream.Position = startPosition + 0x10;
            writer.WriteMultiple(entries.Select(e => e.entryInfo));
        }
    }
}
