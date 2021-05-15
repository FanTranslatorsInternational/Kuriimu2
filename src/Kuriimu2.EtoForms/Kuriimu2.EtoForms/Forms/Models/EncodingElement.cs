using System.Text;

namespace Kuriimu2.EtoForms.Forms.Models
{
    class EncodingElement
    {
        public Encoding Encoding { get; }

        public EncodingElement(Encoding encoding)
        {
            Encoding = encoding;
        }

        public override string ToString()
        {
            return Encoding.EncodingName.Replace("US-", "");
        }
    }
}
