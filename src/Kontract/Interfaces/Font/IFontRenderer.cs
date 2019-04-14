using System.Drawing;

namespace Kontract.Interfaces.Font
{
    /// <inheritdoc />
    /// <summary>
    /// This is the font renderer interface for creating font rendering plugins.
    /// </summary>
    public interface IFontRenderer : IFontAdapter2
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        CharWidthInfo GetCharWidthInfo(char c);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="stopChar"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        float MeasureString(string text, char stopChar, float scale = 1.0f);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        void SetColor(Color color);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="gfx"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scaleX"></param>
        /// <param name="scaleY"></param>
        void Draw(char c, Graphics gfx, float x, float y, float scaleX, float scaleY);
    }
}
