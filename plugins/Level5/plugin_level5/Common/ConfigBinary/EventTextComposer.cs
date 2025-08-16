using Kryptography.Checksum;
using Kryptography.Checksum.Crc;
using plugin_level5.Common.ConfigBinary.Models;
using ValueType = plugin_level5.Common.ConfigBinary.Models.ValueType;

namespace plugin_level5.Common.ConfigBinary
{
    internal class EventTextComposer
    {
        private readonly uint _lastUpdateDatetimeChecksum;
        private readonly uint _lastUpdateUserChecksum;
        private readonly uint _lastUpdateMachineChecksum;
        private readonly uint _textInfoBeginChecksum;
        private readonly uint _textInfoChecksum;
        private readonly uint _textInfoEndChecksum;
        private readonly uint _textIndexBeginChecksum;
        private readonly uint _textIndexChecksum;
        private readonly uint _textIndexEndChecksum;

        public EventTextComposer()
        {
            Checksum<uint> jamCrc = Crc32.JamCrc;

            _lastUpdateDatetimeChecksum = jamCrc.ComputeValue("LAST_UPDATE_DATE_TIME");
            _lastUpdateUserChecksum = jamCrc.ComputeValue("LAST_UPDATE_USER");
            _lastUpdateMachineChecksum = jamCrc.ComputeValue("LAST_UPDATE_MACHINE");
            _textInfoBeginChecksum = jamCrc.ComputeValue("EVENT_TEXT_INFO_BEGIN");
            _textInfoChecksum = jamCrc.ComputeValue("EVENT_TEXT_INFO");
            _textInfoEndChecksum = jamCrc.ComputeValue("EVENT_TEXT_INFO_END");
            _textIndexBeginChecksum = jamCrc.ComputeValue("EVENT_TEXT_INDEX_BEGIN");
            _textIndexChecksum = jamCrc.ComputeValue("EVENT_TEXT_INDEX");
            _textIndexEndChecksum = jamCrc.ComputeValue("EVENT_TEXT_INDEX_END");
        }

        public Configuration<RawConfigurationEntry> Compose(EventTextConfiguration config)
        {
            var result = new Configuration<RawConfigurationEntry>
            {
                Entries = CreateEntries(config),
                Encoding = config.StringEncoding
            };

            return result;
        }

        private RawConfigurationEntry[] CreateEntries(EventTextConfiguration config)
        {
            int entryCount = 7 + config.Texts.Length * 2;
            var result = new RawConfigurationEntry[entryCount];

            // Set user information
            result[0] = CreateLastUpdateDatetimeEntry(config.LastUpdateDateTime);
            result[1] = CreateLastUpdateUserEntry(config.LastUpdateUser);
            result[2] = CreateLastUpdateMachineEntry(config.LastUpdateMachine);

            // Set text information
            result[3] = CreateTextInfoBeginEntry(config.Texts.Length);
            SetTextInfoEntries(result, 4, config.Texts);
            result[4 + config.Texts.Length] = CreateTextInfoEndEntry();

            // Set text index information
            result[5 + config.Texts.Length] = CreateTextIndexBeginEntry(config.Texts.Length);
            SetTextIndexEntries(result, 6 + config.Texts.Length, config.Texts);
            result[6 + config.Texts.Length * 2] = CreateTextIndexEndEntry();

            return result;
        }

        private RawConfigurationEntry CreateLastUpdateDatetimeEntry(DateTime? updateTime)
        {
            return new RawConfigurationEntry
            {
                Hash = _lastUpdateDatetimeChecksum,
                Values =
                [
                    new ConfigurationEntryValue
                    {
                        Type = ValueType.String,
                        Value = updateTime?.ToString("yyyy/MM/dd HH:mm:ss")
                    }
                ]
            };
        }

        private RawConfigurationEntry CreateLastUpdateUserEntry(string? user)
        {
            return new RawConfigurationEntry
            {
                Hash = _lastUpdateUserChecksum,
                Values =
                [
                    new ConfigurationEntryValue
                    {
                        Type = ValueType.String,
                        Value = user
                    }
                ]
            };
        }

        private RawConfigurationEntry CreateLastUpdateMachineEntry(string? machine)
        {
            return new RawConfigurationEntry
            {
                Hash = _lastUpdateMachineChecksum,
                Values =
                [
                    new ConfigurationEntryValue
                    {
                        Type = ValueType.String,
                        Value = machine
                    }
                ]
            };
        }

        private RawConfigurationEntry CreateTextInfoBeginEntry(int count)
        {
            return new RawConfigurationEntry
            {
                Hash = _textInfoBeginChecksum,
                Values =
                [
                    new ConfigurationEntryValue
                    {
                        Type = ValueType.Int,
                        Value = count
                    }
                ]
            };
        }

        private void SetTextInfoEntries(RawConfigurationEntry[] entries, int index, EventText[] texts)
        {
            for (var i = 0; i < texts.Length; i++)
            {
                entries[index + i] = new RawConfigurationEntry
                {
                    Hash = _textInfoChecksum,
                    Values =
                    [
                        new ConfigurationEntryValue
                        {
                            Type = ValueType.Int,
                            Value = unchecked((int)texts[i].Hash)
                        },
                        new ConfigurationEntryValue
                        {
                            Type = ValueType.Int,
                            Value = texts[i].SubId
                        },
                        new ConfigurationEntryValue
                        {
                            Type = ValueType.String,
                            Value = texts[i].Text
                        }
                    ]
                };
            }
        }

        private RawConfigurationEntry CreateTextInfoEndEntry()
        {
            return new RawConfigurationEntry
            {
                Hash = _textInfoEndChecksum,
                Values = []
            };
        }

        private RawConfigurationEntry CreateTextIndexBeginEntry(int count)
        {
            return new RawConfigurationEntry
            {
                Hash = _textIndexBeginChecksum,
                Values =
                [
                    new ConfigurationEntryValue
                    {
                        Type = ValueType.Int,
                        Value = count
                    }
                ]
            };
        }

        private void SetTextIndexEntries(RawConfigurationEntry[] entries, int index, EventText[] texts)
        {
            for (var i = 0; i < texts.Length; i++)
            {
                entries[index + i] = new RawConfigurationEntry
                {
                    Hash = _textIndexChecksum,
                    Values =
                    [
                        new ConfigurationEntryValue
                        {
                            Type = ValueType.Int,
                            Value = unchecked((int)texts[i].Hash)
                        },
                        new ConfigurationEntryValue
                        {
                            Type = ValueType.Int,
                            Value = texts[i].SubId
                        },
                        new ConfigurationEntryValue
                        {
                            Type = ValueType.Int,
                            Value = i
                        }
                    ]
                };
            }
        }

        private RawConfigurationEntry CreateTextIndexEndEntry()
        {
            return new RawConfigurationEntry
            {
                Hash = _textIndexEndChecksum,
                Values = []
            };
        }
    }
}
