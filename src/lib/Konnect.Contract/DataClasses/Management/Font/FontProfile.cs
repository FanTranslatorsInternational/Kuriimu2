namespace Konnect.Contract.DataClasses.Management.Font;

public class FontProfile
{
    public string? FontFamily { get; set; }
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public int FontSize { get; set; } = 12;
    public int Baseline { get; set; } = 18;
    public int GlyphHeight { get; set; } = 32;
    public int SpaceWidth { get; set; } = 4;
    public string Characters { get; set; } = "abcdefghijklmnopqrstuvwxyz\nABCDEFGHIJKLMNOPQRSTUVWXYZ\n0123456789";
    public IDictionary<char, (int, int)> Paddings { get; set; } = new Dictionary<char, (int, int)>();
}