using Kryptography.Checksum;
using Kryptography.Checksum.Crc;
using plugin_level5.Common.ConfigBinary.Models;

namespace plugin_level5.Common.ConfigBinary
{
    internal class EventTextParser
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

        public EventTextParser()
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

        public EventTextConfiguration Parse(Configuration<RawConfigurationEntry> config)
        {
            var result = new EventTextConfiguration();

            for (var i = 0; i < config.Entries.Length; i++)
            {
                RawConfigurationEntry entry = config.Entries[i];

                if (entry.Hash == _lastUpdateDatetimeChecksum)
                {
                    if (entry.Values[0].Value == null)
                        continue;

                    result.LastUpdateDateTime = DateTime.Parse((string)entry.Values[0].Value!);
                    continue;
                }

                if (entry.Hash == _lastUpdateUserChecksum)
                {
                    result.LastUpdateUser = (string?)entry.Values[0].Value;
                    continue;
                }

                if (entry.Hash == _lastUpdateMachineChecksum)
                {
                    result.LastUpdateMachine = (string?)entry.Values[0].Value;
                    continue;
                }

                if (entry.Hash == _textInfoBeginChecksum)
                {
                    result.Texts = ReadEventTextInfos(config.Entries, ref i);
                    continue;
                }

                if (entry.Hash == _textIndexBeginChecksum)
                {
                    if (result.Texts == null)
                        throw new InvalidOperationException("Texts were not read yet.");

                    result.Texts = SortEventTexts(result.Texts, config.Entries, ref i);
                }
            }

            return result;
        }

        private EventText[] ReadEventTextInfos(RawConfigurationEntry[] entries, ref int index)
        {
            if (entries[index].Hash != _textInfoBeginChecksum)
                throw new InvalidOperationException("TextInfos are not properly opened.");

            var count = (int)entries[index++].Values[0].Value!;
            int endIndex = index + count;

            var result = new EventText[count];

            int startIndex = index;
            for (; index < Math.Min(entries.Length, endIndex); index++)
            {
                RawConfigurationEntry entry = entries[index];

                if (entry.Hash == _textInfoChecksum)
                {
                    result[index - startIndex] = new EventText
                    {
                        Hash = (uint)(int)entry.Values[0].Value!,
                        SubId = (int)entry.Values[1].Value!,
                        Text = (string?)entry.Values[2].Value
                    };
                    continue;
                }

                if (entry.Hash == _textInfoEndChecksum)
                    break;
            }

            if (entries[index].Hash != _textInfoEndChecksum)
                throw new InvalidOperationException("TextInfos are not properly closed.");

            return result;
        }

        private EventText[] SortEventTexts(EventText[] texts, RawConfigurationEntry[] entries, ref int index)
        {
            if (entries[index].Hash != _textIndexBeginChecksum)
                throw new InvalidOperationException("TextInfos are not properly opened.");

            var result = new EventText[texts.Length];

            index++;
            for (; index < entries.Length; index++)
            {
                RawConfigurationEntry entry = entries[index];

                if (entry.Hash == _textIndexChecksum)
                {
                    var hash = (uint)(int)entry.Values[0].Value!;
                    var textSubId = (int)entry.Values[1].Value!;

                    EventText? foundText = texts.FirstOrDefault(x => x.Hash == hash && x.SubId == textSubId);
                    if (foundText == null)
                        continue;

                    var newIndex = (int)entry.Values[2].Value!;
                    result[newIndex] = foundText;

                    continue;
                }

                if (entry.Hash == _textIndexEndChecksum)
                    break;
            }

            if (entries[index].Hash != _textIndexEndChecksum)
                throw new InvalidOperationException("TextInfos are not properly closed.");

            return result;
        }
    }
}
