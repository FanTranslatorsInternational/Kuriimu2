using System.Text;
using Komponent.IO;
using plugin_level5.Common.ConfigBinary.Models;
using ValueType = plugin_level5.Common.ConfigBinary.Models.ValueType;

namespace plugin_level5.Common.ConfigBinary
{
    internal class RawConfigurationWriter : IConfigurationWriter<RawConfigurationEntry>
    {
        public Stream Write(Configuration<RawConfigurationEntry> config, Stream output)
        {
            using var bw = new BinaryWriterX(output);

            Encoding enc = GetEncoding(config.Encoding);

            bw.BaseStream.Position = 0x10;
            CfgBinHeader header = WriteEntries(bw, config.Entries, enc);

            bw.BaseStream.Position = 0;
            WriteHeader(bw, header);

            output.Position = 0;
            return output;
        }

        private CfgBinHeader WriteEntries(BinaryWriterX bw, RawConfigurationEntry[] configEntries, Encoding encoding)
        {
            var header = new CfgBinHeader
            {
                entryCount = (uint)configEntries.Length
            };

            var entryLength = 0;
            foreach (RawConfigurationEntry configEntry in configEntries)
                entryLength += 4 + ((int)Math.Ceiling(configEntry.Values.Length / 4f) + 4 & ~3) + configEntry.Values.Length * 4;

            header.stringDataOffset = (uint)(0x10 + entryLength + 0xF & ~0xF);

            uint stringOffset = (uint)bw.BaseStream.Position + header.stringDataOffset - 0x10;
            uint stringOffsetBase = stringOffset;
            var writtenStrings = new Dictionary<string, uint>();
            var stringCount = 0u;
            foreach (RawConfigurationEntry configEntry in configEntries)
                WriteEntry(bw, configEntry, encoding, stringOffsetBase, writtenStrings, ref stringOffset, ref stringCount);

            bw.WriteAlignment(0x10, 0xFF);

            header.stringDataLength = stringOffset - header.stringDataOffset;
            header.stringDataCount = stringCount;

            bw.BaseStream.Position = stringOffset;
            bw.WriteAlignment(0x10, 0xFF);

            return header;
        }

        private void WriteEntry(BinaryWriterX bw, RawConfigurationEntry configEntry, Encoding encoding, uint stringOffsetBase, IDictionary<string, uint> writtenStrings,
            ref uint stringOffset, ref uint stringCount)
        {
            bw.Write(configEntry.Hash);
            bw.Write((byte)configEntry.Values.Length);

            var typesWritten = 0;
            byte typeBuffer = 0;
            for (var i = 0; i < configEntry.Values.Length; i++)
            {
                if (typesWritten >= 4)
                {
                    bw.Write(typeBuffer);

                    typeBuffer = 0;
                    typesWritten = 0;
                }

                typeBuffer |= (byte)((int)configEntry.Values[i].Type << i % 4 * 2);
                typesWritten++;
            }

            if (typesWritten > 0)
                bw.Write(typeBuffer);

            bw.WriteAlignment(4, 0xFF);

            foreach (ConfigurationEntryValue value in configEntry.Values)
            {
                switch (value.Type)
                {
                    case ValueType.String:
                        if (value.Value == null)
                        {
                            bw.Write(-1);
                            break;
                        }

                        WriteString(bw, (string)value.Value, encoding, stringOffsetBase, writtenStrings, ref stringOffset, ref stringCount);
                        break;

                    case ValueType.Int:
                        bw.Write((int)value.Value!);
                        break;

                    case ValueType.Float:
                        bw.Write((float)value.Value!);
                        break;
                }
            }
        }

        private void WriteHeader(BinaryWriterX bw, CfgBinHeader header)
        {
            bw.Write(header.entryCount);
            bw.Write(header.stringDataOffset);
            bw.Write(header.stringDataLength);
            bw.Write(header.stringDataCount);
        }

        private Encoding GetEncoding(StringEncoding encoding)
        {
            switch (encoding)
            {
                case StringEncoding.Sjis:
                    return Encoding.GetEncoding("Shift-JIS");

                case StringEncoding.Utf8:
                    return Encoding.UTF8;

                default:
                    throw new InvalidOperationException($"Unknown encoding {encoding}.");
            }
        }

        private void WriteString(BinaryWriterX bw, string value, Encoding encoding, uint stringOffsetBase, IDictionary<string, uint> writtenNames,
            ref uint stringOffset, ref uint stringCount)
        {
            if (writtenNames.TryGetValue(value, out uint nameOffset))
            {
                bw.Write(nameOffset - stringOffsetBase);
                return;
            }

            stringCount++;

            bw.Write(stringOffset - stringOffsetBase);
            long entryOffset = bw.BaseStream.Position;

            bw.BaseStream.Position = stringOffset;
            CacheStrings(bw, value, encoding, writtenNames);
            bw.WriteString(value, encoding, false);
            stringOffset = (uint)bw.BaseStream.Position;

            bw.BaseStream.Position = entryOffset;
        }

        private void CacheStrings(BinaryWriterX stringWriter, string value, Encoding encoding, IDictionary<string, uint> writtenNames)
        {
            long nameOffset = stringWriter.BaseStream.Position;

            do
            {
                if (!writtenNames.ContainsKey(value))
                    writtenNames[value] = (uint)nameOffset;

                nameOffset += encoding.GetByteCount(value[..1]);
                value = value.Length > 1 ? value[1..] : string.Empty;
            } while (value.Length > 0);

            if (!writtenNames.ContainsKey(value))
                writtenNames[value] = (uint)nameOffset;
        }
    }
}
