using System;

namespace Kuriimu2.Cmd
{
    internal class ArgumentGetter(string[] args)
    {
        private int _index;

        public bool HasArguments(int count = 1) => _index + count <= args.Length;

        public string ReadArgument(int count = 1) => ReadArguments()[0];

        public string[] ReadArguments(int count = 1)
        {
            count = Math.Min(count, args.Length - _index);
            if (count <= 0)
                return [];

            _index += count;
            return args[(_index - count).._index];
        }

        public string PeekArgument(int count = 1) => PeekArguments()[0];

        public string[] PeekArguments(int count = 1)
        {
            count = Math.Min(count, args.Length - _index);
            if (count <= 0)
                return [];

            return args[_index..(_index + count)];
        }
    }
}
