using System.Xml.Serialization;

namespace Konnect.DataClasses.Management.Font;

[XmlRoot(ElementName = "padding")]
public class Padding
{

    [XmlElement(ElementName = "Left")]
    public int Left { get; set; }

    [XmlElement(ElementName = "Right")]
    public int Right { get; set; }
}

[XmlRoot(ElementName = "adjustedCharacter")]
public class AdjustedCharacter
{

    [XmlElement(ElementName = "character")]
    public int Character { get; set; }

    [XmlElement(ElementName = "padding")]
    public Padding Padding { get; set; }
}

[XmlRoot(ElementName = "adjustedCharacters")]
public class AdjustedCharacters
{

    [XmlElement(ElementName = "adjustedCharacter")]
    public List<AdjustedCharacter> AdjustedCharacter { get; set; }
}

[XmlRoot(ElementName = "profile")]
public class SerializedFontProfile
{
    [XmlElement(ElementName = "adjustedCharacters")]
    public AdjustedCharacters AdjustedCharacters { get; set; }

    [XmlElement(ElementName = "fontFamily")]
    public string FontFamily { get; set; }

    [XmlElement(ElementName = "fontSize")]
    public int FontSize { get; set; }

    [XmlElement(ElementName = "baseline")]
    public int Baseline { get; set; }

    [XmlElement(ElementName = "glyphHeight")]
    public int GlyphHeight { get; set; }

    [XmlElement(ElementName = "bold")]
    public bool Bold { get; set; }

    [XmlElement(ElementName = "italic")]
    public bool Italic { get; set; }

    [XmlElement(ElementName = "textRenderingHint")]
    public string TextRenderingHint { get; set; }

    [XmlElement(ElementName = "characters")]
    public string Characters { get; set; }

    [XmlElement(ElementName = "spaceWidth")]
    public int SpaceWidth { get; set; }

    [XmlElement(ElementName = "showDebugBoxes")]
    public bool ShowDebugBoxes { get; set; }
}