using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace Konnect.Contract.Plugin.File.Text;

public interface ITextEntryPager
{
    TextEntryPage[] Page(IReadOnlyList<TextEntry> entries);
}