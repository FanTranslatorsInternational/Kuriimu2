using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.PatternMatch.MatchParser.Support
{
    class PositionElement
    {
        private int _runValue;

        public PositionElement Parent { get; set; }

        public Match Match { get; set; }

        public int Price { get; set; }

        public int CurrentRunLength
        {
            get => IsMatchRun ? -_runValue : _runValue;
            set => _runValue = IsMatchRun ? -value : value;
        }

        public bool IsMatchRun
        {
            get => _runValue < 0;
            set
            {
                if (IsMatchRun != value)
                    _runValue = -_runValue;
            }
        }

        public PositionElement(int currentRunLength, bool isMatchRun)
        {
            _runValue = isMatchRun ? -currentRunLength : currentRunLength;
        }

        public PositionElement(int currentRunLength, bool isMatchRun, PositionElement parent, int price) :
            this(currentRunLength, isMatchRun)
        {
            Parent = parent;
            Price = price;
        }
    }
}
