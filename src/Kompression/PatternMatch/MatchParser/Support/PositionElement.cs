using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.PatternMatch.MatchParser.Support
{
    class PositionElement
    {
        public PositionElement Parent { get; set; }

        public int CurrentRunLength { get; set; }
        public bool IsMatchRun { get; set; }

        public Match? Match { get; set; }

        public int Price { get; set; }

        public PositionElement(int currentRunLength, bool isMatchRun)
        {
            CurrentRunLength = currentRunLength;
            IsMatchRun = isMatchRun;
        }

        public PositionElement(int currentRunLength, bool isMatchRun, PositionElement parent, int price) :
            this(currentRunLength, isMatchRun)
        {
            Parent = parent;
            Price = price;
        }
    }
}
