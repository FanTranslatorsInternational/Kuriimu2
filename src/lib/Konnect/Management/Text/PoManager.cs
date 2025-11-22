using System.Text;
using Konnect.DataClasses.Management.Text;
using Konnect.Exceptions.Management.Text;

namespace Konnect.Management.Text;

public static class PoManager
{
    public static TranslationFileEntry[] Load(Stream input)
    {
        string poText = new StreamReader(input).ReadToEnd();

        var result = new List<TranslationFileEntry>();

        string? reference = null;
        string? msgId = null;
        string? msgStr = null;

        string[] lines = poText.Split(Environment.NewLine);
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("#:"))
            {
                if (reference is not null)
                    throw new PoFileMalformedException(i + 1);

                reference = lines[i].Length > 2 ? lines[i][2..].Trim() : string.Empty;
            }
            else if (lines[i].StartsWith("msgid"))
            {
                if (msgId is not null)
                    throw new PoFileMalformedException(i + 1);

                int beginIndex = lines[i].IndexOf('"');
                if (beginIndex < 0)
                    throw new PoFileMalformedException(i + 1);

                int endIndex = lines[i].IndexOf('"', beginIndex + 1);
                if (endIndex < 0)
                    throw new PoFileMalformedException(i + 1);

                msgId = lines[i][(beginIndex + 1)..endIndex];
            }
            else if (lines[i].StartsWith("msgstr"))
            {
                if (msgStr is not null)
                    throw new PoFileMalformedException(i + 1);

                int beginIndex = lines[i].IndexOf('"');
                if (beginIndex < 0)
                    throw new PoFileMalformedException(i + 1);

                int endIndex = lines[i].IndexOf('"', beginIndex + 1);
                if (endIndex < 0)
                    throw new PoFileMalformedException(i + 1);

                msgStr = lines[i][(beginIndex + 1)..endIndex];
            }

            if (reference is null || msgId is null || msgStr is null)
                continue;

            int pageIndex = reference.IndexOf(';');

            result.Add(new TranslationFileEntry
            {
                Name = pageIndex < 0 ? reference : reference[..pageIndex],
                PageName = pageIndex < 0 ? null : reference[(pageIndex + 1)..],
                OriginalText = msgId.Replace("\\\"", "\"").Replace("\\t", "\t").Replace("\\n", "\n"),
                TranslatedText = msgStr.Replace("\\\"", "\"").Replace("\\t", "\t").Replace("\\n", "\n")
            });

            reference = null;
            msgId = null;
            msgStr = null;
        }

        return [.. result];
    }

    public static void Save(Stream output, TranslationFileEntry[] entries)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < entries.Length; i++)
        {
            sb.Append($"#: {entries[i].Name}");
            if (entries[i].PageName is null)
                sb.AppendLine();
            else
                sb.AppendLine($";{entries[i].PageName}");

            sb.AppendLine($"msgid \"{entries[i].OriginalText.Replace("\"", "\\\"").Replace("\t", "\\t").Replace("\r\n", "\\n").Replace("\n", "\\n")}\"");
            sb.AppendLine($"msgstr \"{entries[i].TranslatedText.Replace("\"", "\\\"").Replace("\t", "\\t").Replace("\r\n", "\\n").Replace("\n", "\\n")}\"");

            if (i + 1 < entries.Length)
                sb.AppendLine();
        }

        var poText = sb.ToString();

        var writer = new StreamWriter(output);
        writer.Write(poText);

        writer.Dispose();
    }
}