using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Kontract.Interfaces;

namespace Kore.SamplePlugins
{
    [XmlRoot("kup")]
    public sealed class KUP
    {
        [XmlArray("entries")]
        [XmlArrayItem("entry")]
        public List<TextEntry> Entries { get; set; }

        public KUP()
        {
            Entries = new List<TextEntry>();
        }

        public static KUP Load(string filename)
        {
            var xmlSettings = new XmlReaderSettings {CheckCharacters = false};

            using (var fs = File.OpenRead(filename))
                return (KUP)new XmlSerializer(typeof(KUP)).Deserialize(XmlReader.Create(fs, xmlSettings));
        }

        public void Save(string filename)
        {
            try
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
                    var serializer = new XmlSerializer(typeof(KUP));
                    var namespaces = new XmlSerializerNamespaces();
                    namespaces.Add(string.Empty, string.Empty);
                    serializer.Serialize(XmlWriter.Create(xmlIO, xmlSettings), this, namespaces);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
