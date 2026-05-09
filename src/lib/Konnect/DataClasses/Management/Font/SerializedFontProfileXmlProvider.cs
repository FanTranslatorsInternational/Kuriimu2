using System.Xml;

namespace Konnect.DataClasses.Management.Font
{
    internal class SerializedFontProfileXmlProvider
    {
        public static void Write(SerializedFontProfile profile, XmlWriter writer)
        {
            writer.WriteStartDocument();

            writer.WriteStartElement("profile");

            // adjustedCharacters
            writer.WriteStartElement("adjustedCharacters");

            foreach (AdjustedCharacter adjustedCharacter in profile.AdjustedCharacters.AdjustedCharacter)
            {
                writer.WriteStartElement("adjustedCharacter");

                writer.WriteElementString("character", adjustedCharacter.Character.ToString());

                writer.WriteStartElement("padding");

                writer.WriteElementString("Left", adjustedCharacter.Padding.Left.ToString());

                writer.WriteElementString("Right", adjustedCharacter.Padding.Right.ToString());

                writer.WriteEndElement(); // padding

                writer.WriteEndElement(); // adjustedCharacter
            }

            writer.WriteEndElement(); // adjustedCharacters

            // Remaining profile fields
            writer.WriteElementString("fontFamily", profile.FontFamily ?? string.Empty);
            writer.WriteElementString("fontSize", profile.FontSize.ToString());
            writer.WriteElementString("baseline", profile.Baseline.ToString());
            writer.WriteElementString("glyphHeight", profile.GlyphHeight.ToString());
            writer.WriteElementString("bold", profile.Bold.ToString());
            writer.WriteElementString("italic", profile.Italic.ToString());
            writer.WriteElementString("textRenderingHint", profile.TextRenderingHint ?? string.Empty);
            writer.WriteElementString("characters", profile.Characters);
            writer.WriteElementString("spaceWidth", profile.SpaceWidth.ToString());
            writer.WriteElementString("showDebugBoxes", profile.ShowDebugBoxes.ToString());

            writer.WriteEndElement(); // profile

            writer.WriteEndDocument();
        }

        public static SerializedFontProfile? Read(XmlReader reader)
        {
            try
            {
                return ReadFontProfile(reader);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static SerializedFontProfile ReadFontProfile(XmlReader reader)
        {
            SerializedFontProfile profile = new()
            {
                AdjustedCharacters = new AdjustedCharacters
                {
                    AdjustedCharacter = []
                },
                Characters = string.Empty
            };

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                switch (reader.Name)
                {
                    case "adjustedCharacter":
                    {
                        AdjustedCharacter adjustedCharacter = ReadAdjustedCharacter(reader);
                        profile.AdjustedCharacters.AdjustedCharacter.Add(adjustedCharacter);
                        break;
                    }

                    case "fontFamily":
                        profile.FontFamily = reader.ReadElementContentAsString();
                        break;

                    case "fontSize":
                        profile.FontSize = reader.ReadElementContentAsInt();
                        break;

                    case "baseline":
                        profile.Baseline = reader.ReadElementContentAsInt();
                        break;

                    case "glyphHeight":
                        profile.GlyphHeight = reader.ReadElementContentAsInt();
                        break;

                    case "bold":
                        profile.Bold = reader.ReadElementContentAsBoolean();
                        break;

                    case "italic":
                        profile.Italic = reader.ReadElementContentAsBoolean();
                        break;

                    case "textRenderingHint":
                        profile.TextRenderingHint = reader.ReadElementContentAsString();
                        break;

                    case "characters":
                        profile.Characters = reader.ReadElementContentAsString();
                        break;

                    case "spaceWidth":
                        profile.SpaceWidth = reader.ReadElementContentAsInt();
                        break;

                    case "showDebugBoxes":
                        profile.ShowDebugBoxes = reader.ReadElementContentAsBoolean();
                        break;
                }
            }

            return profile;
        }

        private static AdjustedCharacter ReadAdjustedCharacter(XmlReader reader)
        {
            AdjustedCharacter adjustedCharacter = new()
            {
                Padding = new Padding()
            };

            using XmlReader subtree = reader.ReadSubtree();

            while (subtree.Read())
            {
                if (subtree.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                switch (subtree.Name)
                {
                    case "character":
                        adjustedCharacter.Character = subtree.ReadElementContentAsInt();
                        break;

                    case "Left":
                        adjustedCharacter.Padding.Left = subtree.ReadElementContentAsInt();
                        break;

                    case "Right":
                        adjustedCharacter.Padding.Right = subtree.ReadElementContentAsInt();
                        break;
                }
            }

            return adjustedCharacter;
        }
    }
}
