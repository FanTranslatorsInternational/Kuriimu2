using System.Xml.Serialization;

namespace Konnect.DataClasses.Management.Text;

[XmlRoot("element")]
public class KupXmlEntry
{
    [XmlAttribute("name")]
    public required string Name { get; init; }

    [XmlElement("original")]
    public required string OriginalText { get; init; }

    [XmlElement("edited")]
    public required string EditedText { get; init; }
}