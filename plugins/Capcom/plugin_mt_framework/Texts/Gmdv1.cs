using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace plugin_mt_framework.Texts
{
    class Gmdv1
    {
        private Gmdv1Header _header;
        private string _name;
        private Gmdv1LabelEntry[] _labelEntries;
        private string[] _labels;

        public List<TextEntry> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            if (br.PeekString(4) == "\0DMG")
                br.ByteOrder = ByteOrder.BigEndian;

            _header = ReadHeader(br);
            _name = br.ReadString(_header.nameSize);
            input.Position++;

            _labelEntries = ReadLabelEntries(br, _header.labelCount);

            long labelDataOffset = br.BaseStream.Position;
            _labels = ReadLabels(br, _labelEntries, labelDataOffset);

            br.BaseStream.Position = labelDataOffset + _header.labelSize;
            Stream textStream = GmdSupport.GetXorStream(input, br.BaseStream.Position);

            using var textBr = new BinaryReaderX(textStream);
            var result = new List<TextEntry>();

            for (var i = 0; i < _header.sectionCount; i++)
            {
                long textPos = textBr.BaseStream.Position;

                int tmp = textBr.ReadByte();
                while (tmp != 0)
                    tmp = textBr.ReadByte();

                long textSize = textBr.BaseStream.Position - textPos;
                textBr.BaseStream.Position = textPos;

                Gmdv1LabelEntry? labelEntry = _labelEntries.FirstOrDefault(l => l.sectionId == i);
                int labelIndex = Array.IndexOf(_labelEntries, labelEntry);

                var textEntry = new TextEntry
                {
                    Name = labelIndex < _labels.Length ? _labels[labelIndex] : null,
                    TextData = textBr.ReadBytes((int)textSize - 1),
                    Encoding = Encoding.UTF8
                };

                textBr.BaseStream.Position++;

                result.Add(textEntry);
            }

            return result;
        }

        private Gmdv1Header ReadHeader(BinaryReaderX reader)
        {
            return new Gmdv1Header
            {
                magic = reader.ReadString(4),
                version = reader.ReadInt32(),
                language = (GmdLanguage)reader.ReadInt32(),
                unk1 = reader.ReadInt64(),
                labelCount = reader.ReadInt32(),
                sectionCount = reader.ReadInt32(),
                labelSize = reader.ReadInt32(),
                sectionSize = reader.ReadInt32(),
                nameSize = reader.ReadInt32()
            };
        }

        private Gmdv1LabelEntry[] ReadLabelEntries(BinaryReaderX reader, int count)
        {
            var result = new Gmdv1LabelEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadLabelEntry(reader);

            return result;
        }

        private Gmdv1LabelEntry ReadLabelEntry(BinaryReaderX reader)
        {
            return new Gmdv1LabelEntry
            {
                sectionId = reader.ReadInt32(),
                labelOffset = reader.ReadInt32()
            };
        }

        private string[] ReadLabels(BinaryReaderX reader, Gmdv1LabelEntry[] labelEntries, long labelDataOffset)
        {
            var result = new string[labelEntries.Length];

            for (var i = 0; i < labelEntries.Length; i++)
            {
                reader.BaseStream.Position = labelDataOffset + labelEntries[i].labelOffset - labelEntries[0].labelOffset;
                result[i] = reader.ReadNullTerminatedString();
            }

            return result;
        }
    }
}
