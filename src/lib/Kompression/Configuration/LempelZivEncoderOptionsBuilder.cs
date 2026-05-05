using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv.MatchFinder;
using Kompression.Contract.DataClasses.Encoder.LempelZiv.MatchParser;
using Kompression.Contract.Encoder.LempelZiv.MatchFinder;
using Kompression.Contract.Encoder.LempelZiv.MatchParser;
using Kompression.Contract.Encoder.LempelZiv.PriceCalculator;
using Kompression.Contract.Enums.Encoder.LempelZiv;
using Kompression.DataClasses.Configuration;
using Kompression.Encoder.LempelZiv.InputManipulation;
using Kompression.Encoder.LempelZiv.MatchFinder;

namespace Kompression.Configuration
{
    internal class LempelZivEncoderOptionsBuilder(LempelZivOptions lempelZivOptions) : ILempelZivEncoderAdditionalOptionsBuilder,
        ILempelZivEncoderLimitationsOptionsBuilder
    {
        private readonly LempelZivEncoderOptions _encoderOptions = new();

        public ILempelZivEncoderLimitationsOptionsBuilder FindWith(CreateMatchFinderDelegate finderDelegate)
        {
            _encoderOptions.MatchFinderDelegates.Clear();
            _encoderOptions.MatchLimitations.Clear();

            _encoderOptions.MatchFinderDelegates.Add(finderDelegate);

            return this;
        }

        public ILempelZivEncoderLimitationsOptionsBuilder FindPatternMatches()
        {
            _encoderOptions.MatchFinderDelegates.Clear();
            _encoderOptions.MatchLimitations.Clear();

            _encoderOptions.MatchFinderDelegates.Add(options => new HistoryMatchFinder(options));

            return this;
        }

        public ILempelZivEncoderLimitationsOptionsBuilder FindRunLength()
        {
            _encoderOptions.MatchFinderDelegates.Clear();
            _encoderOptions.MatchLimitations.Clear();

            _encoderOptions.MatchFinderDelegates.Add(options => new RleMatchFinder(options));

            return this;
        }

        public ILempelZivEncoderLimitationsOptionsBuilder FindConstantRunLength(int value)
        {
            _encoderOptions.MatchFinderDelegates.Clear();
            _encoderOptions.MatchLimitations.Clear();

            _encoderOptions.MatchFinderDelegates.Add(options => new StaticValueRleMatchFinder(value, options));

            return this;
        }

        public ILempelZivEncoderLimitationsOptionsBuilder AndFindWith(CreateMatchFinderDelegate finderDelegate)
        {
            _encoderOptions.MatchFinderDelegates.Add(finderDelegate);
            return this;
        }

        public ILempelZivEncoderLimitationsOptionsBuilder AndFindPatternMatches()
        {
            _encoderOptions.MatchFinderDelegates.Add(options => new HistoryMatchFinder(options));
            return this;
        }

        public ILempelZivEncoderLimitationsOptionsBuilder AndFindRunLength()
        {
            _encoderOptions.MatchFinderDelegates.Add(options => new RleMatchFinder(options));
            return this;
        }

        public ILempelZivEncoderLimitationsOptionsBuilder AndFindConstantRunLength(int value)
        {
            _encoderOptions.MatchFinderDelegates.Add(options => new StaticValueRleMatchFinder(value, options));
            return this;
        }

        public ILempelZivEncoderAdditionalOptionsBuilder WithinLimitations(int minLength, int maxLength)
        {
            _encoderOptions.MatchLimitations.Add(new LempelZivMatchLimitations
            {
                MinLength = minLength,
                MaxLength = maxLength
            });
            return this;
        }

        public ILempelZivEncoderAdditionalOptionsBuilder WithinLimitations(int minLength, int maxLength, int minDisplacement, int maxDisplacement)
        {
            _encoderOptions.MatchLimitations.Add(new LempelZivMatchLimitations
            {
                MinLength = minLength,
                MaxLength = maxLength,
                MinDisplacement = minDisplacement,
                MaxDisplacement = maxDisplacement
            });
            return this;
        }

        public ILempelZivEncoderOptionsBuilder CalculatePricesWith(CreatePriceCalculatorDelegate calculatorDelegate)
        {
            _encoderOptions.CalculatePriceDelegate = calculatorDelegate;
            return this;
        }

        public ILempelZivEncoderOptionsBuilder SkipUnitsAfterMatch(int skipUnits)
        {
            _encoderOptions.SkipUnits = skipUnits;
            return this;
        }

        public ILempelZivEncoderOptionsBuilder HasUnitSize(UnitSize unitSize)
        {
            _encoderOptions.UnitSize = unitSize;
            return this;
        }

        public ILempelZivEncoderOptionsBuilder AdjustInput(AdjustInputDelegate inputDelegate)
        {
            _encoderOptions.AdjustInputDelegate = inputDelegate;
            return this;
        }

        public ILempelZivMatchParser Build()
        {
            var parserOptions = new LempelZivMatchParserOptions
            {
                PriceCalculator = CreatePriceCalculator(),
                MatchFinders = CreateMatchFinders(),
                InputManipulation = CreateInputManipulator(),
                UnitSize = _encoderOptions.UnitSize,
                TaskCount = lempelZivOptions.TaskCount
            };
            return lempelZivOptions.CreateMatchParserDelegate(parserOptions);
        }

        private ILempelZivPriceCalculator CreatePriceCalculator()
        {
            ArgumentNullException.ThrowIfNull(_encoderOptions.CalculatePriceDelegate);

            return _encoderOptions.CalculatePriceDelegate();
        }

        private ILempelZivMatchFinder[] CreateMatchFinders()
        {
            if (_encoderOptions.MatchFinderDelegates.Count <= 0)
                throw new InvalidOperationException("No match finders configured.");
            if (_encoderOptions.MatchFinderDelegates.Count != _encoderOptions.MatchLimitations.Count)
                throw new InvalidOperationException("At least one match finder does not have limitations configured.");

            var matchFinders = new ILempelZivMatchFinder[_encoderOptions.MatchFinderDelegates.Count];
            for (var i = 0; i < matchFinders.Length; i++)
            {
                var options = new LempelZivMatchFinderOptions
                {
                    Limitations = _encoderOptions.MatchLimitations[i],
                    UnitSize = _encoderOptions.UnitSize
                };
                matchFinders[i] = _encoderOptions.MatchFinderDelegates[i](options);
            }

            return matchFinders;
        }

        private InputManipulator CreateInputManipulator()
        {
            var options = new LempelZivInputAdjustmentOptions();

            if (_encoderOptions.AdjustInputDelegate == null)
                return new InputManipulator(options);

            var builder = new LempelZivInputAdjustmentOptionsBuilder(options);
            _encoderOptions.AdjustInputDelegate(builder);

            return new InputManipulator(options);
        }
    }
}
