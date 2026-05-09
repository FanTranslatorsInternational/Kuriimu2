using System.Xml;
using Konnect.Contract.DataClasses.Management.Font;
using Konnect.Contract.Management.Font;
using Konnect.DataClasses.Management.Font;

namespace Konnect.Management.Font;

public class FontProfileManager : IFontProfileManager
{
    public FontProfile? Load(string filePath)
    {
        using var reader = XmlReader.Create(File.OpenRead(filePath));

        SerializedFontProfile? serializedProfile = SerializedFontProfileXmlProvider.Read(reader);
        if (serializedProfile is null)
            return null;

        return new FontProfile
        {
            FontFamily = serializedProfile.FontFamily,
            IsBold = serializedProfile.Bold,
            IsItalic = serializedProfile.Italic,
            FontSize = serializedProfile.FontSize,
            Baseline = serializedProfile.Baseline,
            GlyphHeight = serializedProfile.GlyphHeight,
            SpaceWidth = serializedProfile.SpaceWidth,
            Characters = serializedProfile.Characters,
            Paddings = serializedProfile.AdjustedCharacters.AdjustedCharacter
                .ToDictionary(x => (char)x.Character, y => (y.Padding.Left, y.Padding.Right))
        };
    }

    public void Save(string filePath, FontProfile profile)
    {
        var serializedProfile = new SerializedFontProfile
        {
            FontFamily = profile.FontFamily,
            Bold = profile.IsBold,
            Italic = profile.IsItalic,
            FontSize = profile.FontSize,
            Baseline = profile.Baseline,
            GlyphHeight = profile.GlyphHeight,
            SpaceWidth = profile.SpaceWidth,
            Characters = profile.Characters,
            AdjustedCharacters = new AdjustedCharacters
            {
                AdjustedCharacter = [..profile.Paddings
                    .Where(x => x.Value.Item1 != 0 || x.Value.Item2 != 0)
                    .Select(p => new AdjustedCharacter
                    {
                        Character = p.Key,
                        Padding = new Padding
                        {
                            Left = p.Value.Item1,
                            Right = p.Value.Item2
                        }
                    })]
            }
        };

        using var writer = XmlWriter.Create(File.Create(filePath));

        SerializedFontProfileXmlProvider.Write(serializedProfile, writer);
    }
}