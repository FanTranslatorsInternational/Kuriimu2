using Hexa.NET.ImGui;
using ImGui.Forms;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Resources;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using SixLabors.ImageSharp;
using System;
using System.Numerics;
using Rectangle = ImGui.Forms.Support.Rectangle;

namespace Kuriimu2.ImGui.Components
{
    internal class ZoomableCharacterInfo : ZoomableComponent
    {
        private ImageResource? _glyphResource;

        public CharacterInfo? CharacterInfo { get; private set; }
        public ThemedColor BackgroundColor { get; set; }

        public void SetCharacterInfo(CharacterInfo characterInfo)
        {
            CharacterInfo = characterInfo;

            _glyphResource = characterInfo.Glyph is not null
                ? ImageResource.FromImage(characterInfo.Glyph)
                : null;
        }

        protected override void DrawInternal(Rectangle contentRect)
        {
            base.DrawInternal(contentRect);

            if (CharacterInfo == null)
                return;

            int boundingX = Math.Min(CharacterInfo.GlyphPosition.X, 0);
            int boundingY = Math.Min(CharacterInfo.GlyphPosition.Y, 0);
            int boundingWidth = Math.Max(CharacterInfo.GlyphPosition.X, 0) + Math.Max(CharacterInfo.Glyph?.Width ?? 0, CharacterInfo.BoundingBox.Width);
            int boundingHeight = Math.Max(CharacterInfo.GlyphPosition.Y, 0) + Math.Max(CharacterInfo.Glyph?.Height ?? 0, CharacterInfo.BoundingBox.Height);

            var totalBoundingBox = new Rectangle(new Vector2(boundingX, boundingY), new Vector2(boundingWidth - boundingX, boundingHeight - boundingY));

            DrawBackground(contentRect);

            DrawGlyph(contentRect, totalBoundingBox);
            DrawBoundingBox(contentRect, totalBoundingBox);
            DrawTotalBoundingBox(contentRect, totalBoundingBox);

            DrawMousePixelPosition(contentRect, totalBoundingBox);
        }

        private void DrawBackground(Rectangle contentRect)
        {
            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRect(contentRect.Position, contentRect.Position + contentRect.Size, BackgroundColor.ToUInt32());
        }

        private void DrawGlyph(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            if (CharacterInfo is null || _glyphResource is null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2) + new Vector2(Math.Max(CharacterInfo.GlyphPosition.X, 0), Math.Max(CharacterInfo.GlyphPosition.Y, 0));
            var imageRect = new Rectangle(boundingStartPosition, _glyphResource.Size);
            imageRect = Transform(contentRect, imageRect);

            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddImage(_glyphResource.GetTextureRef(), imageRect.Position, imageRect.Position + imageRect.Size);
        }

        private void DrawBoundingBox(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            if (CharacterInfo == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2) + new Vector2(Math.Max(CharacterInfo.GlyphPosition.X, 0), Math.Max(CharacterInfo.GlyphPosition.Y, 0));
            var imageRect = new Rectangle(boundingStartPosition, new Vector2(CharacterInfo.BoundingBox.Width, CharacterInfo.BoundingBox.Height));
            imageRect = Transform(contentRect, imageRect);

            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Color.OrangeRed.ToUInt32() & 0x00FFFFFF | 0x7F000000);
        }

        private void DrawTotalBoundingBox(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            if (CharacterInfo == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2);
            var imageRect = new Rectangle(boundingStartPosition, totalBoundingBox.Size);
            imageRect = Transform(contentRect, imageRect);

            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Color.Gold.ToUInt32() & 0x00FFFFFF | 0x7F000000);
        }

        private void DrawMousePixelPosition(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            if (!Hexa.NET.ImGui.ImGui.IsItemHovered())
                return;

            var mousePos = Hexa.NET.ImGui.ImGui.GetMousePos();
            if (!contentRect.Contains(mousePos))
                return;

            Vector2 boundingStartPosition = new Vector2(totalBoundingBox.Width, totalBoundingBox.Height) / 2;

            mousePos = UnTransform(contentRect, mousePos);
            mousePos += boundingStartPosition;

            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddText(contentRect.Position, Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.Text), $"X: {(int)Math.Floor(mousePos.X)}");
            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddText(contentRect.Position + new Vector2(0, TextMeasurer.GetCurrentLineHeight()), Hexa.NET.ImGui.ImGui.GetColorU32(ImGuiCol.Text), $"Y: {(int)Math.Floor(mousePos.Y)}");
        }
    }
}
