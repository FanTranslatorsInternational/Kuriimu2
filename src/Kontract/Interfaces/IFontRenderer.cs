using System.Drawing;

namespace Kontract.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// This is the font renderer interface for creating font rendering plugins.
    /// </summary>
    public interface IFontRenderer : IFontAdapter2
    {
        // We so ALPHA up in here~ XD

        CharWidthInfo GetCharWidthInfo(char c);

        float MeasureString(string text, char stopChar, float scale = 1.0f);

        void SetColor(Color color);

        void Draw(char c, Graphics g, float x, float y, float scaleX, float scaleY);
    }
}
