using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Extensions;
using ImGui.Forms.Resources;
using ImGuiNET;
using Kanvas.Swizzle;
using Kuriimu2.ImGui.Resources;
using SixLabors.ImageSharp;
using EventArgs = System.EventArgs;
using Rectangle = Veldrid.Rectangle;

namespace Kuriimu2.ImGui.Components
{
    class ZoomableSwizzleEditorPictureBox : ZoomablePictureBox
    {
        private readonly List<Vector2> _coordinates = new();
        private MasterSwizzle? _swizzle;

        private bool _isMouseDown;
        private int _activeCoordIndex = -1;
        private bool _activeCoordChanged;
        private bool _isNewCoord;

        public bool RenderSwizzle { get; set; }

        public IReadOnlyList<Vector2> Coordinates => _coordinates;

        public event EventHandler CoordinatesChanged;

        protected override void DrawInternal(Rectangle contentRect)
        {
            base.DrawInternal(contentRect);

            if (!HasValidImage())
            {
                DrawControlLegend(contentRect);
                return;
            }

            // Render image border
            Rectangle imageRect = GetTransformedImageRect(contentRect);
            ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + imageRect.Size, Style.GetColor(ImGuiCol.Border).ToUInt32());

            if (RenderSwizzle)
            {
                // Render coordinates
                foreach (Vector2 coordinate in _coordinates)
                    DrawPixelBorder(contentRect, coordinate, Color.Red, 3f);

                if (_coordinates.Count > 0 && _swizzle is not null)
                {
                    // Render swizzle macro block border
                    var endPos = new Vector2(_swizzle.MacroTileWidth, _swizzle.MacroTileHeight);
                    Rectangle macroBlockRect = Transform(contentRect, new Rectangle(0, 0, (int)endPos.X, (int)endPos.Y));

                    ImGuiNET.ImGui.GetWindowDrawList().AddRect(imageRect.Position, imageRect.Position + macroBlockRect.Size, Style.GetColor(ImGuiCol.Border).ToUInt32());

                    // Render swizzle path
                    DrawSwizzlePath(contentRect, macroBlockRect, imageRect, endPos);
                }
            }

            Vector2 mousePos = ImGuiNET.ImGui.GetMousePos();
            mousePos = UnTransform(contentRect, mousePos);

            Vector2 imagePos = UnTransform(contentRect, imageRect.Position);

            float x = mousePos.X - imagePos.X;
            float y = mousePos.Y - imagePos.Y;

            var pixelPos = new Vector2((int)x, (int)y);

            if (ImGuiNET.ImGui.IsItemHovered())
            {
                // Render border for mouse hovered pixel
                if (IsInImage(x, y))
                    DrawPixelBorder(contentRect, pixelPos, Style.GetColor(ImGuiCol.Border), 1f);

                // Handle active coordinate
                if (ImGuiNET.ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    if (!_isMouseDown && _activeCoordIndex < 0)
                    {
                        if (IsInImage(x, y) && (pixelPos.X > 0 || pixelPos.Y > 0))
                        {
                            int lastCoordinateIndex = _coordinates.LastIndexOf(pixelPos);
                            if (lastCoordinateIndex >= 0)
                                _activeCoordIndex = lastCoordinateIndex;
                            else
                            {
                                AddCoordinate(pixelPos);

                                _isNewCoord = true;
                                _activeCoordIndex = _coordinates.Count - 1;

                                CreateSwizzle();
                            }
                        }
                    }
                    else
                    {
                        if (_activeCoordIndex >= 0 && IsInImage(x, y))
                        {
                            bool hasCoordChanged = _coordinates[_activeCoordIndex] != pixelPos;
                            if (hasCoordChanged)
                                _activeCoordChanged = true;

                            _coordinates[_activeCoordIndex] = pixelPos;

                            if (hasCoordChanged)
                            {
                                OnCoordinatesChanged();
                                CreateSwizzle();
                            }
                        }
                    }

                    _isMouseDown = true;
                }
                else if (ImGuiNET.ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    _isMouseDown = false;

                    if (_activeCoordIndex >= 0)
                    {
                        if (!_activeCoordChanged && !_isNewCoord)
                        {
                            RemoveCoordinate(_activeCoordIndex);
                            CreateSwizzle();
                        }

                        _activeCoordIndex = -1;
                        _activeCoordChanged = false;
                        _isNewCoord = false;
                    }
                }
            }

            DrawControlLegend(contentRect);
        }

        private void DrawControlLegend(Rectangle contentRect)
        {
            ImGuiNET.ImGui.GetWindowDrawList().AddText(contentRect.Position, ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), LocalizationResources.MenuToolsRawImageViewerSwizzleEditorAddControl);
            ImGuiNET.ImGui.GetWindowDrawList().AddText(contentRect.Position + new Vector2(0, TextMeasurer.GetCurrentLineHeight()), ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), LocalizationResources.MenuToolsRawImageViewerSwizzleEditorRemoveControl);
            ImGuiNET.ImGui.GetWindowDrawList().AddText(contentRect.Position + new Vector2(0, TextMeasurer.GetCurrentLineHeight() * 2), ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), LocalizationResources.MenuToolsRawImageViewerSwizzleEditorMoveControl);
            ImGuiNET.ImGui.GetWindowDrawList().AddText(contentRect.Position + new Vector2(0, TextMeasurer.GetCurrentLineHeight() * 3), ImGuiNET.ImGui.GetColorU32(ImGuiCol.Text), LocalizationResources.MenuToolsRawImageViewerSwizzleEditorCopyControl);
        }

        private void DrawPixelBorder(Rectangle contentRect, Vector2 pixelPos, Color color, float thickness)
        {
            Vector2 coordinateStartPos = Transform(contentRect, pixelPos - new Vector2(Image!.Width / 2f, Image.Height / 2f));
            Vector2 coordinateEndPos = Transform(contentRect, pixelPos - new Vector2(Image.Width / 2f, Image.Height / 2f) + Vector2.One);

            ImGuiNET.ImGui.GetWindowDrawList().AddRect(coordinateStartPos, coordinateEndPos, color.ToUInt32(), 0f, ImDrawFlags.None, thickness);
        }

        private void DrawSwizzlePath(Rectangle contentRect, Rectangle macroBlockRect, Rectangle imageRect, Vector2 endPos)
        {
            var points = new List<Vector2>();

            var startCoordinate = new Vector2(.5f, .5f);
            points.Add(Transform(contentRect, startCoordinate) - macroBlockRect.Position + imageRect.Position);

            for (var i = 1; i < Image!.Width * Image!.Height; i++)
            {
                Vector2 swizzledCoordinate = _swizzle.Get(i);
                Vector2 centeredSwizzledCoordinate = swizzledCoordinate + new Vector2(.5f, .5f);

                points.Add(Transform(contentRect, centeredSwizzledCoordinate) - macroBlockRect.Position + imageRect.Position);

                if (swizzledCoordinate == endPos - Vector2.One)
                    break;
            }

            Vector2[] pointsArray = points.ToArray();
            ImGuiNET.ImGui.GetWindowDrawList().AddPolyline(ref pointsArray[0], points.Count, Color.Red.ToUInt32(), ImDrawFlags.None, 1f);
        }

        private bool IsInImage(float x, float y)
        {
            return x >= 0 && y >= 0 && x < Image!.Width && y < Image.Height;
        }

        private void AddCoordinate(Vector2 pixelPos)
        {
            _coordinates.Add(pixelPos);

            OnCoordinatesChanged();
        }

        private void RemoveCoordinate(int index)
        {
            _coordinates.RemoveAt(index);

            OnCoordinatesChanged();
        }

        private void CreateSwizzle()
        {
            (int, int)[] coords = _coordinates.Select(x => ((int)x.X, (int)x.Y)).ToArray();
            _swizzle = new MasterSwizzle(Image!.Width, Point.Empty, coords);
        }

        private void OnCoordinatesChanged()
        {
            CoordinatesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
