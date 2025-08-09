using Kaligraphy.Contract.DataClasses.Layout;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.DataClasses.Rendering;
using Kaligraphy.Contract.Layout;
using Kaligraphy.Contract.Rendering;
using Kaligraphy.DataClasses.Layout;
using Kaligraphy.DataClasses.Parsing;
using Kaligraphy.Enums.Layout;
using SixLabors.ImageSharp;

namespace Kaligraphy.Layout
{
    public class TextLayouter : ITextLayouter
    {
        protected IGlyphProvider GlyphProvider { get; }

        protected LayoutOptions Options { get; }

        public TextLayouter(LayoutOptions options, IGlyphProvider glyphProvider)
        {
            GlyphProvider = glyphProvider;
            Options = options;
        }

        public IList<TextLayoutLineData> Create(IList<CharacterData> characters)
        {
            return characters.Count <= 0 ? [] : CreateLines(characters);
        }

        public TextLayoutData Create(IList<CharacterData> characters, Size boundingBox)
        {
            if (characters.Count <= 0)
                return new TextLayoutData(Array.Empty<TextLayoutLineData>(), new Rectangle(Options.InitPoint, Size.Empty));

            IList<TextLayoutLineData> layoutLines = CreateLines(characters);

            return Create(layoutLines, boundingBox);
        }

        public TextLayoutData Create(IList<TextLayoutLineData> layoutLines, Size boundingBox)
        {
            int lineHeight = GetLineHeight();

            for (var i = 0; i < layoutLines.Count; i++)
            {
                TextLayoutLineData layoutLine = layoutLines[i];

                Point linePoint = GetLinePosition(layoutLine, boundingBox, layoutLines.Sum(l => l.BoundingBox.Height));
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

            var textPoint = new Point(layoutLines.Min(x => x.BoundingBox.X), layoutLines[0].BoundingBox.Y);
            var textSize = new Size(layoutLines.Max(x => x.BoundingBox.Width), layoutLines.Sum(l => l.BoundingBox.Height));

            return new TextLayoutData(layoutLines.AsReadOnly(), new Rectangle(textPoint, textSize));
        }

        protected virtual Point GetLinePosition(TextLayoutLineData currentLine, Size boundingBox, int linesHeight)
        {
            int x = GetLinePositionX(currentLine, boundingBox.Width);
            int y = GetLinePositionY(currentLine, boundingBox.Height, linesHeight);

            return new Point(x, y);
        }

        protected virtual int GetLinePositionX(TextLayoutLineData currentLine, int boundingWidth)
        {
            switch (Options.HorizontalAlignment)
            {
                case HorizontalTextAlignment.Left:
                    return Options.InitPoint.X + currentLine.BoundingBox.X;

                case HorizontalTextAlignment.Center:
                    return Options.InitPoint.X + currentLine.BoundingBox.X + (boundingWidth - Options.InitPoint.X - currentLine.BoundingBox.Width) / 2;

                case HorizontalTextAlignment.Right:
                    return boundingWidth - Options.InitPoint.Y - currentLine.BoundingBox.Width;

                default:
                    throw new InvalidOperationException($"Unsupported text alignment {Options.HorizontalAlignment}.");
            }
        }

        protected virtual int GetLinePositionY(TextLayoutLineData currentLine, int boundingHeight, int linesHeight)
        {
            switch (Options.VerticalAlignment)
            {
                case VerticalTextAlignment.Top:
                    return Options.InitPoint.Y + currentLine.BoundingBox.Y;

                case VerticalTextAlignment.Center:
                    return Options.InitPoint.Y + currentLine.BoundingBox.Y + (boundingHeight - Options.InitPoint.Y - linesHeight) / 2;

                case VerticalTextAlignment.Bottom:
                    return boundingHeight - linesHeight - Options.InitPoint.Y + currentLine.BoundingBox.Y;

                default:
                    throw new InvalidOperationException($"Unsupported text alignment {Options.VerticalAlignment}.");
            }
        }

        private IList<TextLayoutLineData> CreateLines(IList<CharacterData> parsedCharacters)
        {
            var context = new LayoutContext();

            foreach (CharacterData character in parsedCharacters)
                CreateCharacter(character, context);

            if (context.Characters.Count > 0)
            {
                context.Lines.Add(new TextLayoutLineData
                {
                    Characters = context.Characters,
                    BoundingBox = new Rectangle(new Point(0, context.Y), new Size(context.VisibleX, GetLineHeight()))
                });
            }

            return context.Lines;
        }

        protected virtual void CreateCharacter(CharacterData character, LayoutContext context)
        {
            var characterLocation = new Point(context.VisibleX, context.Y);

            switch (character)
            {
                case LineBreakCharacterData:
                    // Add line break character
                    context.Characters.Add(new TextLayoutCharacterData
                    {
                        Character = character,
                        BoundingBox = new Rectangle(characterLocation, Size.Empty),
                        GlyphBoundingBox = new Rectangle(characterLocation, Size.Empty)
                    });

                    // Create line from all current characters
                    context.Lines.Add(new TextLayoutLineData
                    {
                        Characters = context.Characters,
                        BoundingBox = new Rectangle(new Point(0, context.Y), new Size(context.VisibleX, GetLineHeight()))
                    });

                    context.X = 0;
                    context.Y += GetLineHeight();
                    context.VisibleX = 0;

                    context.Characters = new List<TextLayoutCharacterData>();
                    break;

                default:
                    Rectangle characterBox = GetCharacterBoundingBox(character, characterLocation, out bool isVisible);

                    if (isVisible && Options.LineWidth > 0 && context.X + characterBox.Width > Options.LineWidth)
                    {
                        context.Lines.Add(new TextLayoutLineData
                        {
                            Characters = context.Characters,
                            BoundingBox = new Rectangle(new Point(0, context.Y), new Size(context.VisibleX, GetLineHeight()))
                        });

                        context.X = 0;
                        context.Y += GetLineHeight();
                        context.VisibleX = 0;

                        context.Characters = new List<TextLayoutCharacterData>();

                        characterLocation = new Point(context.VisibleX, context.Y);
                        characterBox = GetCharacterBoundingBox(character, characterLocation, out isVisible);
                    }

                    Rectangle glyphBox = GetGlyphBoundingBox(character, characterLocation);

                    context.X += characterBox.Width;
                    if (isVisible)
                        context.VisibleX += characterBox.Width;

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

        protected virtual Rectangle GetCharacterBoundingBox(CharacterData character, Point characterLocation, out bool isVisible)
        {
            isVisible = true;

            switch (character)
            {
                case TextCharacterData fontCharacter:
                    CharacterInfo? glyph = GlyphProvider.GetOrDefault(fontCharacter.Character);
                    if (glyph == null)
                        break;

                    var glyphWidth = (int)(glyph.BoundingBox.Width * Options.TextScale);
                    var glyphSize = new Size(glyphWidth + Options.TextSpacing, GetLineHeight());

                    return new Rectangle(characterLocation, glyphSize);
            }

            return new Rectangle(characterLocation, Size.Empty);
        }

        protected virtual Rectangle GetGlyphBoundingBox(CharacterData character, Point characterLocation)
        {
            switch (character)
            {
                case TextCharacterData fontCharacter:
                    CharacterInfo? glyph = GlyphProvider.GetOrDefault(fontCharacter.Character);
                    if (glyph?.Glyph == null)
                        break;

                    var glyphWidth = (int)(glyph.Glyph.Width * Options.TextScale);
                    var glyphHeight = (int)(glyph.Glyph.Height * Options.TextScale);

                    int glyphX = characterLocation.X + glyph.GlyphPosition.X;
                    int glyphY = characterLocation.Y + glyph.GlyphPosition.Y;

                    return new Rectangle(glyphX, glyphY, glyphWidth, glyphHeight);
            }

            return new Rectangle(characterLocation, Size.Empty);
        }

        private int GetLineHeight()
        {
            if (Options.LineHeight > 0)
                return Options.LineHeight;

            return GlyphProvider.GetMaxHeight();
        }
    }
}
