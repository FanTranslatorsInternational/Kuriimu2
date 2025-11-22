using System.Xml.Serialization;

namespace Konnect.DataClasses.Management.Text;

[XmlRoot("entries")]
public class KupXmlEntries
{
    [XmlElement("entry")]
    public required KupXmlEntry[] Entry { get; init; }
}