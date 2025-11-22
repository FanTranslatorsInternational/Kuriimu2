using System.Xml.Serialization;
using Konnect.Contract.DataClasses.Management.Font;
using Konnect.Contract.Management.Font;
using Konnect.DataClasses.Management.Font;

namespace Konnect.Management.Font;

public class FontProfileManager : IFontProfileManager
{
    public FontProfile? Load(string filePath)
    {
        var serializer = new XmlSerializer(typeof(SerializedFontProfile));
        using var reader = new StreamReader(File.OpenRead(filePath));

        var serializedProfile = (SerializedFontProfile?)serializer.Deserialize(reader);
        if (serializedProfile == null)
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
                AdjustedCharacter = profile.Paddings
                    .Where(x => x.Value.Item1 != 0 || x.Value.Item2 != 0)
                    .Select(p => new AdjustedCharacter
                    {
                        Character = p.Key,
                        Padding = new Padding
                        {
                            Left = p.Value.Item1,
                            Right = p.Value.Item2
                        }
                    }).ToList()
            }
        };

        var serializer = new XmlSerializer(typeof(SerializedFontProfile));
        using var reader = new StreamWriter(File.Create(filePath));

        serializer.Serialize(reader, serializedProfile);
    }
}