using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Kanvas.Configuration
{
    public interface IImageConfiguration
    {
        IImageConfiguration WithImageSize(Size size);

        IImageConfiguration WithPaddedImageSize(Size size);

        IImageConfiguration WithSwizzle(Func<Size, IImageSwizzle> func);

        IColorConfiguration TranscodeWith(Func<Size, IColorEncoding> func);

        IIndexConfiguration TranscodeWith(Func<Size, IColorIndexEncoding> func);
    }
}
