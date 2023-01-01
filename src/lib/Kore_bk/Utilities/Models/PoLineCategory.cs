namespace Kore.Utilities.Models
{
    enum PoLineCategory
    {
        // Comment types
        NormalComment,
        ExtractedComment,
        Flags,
        SourceReference,

        // Translation types
        MessageContext,
        MessageId,
        MessageString,

        // Misc
        String,
        Blank
    }
}
