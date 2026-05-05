using Kompression.Contract.DataClasses.Encoder.LempelZiv;

namespace Kompression.DataClasses.Encoder.LemeplZiv.MatchParser
{
    internal class MatchParserPositionData(int currentRunLength, bool isMatchRun, MatchParserPositionData? parent, int price)
    {
        private int _runValue = isMatchRun ? -currentRunLength : currentRunLength;

        public MatchParserPositionData? Parent { get; set; } = parent;

        public LempelZivMatch? Match { get; set; }

        public int Price { get; set; } = price;

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

        public MatchParserPositionData(int currentRunLength, bool isMatchRun) :
            this(currentRunLength, isMatchRun, null, 0)
        {
        }
    }
}
