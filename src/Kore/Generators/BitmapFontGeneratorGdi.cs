using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Kore.Generators
{
    [XmlRoot("profile")]
    public class BitmapFontGeneratorGdiProfile
    {
        [XmlElement("padding")]
        public Padding GlyphPadding { get; set; }

        [XmlArray("adjustedCharacters")]
        [XmlArrayItem("adjustedCharacter")]
        public List<AdjustedCharacter> AdjustedCharacters { get; set; }

        [XmlElement("fontFamily")]
        public string FontFamily { get; set; } = "Arial";

        [XmlElement("fontSize")]
        public float FontSize { get; set; } = 24;

        [XmlElement("baseline")]
        public float Baseline { get; set; } = 30;

        [XmlElement("glyphHeight")]
        public int GlyphHeight { get; set; } = 36;

        [XmlElement("bold")]
        public bool Bold { get; set; }

        [XmlElement("italic")]
        public bool Italic { get; set; }

        [XmlElement("textRenderingHint")]
        public string TextRenderingHint { get; set; } = "AntiAlias";

        [XmlElement("characters")]
        public string Characters { get; set; }

        [XmlElement("spaceWidth")]
        public float SpaceWidth { get; set; } = 5f;

        [XmlElement("showDebugBoxes")]
        public bool ShowDebugBoxes { get; set; }

        public BitmapFontGeneratorGdiProfile()
        {
            AdjustedCharacters = new List<AdjustedCharacter>();
        }

        public static BitmapFontGeneratorGdiProfile Load(string filename)
        {
            var xmlSettings = new XmlReaderSettings { CheckCharacters = false };

            using (var fs = File.OpenRead(filename))
            {
                return (BitmapFontGeneratorGdiProfile)new XmlSerializer(typeof(BitmapFontGeneratorGdiProfile)).Deserialize(XmlReader.Create(fs, xmlSettings));
            }
        }

        public void Save(string filename)
        {
            var xmlSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                NewLineOnAttributes = false,
                NewLineHandling = NewLineHandling.Entitize,
                IndentChars = "	",
                CheckCharacters = false
            };

            using (var xmlIO = new StreamWriter(filename, false, xmlSettings.Encoding))
            {
                var serializer = new XmlSerializer(typeof(BitmapFontGeneratorGdiProfile));
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);
                serializer.Serialize(XmlWriter.Create(xmlIO, xmlSettings), this, namespaces);
            }
        }
    }

    [XmlRoot("adjustedCharacter")]
    public class AdjustedCharacter
    {
        [XmlElement("character")]
        public char Character { get; set; }

        [XmlElement("padding")]
        public Padding Padding { get; set; }
    }
}
