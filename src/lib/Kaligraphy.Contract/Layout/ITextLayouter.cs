using Kaligraphy.Contract.DataClasses.Layout;
using Kaligraphy.Contract.DataClasses.Parsing;
using SixLabors.ImageSharp;

namespace Kaligraphy.Contract.Layout
{
    public interface ITextLayouter
    {
        IList<TextLayoutLineData> Create(IList<CharacterData> characters);

        TextLayoutData Create(IList<CharacterData> characters, Size boundingBox);

        TextLayoutData Create(IList<TextLayoutLineData> layoutLines, Size boundingBox);
    }
}
