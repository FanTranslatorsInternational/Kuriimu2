using Kaligraphy.Contract.DataClasses.Layout;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.DataClasses.Rendering;
using Kaligraphy.Contract.Layout;
using Kaligraphy.Contract.Rendering;
using Kaligraphy.DataClasses.Layout;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Enums.Layout;
using SixLabors.ImageSharp;

namespace Kaligraphy.Layout;

public class TextLayouter<TContext, TOptions> : ITextLayouter
    where TContext : LayoutContext, new()
    where TOptions : LayoutOptions, new()
{
    private readonly IGlyphProvider _glyphProvider;

    protected TOptions Options { get; }

    public TextLayouter(TOptions options, IGlyphProvider glyphProvider)
    {
        _glyphProvider = glyphProvider;
        Options = options;
    }

    public IList<TextLayoutLineData> Create(IList<CharacterData> characters)
    {
        return characters.Count <= 0 ? [] : CreateLines(characters);
    }

    public TextLayoutData Create(IList<CharacterData> characters, Point initPoint, Size boundingBox)
    {
        if (characters.Count <= 0)
            return new TextLayoutData([], new Rectangle(initPoint, Size.Empty));

        IList<TextLayoutLineData> layoutLines = CreateLines(characters);

        return Create(layoutLines, initPoint, boundingBox);
    }

    public TextLayoutData Create(IList<TextLayoutLineData> layoutLines, Point initPoint, Size boundingBox)
    {
        int lineHeight = GetLineHeight();

        for (var i = 0; i < layoutLines.Count; i++)
        {
            TextLayoutLineData layoutLine = layoutLines[i];

            PointF linePoint = GetLinePosition(layoutLine, initPoint, boundingBox, layoutLines.Sum(l => l.BoundingBox.Height));
            linePoint = linePoint with
            {
                Y = linePoint.Y - i * lineHeight
            };

            var layoutCharacters = new List<TextLayoutCharacterData>();
            foreach (TextLayoutCharacterData lineCharacter in layoutLine.Characters)
            {
                lineCharacter.BoundingBox = lineCharacter.BoundingBox with
                {
                    X = lineCharacter.BoundingBox.X + linePoint.X,
                    Y = lineCharacter.BoundingBox.Y + linePoint.Y
                };
                lineCharacter.GlyphBoundingBox = lineCharacter.GlyphBoundingBox with
                {
                    X = lineCharacter.GlyphBoundingBox.X + linePoint.X,
                    Y = lineCharacter.GlyphBoundingBox.Y + linePoint.Y
                };

                layoutCharacters.Add(lineCharacter);
            }

            layoutLine.Characters = layoutCharacters;
            layoutLine.BoundingBox = layoutLine.BoundingBox with
            {
                X = layoutLine.BoundingBox.X + linePoint.X,
                Y = layoutLine.BoundingBox.Y + linePoint.Y
            };
        }

        var textPoint = new PointF(layoutLines.Min(x => x.BoundingBox.X), layoutLines[0].BoundingBox.Y);
        var textSize = new SizeF(layoutLines.Max(x => x.BoundingBox.Width), layoutLines.Sum(l => l.BoundingBox.Height));

        return new TextLayoutData(layoutLines.AsReadOnly(), new RectangleF(textPoint, textSize));
    }

    protected virtual PointF GetLinePosition(TextLayoutLineData currentLine, Point initPoint, Size boundingBox, float linesHeight)
    {
        float x = GetLinePositionX(currentLine, initPoint, boundingBox.Width);
        float y = GetLinePositionY(currentLine, initPoint, boundingBox.Height, linesHeight);

        return new PointF(x, y);
    }

    protected virtual float GetLinePositionX(TextLayoutLineData currentLine, Point initPoint, int boundingWidth)
    {
        switch (Options.HorizontalAlignment)
        {
            case HorizontalTextAlignment.Left:
                return initPoint.X + currentLine.BoundingBox.X;

            case HorizontalTextAlignment.Center:
                return initPoint.X + currentLine.BoundingBox.X + (boundingWidth - initPoint.X - currentLine.BoundingBox.Width) / 2;

            case HorizontalTextAlignment.Right:
                return boundingWidth - initPoint.Y - currentLine.BoundingBox.Width;

            default:
                throw new InvalidOperationException($"Unsupported text alignment {Options.HorizontalAlignment}.");
        }
    }

    protected virtual float GetLinePositionY(TextLayoutLineData currentLine, Point initPoint, int boundingHeight, float linesHeight)
    {
        switch (Options.VerticalAlignment)
        {
            case VerticalTextAlignment.Top:
                return initPoint.Y + currentLine.BoundingBox.Y;

            case VerticalTextAlignment.Center:
                return initPoint.Y + currentLine.BoundingBox.Y + (boundingHeight - initPoint.Y - linesHeight) / 2;

            case VerticalTextAlignment.Bottom:
                return boundingHeight - linesHeight - initPoint.Y + currentLine.BoundingBox.Y;

            default:
                throw new InvalidOperationException($"Unsupported text alignment {Options.VerticalAlignment}.");
        }
    }

    private IList<TextLayoutLineData> CreateLines(IList<CharacterData> parsedCharacters)
    {
        var context = new TContext();

        foreach (CharacterData character in parsedCharacters)
            CreateCharacter(character, context);

        if (context.Characters.Count > 0)
        {
            context.Lines.Add(new TextLayoutLineData
            {
                Characters = context.Characters,
                BoundingBox = new RectangleF(new PointF(0, context.Y), new SizeF(context.VisibleX, GetLineHeight()))
            });
        }

        return context.Lines;
    }

    protected virtual void CreateCharacter(CharacterData character, TContext context)
    {
        var characterLocation = new PointF(context.VisibleX, context.Y);

        switch (character)
        {
            case LineBreakCharacterData:
                // Add line break character
                context.Characters.Add(new TextLayoutCharacterData
                {
                    Character = character,
                    BoundingBox = new RectangleF(characterLocation, SizeF.Empty),
                    GlyphBoundingBox = new RectangleF(characterLocation, SizeF.Empty)
                });

                // Create line from all current characters
                context.Lines.Add(new TextLayoutLineData
                {
                    Characters = context.Characters,
                    BoundingBox = new RectangleF(new PointF(0, context.Y), new SizeF(context.VisibleX, GetLineHeight()))
                });

                context.X = 0;
                context.Y += GetLineHeight();
                context.VisibleX = 0;

                context.Characters = new List<TextLayoutCharacterData>();
                break;

            default:
                RectangleF characterBox = GetCharacterBoundingBox(character, context, characterLocation, out bool isVisible, out bool isPersistent);

                if (isPersistent && Options.LineWidth > 0 && context.X + characterBox.Width > Options.LineWidth)
                {
                    context.Lines.Add(new TextLayoutLineData
                    {
                        Characters = context.Characters,
                        BoundingBox = new RectangleF(new PointF(0, context.Y), new SizeF(context.VisibleX, GetLineHeight()))
                    });

                    context.X = 0;
                    context.Y += GetLineHeight();
                    context.VisibleX = 0;

                    context.Characters = new List<TextLayoutCharacterData>();

                    characterLocation = new PointF(context.VisibleX, context.Y);
                    characterBox = GetCharacterBoundingBox(character, context, characterLocation, out isVisible, out isPersistent);
                }

                RectangleF glyphBox = GetGlyphBoundingBox(character, context, characterLocation);

                if (isPersistent)
                    context.X += characterBox.Width + Options.TextSpacing;
                if (isVisible)
                    context.VisibleX += characterBox.Width + Options.TextSpacing;

                else
                {
                    characterBox = characterBox with
                    {
                        Width = 0,
                        Height = 0
                    };

                    glyphBox = glyphBox with
                    {
                        Width = 0,
                        Height = 0
                    };
                }

                context.Characters.Add(new TextLayoutCharacterData
                {
                    Character = character,
                    BoundingBox = characterBox,
                    GlyphBoundingBox = glyphBox
                });

                break;
        }
    }

    protected virtual RectangleF GetCharacterBoundingBox(CharacterData character, TContext context, PointF characterLocation,
        out bool isVisible, out bool isPersistent)
    {
        isVisible = character.IsVisible;
        isPersistent = character.IsPersistent;

        switch (character)
        {
            case FontCharacterData fontCharacter:
                CharacterInfo? glyph = GetGlyphProvider(context).GetOrDefault(fontCharacter.Character);
                if (glyph == null)
                    break;

                var glyphWidth = (int)(glyph.BoundingBox.Width * Options.TextScale);
                var glyphSize = new SizeF(glyphWidth, GetLineHeight());

                return new RectangleF(characterLocation, glyphSize);
        }

        return new RectangleF(characterLocation, SizeF.Empty);
    }

    protected virtual RectangleF GetGlyphBoundingBox(CharacterData character, TContext context, PointF characterLocation)
    {
        switch (character)
        {
            case FontCharacterData fontCharacter:
                CharacterInfo? glyph = GetGlyphProvider(context).GetOrDefault(fontCharacter.Character);
                if (glyph?.Glyph == null)
                    break;

                var glyphWidth = (int)(glyph.Glyph.Width * Options.TextScale);
                var glyphHeight = (int)(glyph.Glyph.Height * Options.TextScale);

                float glyphX = characterLocation.X + glyph.GlyphPosition.X;
                float glyphY = characterLocation.Y + glyph.GlyphPosition.Y;

                return new RectangleF(glyphX, glyphY, glyphWidth, glyphHeight);
        }

        return new RectangleF(characterLocation, SizeF.Empty);
    }

    protected virtual IGlyphProvider GetGlyphProvider(TContext context) => _glyphProvider;

    protected int GetLineHeight()
    {
        if (Options.LineHeight > 0)
            return Options.LineHeight;

        return GetFontHeight();
    }

    protected virtual int GetFontHeight()
    {
        return _glyphProvider.GetMaxHeight();
    }
}