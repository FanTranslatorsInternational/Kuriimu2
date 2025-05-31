using Konnect.Contract.DataClasses.Plugin.File.Font;

namespace plugin_nintendo.Font
{
    class CodeRangeOptimalParser
    {
        public Match[] Parse(CharacterInfo[] characters)
        {
            var history = new MatchPrice[characters.Length];
            for (var i = 0; i < history.Length; i++)
                history[i] = new MatchPrice(null, int.MaxValue);
            history[0].Price = 0;

            ForwardPass(characters, history);
            return [.. BackwardPass(history).Reverse()];
        }

        private void ForwardPass(CharacterInfo[] characters, MatchPrice[] history)
        {
            IList<IList<Match>> matches = GetAllMatches(characters);

            for (var dataPosition = 0; dataPosition < characters.Length; dataPosition++)
            {
                // Calculate literal price at position
                MatchPrice element = history[dataPosition];
                int literalPrice = element.Price + 4;

                if (dataPosition + 1 < history.Length &&
                    literalPrice <= history[dataPosition + 1].Price)
                {
                    MatchPrice nextElement = history[dataPosition + 1];

                    nextElement.Parent = element;
                    nextElement.Price = literalPrice;
                    nextElement.Match = null;
                }

                // Then go through all longest matches at current position
                for (var methodIndex = 0; methodIndex < 2; methodIndex++)
                {
                    Match match = matches[methodIndex][dataPosition];

                    for (var j = 0; j < match.End - match.Start + 1; j++)
                    {
                        int matchPrice = GetPrice(characters, match, j);
                        matchPrice += element.Price;

                        if (dataPosition + j < history.Length &&
                            matchPrice < history[dataPosition + j].Price)
                        {
                            MatchPrice nextElement = history[dataPosition + j];

                            nextElement.Parent = element;
                            nextElement.Price = matchPrice;
                            nextElement.Match = match with { End = match.Start + j };
                        }
                    }
                }
            }
        }

        private IEnumerable<Match> BackwardPass(MatchPrice[] history)
        {
            MatchPrice? element = history.Last();
            //int position = history.Length - 1;
            while (element != null)
            {
                if (element.Match != null)
                {
                    //position -= element.Match.End - element.Match.Start + 1;
                    yield return element.Match;
                }
                else
                {
                    //position--;
                }

                element = element.Parent;
            }
        }

        private IList<IList<Match>> GetAllMatches(CharacterInfo[] input)
        {
            var result = new IList<Match>[2];

            for (var i = 0; i < 2; i++)
            {
                int method = i;
                result[i] = Enumerable.Range(0, input.Length)
                    .AsParallel()
                    .AsOrdered()
                    .WithDegreeOfParallelism(Environment.ProcessorCount)
                    .Select(x => GetMatch(input, x, method)).ToArray();
            }

            return result;
        }

        private Match GetMatch(CharacterInfo[] input, int position, int method)
        {
            switch (method)
            {
                case 0:
                    return GetDirectMatch(input, position);

                case 1:
                    return GetIndirectMatch(input, position);

                default:
                    throw new InvalidOperationException($"Invalid section method {method}.");
            }
        }

        private Match GetDirectMatch(CharacterInfo[] characters, int index)
        {
            var directRange = 1;
            while (index + directRange < characters.Length
                   && characters[index + directRange].CodePoint - directRange == characters[index].CodePoint)
                directRange++;

            return new Match(index, index + directRange - 1, 0);
        }

        private Match GetIndirectMatch(CharacterInfo[] characters, int index)
        {
            var codes = 1;
            while (index + codes < characters.Length
                   && characters[index + codes].CodePoint - characters[index].CodePoint - codes <= codes + 1)
                codes++;

            return new Match(index, index + codes - 1, 1);
        }

        private int GetPrice(CharacterInfo[] characters, Match match, int index)
        {
            switch (match.Method)
            {
                case 0:
                    return 0xE;

                case 1:
                    ushort codeBegin = characters[match.Start].CodePoint;
                    ushort codeEnd = characters[match.Start + index].CodePoint;
                    int codePrice = (codeEnd - codeBegin + 1) * 2;

                    return 0xC + codePrice;

                default:
                    throw new InvalidOperationException($"Invalid section method {match.Method}.");
            }
        }
    }

    class MatchPrice(Match? match, int price)
    {
        public MatchPrice? Parent { get; set; }

        public Match? Match { get; set; } = match;
        public int Price { get; set; } = price;
    }

    record Match(int Start, int End, int Method);
}
