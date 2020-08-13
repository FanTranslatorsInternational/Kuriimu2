using System;
using System.Collections.Generic;
using System.Linq;

namespace Kuriimu2.Cmd.Parsers
{
    class ArgumentGetter : IArgumentGetter
    {
        private string[] _bufferedArguments;
        private int _argumentCount;

        public ArgumentGetter(params string[] bufferedArguments)
        {
            _bufferedArguments = bufferedArguments;
        }

        public string GetNextArgument()
        {
            return GetNextArguments(1)[0];
        }

        public IList<string> GetNextArguments(int count)
        {
            var result = new List<string>();

            // Get buffered arguments
            if (_argumentCount < _bufferedArguments.Length)
            {
                var length = Math.Min(count, _bufferedArguments.Length - _argumentCount);
                for (var i = _argumentCount; i < _argumentCount + length; i++)
                    result.Add(_bufferedArguments[i]);

                _argumentCount += length;
                count -= length;
            }

            if (count <= 0)
                return result.ToArray();

            // Get new arguments
            var arguments = ReadArguments(count);
            for (var i = 0; i < count; i++)
                result.Add(arguments[i]);

            if (arguments.Count <= count)
                return result;

            // Set overflowing arguments in buffer
            _bufferedArguments = arguments.Skip(count).Take(arguments.Count - count).ToArray();
            _argumentCount = 0;

            return result;
        }

        private IList<string> ReadArguments(int count)
        {
            var result = new List<string>();

            while (result.Count < count)
            {
                Console.Write("> ");
                var input = Console.ReadLine();

                result.AddRange(SplitInput(input));
            }

            return result;
        }

        private IList<string> SplitInput(string input)
        {
            var index = 0;
            var result = new List<string>();

            while (index < input.Length)
            {
                var spaceIndex = input.IndexOf(' ', index);
                var quoteIndex = input.IndexOf('"', index);

                int endIndex;
                if ((spaceIndex >= 0 && spaceIndex < quoteIndex) || quoteIndex != index)
                {
                    // Split at space
                    endIndex = spaceIndex < 0 ? input.Length : spaceIndex;

                    result.Add(input.Substring(index, endIndex - index));
                    index = endIndex + 1;

                    continue;
                }

                // Split at quote
                var nextQuoteIndex = input.IndexOf('"', index + 1);
                endIndex = nextQuoteIndex < 0 ? input.Length : nextQuoteIndex;

                result.Add(input.Substring(index + 1, endIndex - (index + 1)));
                index = endIndex + 2;   // Skip quote and following "space", or be 2 indices out of input
            }

            return result;
        }
    }
}
