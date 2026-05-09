using System.Xml.Serialization;

namespace Konnect.DataClasses.Management.Text;

[XmlRoot("entries")]
public class SerializedKupEntries
{
    [XmlElement("entry")]
    public required SerializedKupEntry[] Entry { get; init; }
}

[XmlRoot("element")]
public class SerializedKupEntry
{
    [XmlAttribute("name")]
    public required string Name { get; init; }

    [XmlElement("original")]
    public required string OriginalText { get; init; }

    [XmlElement("edited")]
    public required string EditedText { get; init; }
}

[XmlRoot("kup")]
public class SerializedKup
{
    [XmlElement("entries")]
    public required SerializedKupEntries Entries { get; init; }
}
