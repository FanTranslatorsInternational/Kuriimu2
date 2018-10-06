using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Kontract.Interfaces;

namespace Kore.SamplePlugins
{
    [XmlRoot("lot")]
    public sealed class Lot
    {
        #region Properties

        [XmlArray("lotEntries")]
        [XmlArrayItem("lotEntry")]
        public List<LotEntry> LotEntries { get; set; }

        #endregion

        public Lot()
        {
            LotEntries = new List<LotEntry>();
        }

        public void Populate(List<TextEntry> entries)
        {
            foreach (var entry in entries)
            {
                var lotEntry = new LotEntry { Entry = entry.Name };
                LotEntries.Add(lotEntry);
            }
        }

        public static Lot Load(string filename)
        {
            var xmlSettings = new XmlReaderSettings {CheckCharacters = false};

            using (var fs = File.OpenRead(filename))
                return (Lot)new XmlSerializer(typeof(Lot)).Deserialize(XmlReader.Create(fs, xmlSettings));
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
                    var serializer = new XmlSerializer(typeof(Lot));
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

    public sealed class LotEntry
    {
        [XmlAttribute("entry")]
        public string Entry { get; set; }

        [XmlAttribute("labelID")]
        public string LabelID { get; set; }

        [XmlElement("notes")]
        public string Notes { get; set; }

        [XmlElement("screenshot")]
        public List<Screenshot> Screenshots { get; set; }

        public LotEntry()
        {
            Entry = string.Empty;
            LabelID = string.Empty;
            Notes = string.Empty;
            Screenshots = new List<Screenshot>();
        }
    }

    public sealed class Screenshot
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlText]
        public string BitmapBase64 { get; set; }

        [XmlIgnore]
        public Bitmap Bitmap
        {
            get
            {
                var bytes = Convert.FromBase64String(BitmapBase64);
                using (var ms = new MemoryStream())
                {
                    ms.Write(bytes, 0, bytes.Length);
                    return new Bitmap(ms);
                }
            }
            set
            {
                using (var ms = new MemoryStream())
                {
                    value.Save(ms, ImageFormat.Png);
                    var bytes = ms.ToArray();
                    BitmapBase64 = Convert.ToBase64String(bytes);
                }
            }
        }

        public Screenshot()
        {
            Name = string.Empty;
            BitmapBase64 = string.Empty;
        }
    }
}
