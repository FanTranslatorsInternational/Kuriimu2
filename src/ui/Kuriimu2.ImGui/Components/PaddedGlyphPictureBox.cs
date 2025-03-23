using System.Numerics;
using ImGui.Forms;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Resources;
using ImGuiNET;
using Kuriimu2.ImGui.Models.Forms.Dialogs.Font;
using SixLabors.ImageSharp;
using Rectangle = Veldrid.Rectangle;

namespace Kuriimu2.ImGui.Components
{
    internal class PaddedGlyphPictureBox : ZoomableComponent
    {
        private PaddedGlyph? _paddedGlyph;
        private ImageResource? _paddedGlyphResource;

        public void SetPaddedGlyph(PaddedGlyph? paddedGlyph)
        {
            _paddedGlyph = paddedGlyph;
            _paddedGlyphResource = paddedGlyph?.Glyph == null ? null : ImageResource.FromImage(paddedGlyph.Glyph);
        }

        protected override void DrawInternal(Rectangle contentRect)
        {
            if (_paddedGlyph == null)
                return;

            DrawGlyph(contentRect);
            DrawUnpaddedBox(contentRect);
            DrawBaseline(contentRect);
        }

        private void DrawGlyph(Rectangle contentRect)
        {
            if (_paddedGlyphResource == null)
                return;

            var imageStartPosition = -(_paddedGlyphResource.Size / 2);
            var imageRect = new Rectangle((int)imageStartPosition.X, (int)imageStartPosition.Y, _paddedGlyphResource.Width, _paddedGlyphResource.Height);
            imageRect = Transform(contentRect, imageRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddImage((nint)_paddedGlyphResource, imageRect.Position, imageRect.Position + imageRect.Size);
            ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Color.Gold.ToUInt32());
        }

        private void DrawUnpaddedBox(Rectangle contentRect)
        {
            var unpaddedWidth = _paddedGlyphResource.Width - _paddedGlyph!.PaddingLeft - _paddedGlyph!.PaddingRight;
            var imageStartPosition = -(_paddedGlyphResource.Size / 2 - new Vector2(_paddedGlyph!.PaddingLeft, 0));
            var imageRect = new Rectangle((int)imageStartPosition.X, (int)imageStartPosition.Y, unpaddedWidth, _paddedGlyphResource.Height);
            imageRect = Transform(contentRect, imageRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Style.GetColor(ImGuiCol.Border).ToUInt32(), 0, ImDrawFlags.None, 3f);
        }

        private void DrawBaseline(Rectangle contentRect)
        {
            var baseLineRect = new Rectangle(-(_paddedGlyph!.Glyph.Width / 2), _paddedGlyph!.Baseline - _paddedGlyphResource.Height / 2, _paddedGlyphResource.Width, 0);
            var baseLineRectTransformed = Transform(contentRect, baseLineRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddLine(baseLineRectTransformed.Position, baseLineRectTransformed.Position + baseLineRectTransformed.Size,
                Color.OrangeRed.ToUInt32(), 3f);
        }
    }
}
