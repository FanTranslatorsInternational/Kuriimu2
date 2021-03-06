using System.Drawing;

namespace Kontract.Kanvas
{
    public interface IColorShader
    {
        Color Read(Color c);

        Color Write(Color c);
    }
}
