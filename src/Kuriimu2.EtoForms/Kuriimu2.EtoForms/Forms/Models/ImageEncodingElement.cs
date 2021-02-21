using Kontract.Kanvas;
using Kontract.Models.Image;

namespace Kuriimu2.EtoForms.Forms.Models
{
    class ImageEncodingElement
    {
        private readonly IColorEncoding _colorEncoding;
        private readonly IndexEncodingDefinition _indexEncoding;

        public int ImageIdent { get; }

        public ImageEncodingElement(int identifier,IColorEncoding colorEncoding)
        {
            _colorEncoding = colorEncoding;
            ImageIdent = identifier;
        }

        public ImageEncodingElement(int identifier, IndexEncodingDefinition indexEncodingDefinition)
        {
            _indexEncoding = indexEncodingDefinition;
            ImageIdent = identifier;
        }

        public override string ToString()
        {
            return _colorEncoding?.FormatName ?? _indexEncoding.IndexEncoding.FormatName;
        }
    }
}
