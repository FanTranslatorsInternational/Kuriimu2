using System;
using System.Numerics;
using ImGui.Forms;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Resources;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using SixLabors.ImageSharp;
using Rectangle = Veldrid.Rectangle;

namespace Kuriimu2.ImGui.Components
{
    class ZoomableCharacterInfo : ZoomableComponent
    {
        private ImageResource? _glyphResource;

        public CharacterInfo? CharacterInfo { get; private set; }
        public ThemedColor BackgroundColor { get; set; }

        public void SetCharacterInfo(CharacterInfo characterInfo)
        {
            CharacterInfo = characterInfo;

            if (characterInfo.Glyph is not null)
                _glyphResource = ImageResource.FromImage(characterInfo.Glyph);
        }

        protected override void DrawInternal(Rectangle contentRect)
        {
            if (CharacterInfo == null)
                return;

            int totalWidth = Math.Max(CharacterInfo.GlyphPosition.X + (CharacterInfo.Glyph?.Width ?? 0), CharacterInfo.BoundingBox.Width);
            int totalHeight = Math.Max(CharacterInfo.GlyphPosition.Y + (CharacterInfo.Glyph?.Height ?? 0), CharacterInfo.BoundingBox.Height);
            int boundingX = Math.Min(CharacterInfo.GlyphPosition.X + (CharacterInfo.Glyph?.Width ?? 0), 0);
            int boundingY = Math.Min(CharacterInfo.GlyphPosition.Y + (CharacterInfo.Glyph?.Height ?? 0), 0);

            var totalBoundingBox = new Rectangle(boundingX, boundingY, totalWidth, totalHeight);

            DrawBackground(contentRect);
            DrawGlyph(contentRect);
            DrawBoundingBox(contentRect);
            DrawTotalBoundingBox(contentRect, totalBoundingBox);
        }

        private void DrawBackground(Rectangle contentRect)
        {
            ImGuiNET.ImGui.GetWindowDrawList().AddRect(contentRect.Position, contentRect.Position + contentRect.Size, BackgroundColor.ToUInt32());
        }

        private void DrawGlyph(Rectangle contentRect)
        {
            if (CharacterInfo is null || _glyphResource is null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(CharacterInfo.BoundingBox.Width, CharacterInfo.BoundingBox.Height) / 2) + CharacterInfo.GlyphPosition;
            var imageRect = new Rectangle((int)boundingStartPosition.X, (int)boundingStartPosition.Y, _glyphResource.Width, _glyphResource.Height);
            imageRect = Transform(contentRect, imageRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddImage((nint)_glyphResource, imageRect.Position, imageRect.Position + imageRect.Size);
        }

        private void DrawBoundingBox(Rectangle contentRect)
        {
            if (CharacterInfo == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(CharacterInfo.BoundingBox.Width, CharacterInfo.BoundingBox.Height) / 2);
            var imageRect = new Rectangle((int)boundingStartPosition.X, (int)boundingStartPosition.Y, CharacterInfo.BoundingBox.Width, CharacterInfo.BoundingBox.Height);
            imageRect = Transform(contentRect, imageRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Color.WhiteSmoke.ToUInt32());
        }

        private void DrawTotalBoundingBox(Rectangle contentRect, Rectangle totalBoundingBox)
        {
            if (CharacterInfo == null)
                return;

            Vector2 boundingStartPosition = -(new Vector2(CharacterInfo.BoundingBox.Width, CharacterInfo.BoundingBox.Height) / 2) + totalBoundingBox.Position;
            var imageRect = new Rectangle((int)boundingStartPosition.X, (int)boundingStartPosition.Y, totalBoundingBox.Width, totalBoundingBox.Height);
            imageRect = Transform(contentRect, imageRect);

            ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Color.White.ToUInt32());
        }
    }
}
