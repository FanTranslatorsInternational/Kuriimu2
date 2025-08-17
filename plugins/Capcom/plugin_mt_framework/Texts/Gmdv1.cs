using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace plugin_mt_framework.Texts
{
    class Gmdv1
    {
        private const int HeaderSize_ = 0x28;

        private GmdHeader _header;
        private string _name;
        private int _labelObscure;
        private int _keyPair;

        public List<TextEntry> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            if (br.PeekString(4) == "\0DMG")
                br.ByteOrder = ByteOrder.BigEndian;

            _header = ReadHeader(br);
            _name = br.ReadString(_header.nameSize);
            input.Position++;

            Gmdv1LabelEntry[] labelEntries = ReadLabelEntries(br, _header.labelCount);
            _labelObscure = labelEntries[0].labelOffset;

            long labelDataOffset = br.BaseStream.Position;
            string[] labels = ReadLabels(br, labelEntries, labelDataOffset);

            br.BaseStream.Position = labelDataOffset + _header.labelSize;
            
            _keyPair = GmdSupport.DetectKeypair(input, br.BaseStream.Position);
            Stream textStream = GmdSupport.GetXorStream(input, br.BaseStream.Position, _keyPair);

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

                Gmdv1LabelEntry? labelEntry = labelEntries.FirstOrDefault(l => l.sectionId == i);
                int labelIndex = Array.IndexOf(labelEntries, labelEntry);

                var textEntry = new TextEntry
                {
                    Name = labelIndex < labels.Length ? labels[labelIndex] : null,
                    TextData = textBr.ReadBytes((int)textSize - 1),
                    Encoding = Encoding.UTF8
                };

                textBr.BaseStream.Position++;

                result.Add(textEntry);
            }

            return result;
        }

        public void Save(IList<TextEntry> entries, Stream output)
        {
            Stream textStream = new MemoryStream();
            Stream cryptStream = GmdSupport.GetXorStream(textStream, _keyPair);

            var labelEntries = new Gmdv1LabelEntry[entries.Count];

            var labelPos = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                cryptStream.Write(entries[i].TextData);
                cryptStream.WriteByte(0);

                labelEntries[i] = new Gmdv1LabelEntry
                {
                    sectionId = i,
                    labelOffset = _labelObscure + labelPos
                };

                labelPos += entries[i].Name is null ? 1 : entries[i].Name!.Length + 1;
            }

            cryptStream.Flush();

            using var bw = new BinaryWriterX(output);

            // Write name
            output.Position = HeaderSize_;
            bw.WriteString(_name);

            // Write label entries
            WriteLabelEntries(labelEntries, bw);

            // Write labels
            long labelPos1 = output.Position;
            WriteLabels(entries, bw);

            long labelSize = output.Length - labelPos1;

            // Write text sections
            textStream.Position = 0;
            textStream.CopyTo(output);

            // Write header
            _header.labelCount = labelEntries.Length;
            _header.sectionCount = entries.Count;
            _header.labelSize = (int)labelSize;
            _header.sectionSize = (int)textStream.Length;
            _header.nameSize = _name.Length;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private GmdHeader ReadHeader(BinaryReaderX reader)
        {
            return new GmdHeader
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

        private void WriteHeader(GmdHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write((int)header.language);
            writer.Write(header.unk1);
            writer.Write(header.labelCount);
            writer.Write(header.sectionCount);
            writer.Write(header.labelSize);
            writer.Write(header.sectionSize);
            writer.Write(header.nameSize);
        }

        private void WriteLabelEntries(IList<Gmdv1LabelEntry> entries, BinaryWriterX writer)
        {
            foreach (Gmdv1LabelEntry entry in entries)
                WriteLabelEntry(entry, writer);
        }

        private void WriteLabelEntry(Gmdv1LabelEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.sectionId);
            writer.Write(entry.labelOffset);
        }

        private void WriteLabels(IList<TextEntry> entries, BinaryWriterX writer)
        {
            foreach (TextEntry entry in entries)
                writer.WriteString(entry.Name ?? string.Empty);
        }
    }
}
