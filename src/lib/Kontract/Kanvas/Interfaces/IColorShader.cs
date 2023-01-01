using System.Drawing;

namespace Kontract.Kanvas.Interfaces
{
    public interface IColorShader
    {
        Color Read(Color c);

        Color Write(Color c);
    }
}
