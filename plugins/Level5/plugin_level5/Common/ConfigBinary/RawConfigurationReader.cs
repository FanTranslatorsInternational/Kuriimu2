using System.Text;
using Komponent.IO;
using plugin_level5.Common.ConfigBinary.Models;
using ValueType = plugin_level5.Common.ConfigBinary.Models.ValueType;

namespace plugin_level5.Common.ConfigBinary
{
    internal class RawConfigurationReader : IConfigurationReader<RawConfigurationEntry>
    {
        public Configuration<RawConfigurationEntry> Read(Stream input)
        {
            return Read(input, StringEncoding.Utf8);
        }

        public Configuration<RawConfigurationEntry> Read(Stream input, StringEncoding encoding)
        {
            using var br = new BinaryReaderX(input);

            CfgBinHeader header = ReadHeader(br);
            CfgBinEntry[] entries = ReadEntries(br, header.entryCount);

            return CreateRawConfigurationEntries(br, entries, header.stringDataOffset, encoding);
        }

        private CfgBinHeader ReadHeader(BinaryReaderX br)
        {
            return new CfgBinHeader
            {
                entryCount = br.ReadUInt32(),
                stringDataOffset = br.ReadUInt32(),
                stringDataLength = br.ReadUInt32(),
                stringDataCount = br.ReadUInt32()
            };
        }

        private CfgBinEntry[] ReadEntries(BinaryReaderX br, uint entryCount)
        {
            var result = new CfgBinEntry[entryCount];

            for (var i = 0; i < entryCount; i++)
            {
                result[i] = new CfgBinEntry
                {
                    crc32 = br.ReadUInt32(),
                    entryCount = br.ReadByte()
                };

                var types = new byte[result[i].entryCount];
                for (var j = 0; j < types.Length; j += 4)
                {
                    byte typeChunk = br.ReadByte();
                    for (var h = 0; h < 4; h++)
                    {
                        if (j + h >= types.Length)
                            break;

                        types[j + h] = (byte)(typeChunk >> h * 2 & 0x3);
                    }
                }

                br.SeekAlignment(4);

                var values = new int[result[i].entryCount];
                for (var j = 0; j < types.Length; j++)
                    values[j] = br.ReadInt32();

                result[i].entryTypes = types;
                result[i].entryValues = values;
            }

            return result;
        }

        private Configuration<RawConfigurationEntry> CreateRawConfigurationEntries(BinaryReaderX br,
            CfgBinEntry[] entries, long stringOffset, StringEncoding encoding)
        {
            var configEntries = new RawConfigurationEntry[entries.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                var configEntryValues = new ConfigurationEntryValue[entries[i].entryCount];
                for (var j = 0; j < entries[i].entryCount; j++)
                {
                    var type = (ValueType)entries[i].entryTypes[j];
                    int intValue = entries[i].entryValues[j];

                    object? value;
                    switch (type)
                    {
                        case ValueType.String:
                            if (intValue < 0)
                            {
                                value = null;
                                break;
                            }

                            br.BaseStream.Position = stringOffset + intValue;
                            value = ReadString(br, encoding);
                            break;

                        case ValueType.Int:
                            value = intValue;
                            break;

                        case ValueType.Float:
                            value = BitConverter.Int32BitsToSingle(intValue);
                            break;

                        default:
                            throw new InvalidOperationException($"Unknown value type {type} in config entry.");
                    }

                    configEntryValues[j] = new ConfigurationEntryValue
                    {
                        Type = type,
                        Value = value
                    };
                }

                configEntries[i] = new RawConfigurationEntry
                {
                    Hash = entries[i].crc32,
                    Values = configEntryValues
                };
            }

            return new Configuration<RawConfigurationEntry>
            {
                Entries = configEntries,
                Encoding = encoding
            };
        }

        private string ReadString(BinaryReaderX br, StringEncoding encoding)
        {
            var result = new List<byte>();

            byte byteValue = br.ReadByte();
            while (byteValue != 0)
            {
                result.Add(byteValue);
                byteValue = br.ReadByte();
            }

            switch (encoding)
            {
                case StringEncoding.Sjis:
                    return Encoding.GetEncoding("Shift-JIS").GetString(result.ToArray());

                case StringEncoding.Utf8:
                    return Encoding.UTF8.GetString(result.ToArray());

                default:
                    throw new InvalidOperationException($"Unknown encoding {encoding}.");
            }
        }
    }
}
