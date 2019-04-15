using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Kontract.Interfaces.Text;

namespace plugin_kuriimu.KUP
{
    /// <summary>
    /// KUP is Kuriimu's basic transport format for text.
    /// </summary>
    [XmlRoot("kup")]
    public sealed class KUP
    {
        /// <summary>
        /// The text entries stored in the file.
        /// </summary>
        [XmlArray("entries")]
        [XmlArrayItem("entry")]
        public List<TextEntry> Entries { get; set; }

        /// <summary>
        /// Creates a new KUP file.
        /// </summary>
        public KUP()
        {
            Entries = new List<TextEntry>();
        }

        /// <summary>
        /// Load a KUP files into memory.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static KUP Load(Stream input)
        {
            var xmlSettings = new XmlReaderSettings { CheckCharacters = false };
            return (KUP)new XmlSerializer(typeof(KUP)).Deserialize(XmlReader.Create(input, xmlSettings));
        }

        /// <summary>
        /// Save the text to a KUP file.
        /// </summary>
        /// <param name="output"></param>
        public void Save(Stream output)
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

                using (var xmlIO = new StreamWriter(output, xmlSettings.Encoding, 0x1000, true))
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
