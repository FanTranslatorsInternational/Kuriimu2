using System.Xml;

namespace Konnect.DataClasses.Management.Text
{
    internal class KupXmlProvider
    {
        public static void Write(SerializedKup kup, XmlWriter writer)
        {
            writer.WriteStartDocument();

            writer.WriteStartElement("kup");

            writer.WriteStartElement("entries");

            foreach (SerializedKupEntry entry in kup.Entries.Entry)
            {
                writer.WriteStartElement("entry");

                writer.WriteAttributeString("name", entry.Name);

                writer.WriteElementString("original", entry.OriginalText);
                writer.WriteElementString("edited", entry.EditedText);

                writer.WriteEndElement(); // entry
            }

            writer.WriteEndElement(); // entries

            writer.WriteEndElement(); // kup

            writer.WriteEndDocument();
        }

        public static SerializedKup? Read(XmlReader reader)
        {
            try
            {
                return ReadKup(reader);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static SerializedKup ReadKup(XmlReader reader)
        {
            List<SerializedKupEntry> entries =[];

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (reader.Name == "entry")
                {
                    entries.Add(ReadEntry(reader));
                }
            }

            return new SerializedKup
            {
                Entries = new SerializedKupEntries
                {
                    Entry = [.. entries]
                }
            };
        }

        private static SerializedKupEntry ReadEntry(XmlReader reader)
        {
            string name = reader.GetAttribute("name") ?? string.Empty;

            string original = string.Empty;
            string edited = string.Empty;

            using XmlReader subtree = reader.ReadSubtree();

            while (subtree.Read())
            {
                if (subtree.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                switch (subtree.Name)
                {
                    case "original":
                        original = subtree.ReadElementContentAsString();
                        break;

                    case "edited":
                        edited = subtree.ReadElementContentAsString();
                        break;
                }
            }

            return new SerializedKupEntry
            {
                Name = name,
                OriginalText = original,
                EditedText = edited
            };
        }
    }
}
