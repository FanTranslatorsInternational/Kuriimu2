namespace Konnect.DataClasses.Management.Text;

public class TranslationFileEntry
{
    public required string Name { get; init; }

    public string? PageName { get; init; }

    public required string OriginalText { get; init; }

    public required string TranslatedText { get; init; }
}