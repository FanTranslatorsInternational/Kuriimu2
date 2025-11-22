using Kaligraphy.Contract.DataClasses.Layout;
using Kaligraphy.Contract.DataClasses.Rendering;
using Kaligraphy.Contract.Rendering;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.DataClasses.Rendering;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace Kaligraphy.Rendering;

public class TextRenderer<TContext, TOptions> : ITextRenderer
    where TContext : RenderContext, new()
    where TOptions : RenderOptions, new()
{
    private readonly IGlyphProvider _glyphProvider;

    protected TOptions Options { get; }

    public TextRenderer(TOptions options, IGlyphProvider glyphProvider)
    {
        _glyphProvider = glyphProvider;
        Options = options;
    }

    public void Render(Image<Rgba32> image, TextLayoutData layout)
    {
        var context = new TContext();

        RenderLines(image, layout.Lines, context);

        if (Options.DrawBoundingBoxes)
            image.Mutate(x => x.Draw(Color.Red, 1f, layout.BoundingBox));
    }

    private void RenderLines(Image<Rgba32> image, IReadOnlyList<TextLayoutLineData> lines, TContext context)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            bool isLineVisible = Options.VisibleLines <= 0 || lines.Count - i <= Options.VisibleLines;

            RenderLine(image, lines[i], context, isLineVisible);
        }
    }

    private void RenderLine(Image<Rgba32> image, TextLayoutLineData line, TContext context, bool isLineVisible)
    {
        if (Options.TextOutlineColor != Color.Transparent)
            foreach (TextLayoutCharacterData character in line.Characters)
                RenderCharacterOutline(image, character, context, isLineVisible);

        foreach (TextLayoutCharacterData character in line.Characters)
        {
            RenderCharacter(image, character, context, isLineVisible);

            if (Options.DrawBoundingBoxes)
                image.Mutate(x => x.Draw(Color.RebeccaPurple, 1f, character.BoundingBox));
        }

        if (Options.DrawBoundingBoxes)
            image.Mutate(x => x.Draw(Color.PaleVioletRed, 1f, line.BoundingBox));
    }

    private void RenderCharacterOutline(Image<Rgba32> image, TextLayoutCharacterData character, TContext context, bool isLineVisible)
    {
        if (Options.OutlineRadius <= 0 || character.GlyphBoundingBox.Width == 0 || character.GlyphBoundingBox.Height == 0)
            return;

        DrawCharacterOutline(image, character, context, isLineVisible);
    }

    protected virtual void RenderCharacter(Image<Rgba32> image, TextLayoutCharacterData character, TContext context, bool isLineVisible)
    {
        if (character.GlyphBoundingBox.Width == 0 || character.GlyphBoundingBox.Height == 0)
            return;

        DrawCharacter(image, character, context, isLineVisible);
    }

    protected virtual void DrawCharacter(Image<Rgba32> image, TextLayoutCharacterData character, TContext context, bool isLineVisible)
    {
        Color textColor = GetTextColor(context);

        switch (character.Character)
        {
            case FontCharacterData fontCharacter:
                CharacterInfo? glyph = GetGlyphProvider(context).GetOrDefault(fontCharacter.Character, textColor);
                if (glyph?.Glyph == null)
                    break;

                Image<Rgba32> resizedGlyph = glyph.Glyph.Clone(ctx => ctx.Resize((Size)character.GlyphBoundingBox.Size));
                image.Mutate(x => x.DrawImage(resizedGlyph, (Point)character.GlyphBoundingBox.Location, isLineVisible ? 1f : .25f));

                break;
        }
    }

    private void DrawCharacterOutline(Image<Rgba32> image, TextLayoutCharacterData character, TContext context, bool isLineVisible)
    {
        switch (character.Character)
        {
            case FontCharacterData fontCharacter:
                CharacterInfo? glyph = GetGlyphProvider(context).GetOrDefault(fontCharacter.Character);
                if (glyph?.Glyph == null)
                    break;

                Color centerColor = Options.TextOutlineColor;
                if (!isLineVisible)
                    centerColor = centerColor.WithAlpha(.25f);

                for (var y = 0; y < glyph.Glyph.Height; y++)
                {
                    for (var x = 0; x < glyph.Glyph.Width; x++)
                    {
                        PointF centerLocation = new(character.GlyphBoundingBox.X + x, character.GlyphBoundingBox.Y + y);
                        PointF[] ellipse = new EllipsePolygon(centerLocation, Options.OutlineRadius).Points.ToArray();

                        if (glyph.Glyph[x, y].A > 0)
                            image.Mutate(z => z.FillPolygon(centerColor, ellipse));
                    }
                }

                break;
        }
    }

    protected virtual Color GetTextColor(TContext context) => Options.TextColor;

    protected virtual IGlyphProvider GetGlyphProvider(TContext context) => _glyphProvider;
}