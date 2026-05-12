using System;
using System.Numerics;
using Hexa.NET.ImGui;
using ImGui.Forms;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Extensions;
using ImGui.Forms.Resources;
using ImGui.Forms.Support;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Kuriimu2.ImGui.Resources;
using Size = ImGui.Forms.Models.Size;

namespace Kuriimu2.ImGui.Components
{
    internal class GlyphElement : Component
    {
        private static readonly Vector2 GlyphMaxSize = new(36, 36);

        private readonly FontResource _mainFont = FontResources.GetFont(FontType.Application, 15);
        private readonly FontResource _codeFont = FontResources.GetFont(FontType.Hexadecimal, 11);

        private readonly ThemedImageResource? _glyph;

        public CharacterInfo CharacterInfo { get; }

        public ThemedColor BackgroundColor { get; set; }

        public bool IsSelected { get; set; }

        public event EventHandler? SelectedChanged;

        public GlyphElement(CharacterInfo charInfo)
        {
            CharacterInfo = charInfo;

            if (charInfo.Glyph != null)
                _glyph = ImageResource.FromImage(charInfo.Glyph);

        }

        public override Size GetSize() => Size.Content;

        protected override void UpdateInternal(Rectangle contentRect)
        {
            // Draw background
            if (!BackgroundColor.IsEmpty)
                Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRectFilled(contentRect.Position, contentRect.Position + contentRect.Size, BackgroundColor.ToUInt32());

            // Draw selection
            bool isSelected = IsSelected;
            Hexa.NET.ImGui.ImGui.Selectable($"##{Id}", ref isSelected, ImGuiSelectableFlags.None, contentRect.Size);

            if (IsSelected != isSelected)
            {
                IsSelected = isSelected;
                OnSelectedChanged();
            }

            // Draw glyph
            if (_glyph != null)
            {
                var imageSize = new Vector2(Math.Min(GlyphMaxSize.X, _glyph.Width), Math.Min(GlyphMaxSize.Y, _glyph.Height));
                var imgPosition = contentRect.Position + (GlyphMaxSize - imageSize) / 2;

                Hexa.NET.ImGui.ImGui.SetCursorScreenPos(imgPosition);
                Hexa.NET.ImGui.ImGui.Image(_glyph.GetTextureRef(), imageSize);
            }

            // Draw character and code
            var character = $"{CharacterInfo.CodePoint}";
            var codeUpper = $"{(CharacterInfo.CodePoint >> 8) & 0xFF:X2}";
            var codeLower = $"{CharacterInfo.CodePoint & 0xFF:X2}";

            var characterSize = new Vector2(_mainFont.GetLineWidth(character), _mainFont.GetLineHeight());
            var codeUpperSize = new Vector2(_codeFont.GetLineWidth(codeUpper), _codeFont.GetLineHeight());
            var codeLowerSize = new Vector2(_codeFont.GetLineWidth(codeLower), _codeFont.GetLineHeight());

            var textWidth = characterSize.X + Math.Max(codeUpperSize.X, codeLowerSize.X) + 5;
            var textHeight = Math.Max(characterSize.Y, codeUpperSize.Y + codeLowerSize.Y);

            var characterX = (GlyphMaxSize.X - textWidth) / 2;
            var characterPosition = contentRect.Position + new Vector2(characterX, GlyphMaxSize.Y + (textHeight - characterSize.Y) / 2);
            var codeUpperPosition = contentRect.Position + new Vector2(characterX + characterSize.X + 5, GlyphMaxSize.Y);
            var codeLowerPosition = contentRect.Position + new Vector2(characterX + characterSize.X + 5, GlyphMaxSize.Y + codeUpperSize.Y);

            Hexa.NET.ImGui.ImGui.SetCursorScreenPos(characterPosition);
            Hexa.NET.ImGui.ImGui.Text(character);

            if (_codeFont.GetPointer().HasValue)
                Hexa.NET.ImGui.ImGui.PushFont(_codeFont.GetPointer()!.Value, _codeFont.Data.Size);

            Hexa.NET.ImGui.ImGui.SetCursorScreenPos(codeUpperPosition);
            Hexa.NET.ImGui.ImGui.Text(codeUpper);

            Hexa.NET.ImGui.ImGui.SetCursorScreenPos(codeLowerPosition);
            Hexa.NET.ImGui.ImGui.Text(codeLower);

            if (_codeFont.GetPointer().HasValue)
                Hexa.NET.ImGui.ImGui.PopFont();

            // Draw border
            Hexa.NET.ImGui.ImGui.GetWindowDrawList().AddRect(contentRect.Position, contentRect.Position + contentRect.Size, Style.GetColor(ImGuiCol.Border).ToUInt32());
        }

        protected override int GetContentWidth(int parentWidth, int parentHeight, float layoutCorrection = 1)
        {
            return (int)GlyphMaxSize.X;
        }

        protected override int GetContentHeight(int parentWidth, int parentHeight, float layoutCorrection = 1)
        {
            float textHeight = Math.Max(_mainFont.GetLineHeight(), _codeFont.GetLineHeight() + 1);
            return (int)(GlyphMaxSize.Y + 12 + textHeight);
        }

        private void OnSelectedChanged()
        {
            SelectedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
