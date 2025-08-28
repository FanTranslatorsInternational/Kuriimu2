using System.Xml.Serialization;
using Konnect.DataClasses.Management.Text;

namespace Konnect.Management.Text
{
    public static class KupManager
    {
        public static TranslationFileEntry[] Load(string filePath)
        {
            using Stream inputStream = File.OpenRead(filePath);

            var serializer = new XmlSerializer(typeof(KupXmlRoot));
            var root = (KupXmlRoot?)serializer.Deserialize(inputStream);

            if (root is null)
                return [];

            var result = new List<TranslationFileEntry>();

            foreach (KupXmlEntry entry in root.Entries.Entry)
            {
                int pageIndex = entry.Name.IndexOf(';');

                result.Add(new TranslationFileEntry
                {
                    Name = pageIndex >= 0 ? entry.Name[..pageIndex] : entry.Name,
                    PageName = pageIndex >= 0 ? entry.Name[(pageIndex + 1)..] : null,
                    OriginalText = entry.OriginalText,
                    TranslatedText = entry.EditedText
                });
            }

            return [.. result];
        }

        public static void Save(string filePath, TranslationFileEntry[] entries)
        {
            var xmlEntries = new List<KupXmlEntry>();

            foreach (TranslationFileEntry entry in entries)
            {
                string name = entry.Name;
                if (entry.PageName is not null)
                    name += $";{entry.PageName}";

                xmlEntries.Add(new KupXmlEntry
                {
                    Name = name,
                    OriginalText = entry.OriginalText,
                    EditedText = entry.TranslatedText
                });
            }

            var root = new KupXmlRoot
            {
                Entries = new KupXmlEntries
                {
                    Entry = [.. xmlEntries]
                }
            };

            using Stream outputStream = File.Create(filePath);

            var serializer = new XmlSerializer(typeof(KupXmlRoot));
            serializer.Serialize(outputStream, root);
        }
    }
}
