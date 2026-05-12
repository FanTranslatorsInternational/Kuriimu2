using System.Xml;
using Konnect.DataClasses.Management.Text;

namespace Konnect.Management.Text;

public static class KupManager
{
    public static TranslationFileEntry[] Load(Stream input)
    {
        using var reader = XmlReader.Create(input);
        SerializedKup? root = KupXmlProvider.Read(reader);

        if (root is null)
            return [];

        var result = new List<TranslationFileEntry>();

        foreach (SerializedKupEntry entry in root.Entries.Entry)
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

    public static void Save(Stream output, TranslationFileEntry[] entries)
    {
        var xmlEntries = new List<SerializedKupEntry>();

        foreach (TranslationFileEntry entry in entries)
        {
            string name = entry.Name;
            if (entry.PageName is not null)
                name += $";{entry.PageName}";

            xmlEntries.Add(new SerializedKupEntry
            {
                Name = name,
                OriginalText = entry.OriginalText,
                EditedText = entry.TranslatedText
            });
        }

        var root = new SerializedKup
        {
            Entries = new SerializedKupEntries
            {
                Entry = [.. xmlEntries]
            }
        };

        using var writer = XmlWriter.Create(output);
        KupXmlProvider.Write(root, writer);
    }
}