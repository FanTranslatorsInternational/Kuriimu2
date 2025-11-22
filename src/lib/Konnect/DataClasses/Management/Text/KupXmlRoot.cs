using System.Xml.Serialization;

namespace Konnect.DataClasses.Management.Text;

[XmlRoot("kup")]
public class KupXmlRoot
{
    [XmlElement("entries")]
    public required KupXmlEntries Entries { get; init; }
}