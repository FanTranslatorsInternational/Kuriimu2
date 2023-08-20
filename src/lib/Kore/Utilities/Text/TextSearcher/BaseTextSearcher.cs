using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kore.Utilities.Text.TextSearcher
{
    abstract class BaseTextSearcher : ITextSearcher
    {
        private CancellationTokenSource _cancellationSource;

        /// <inheritdoc />
        public Task<int> SearchAsync(string input, Encoding encoding)
        {
            return SearchAsync(encoding.GetBytes(input));
        }

        /// <inheritdoc />
        public Task<int> SearchAsync(byte[] input)
        {
            return SearchInit(input.Length, index => input[index]);
        }

        /// <inheritdoc />
        public Task<int> SearchAsync(BinaryReader br)
        {
            return SearchInit((int)br.BaseStream.Length, index =>
            {
                br.BaseStream.Position = index;
                return br.ReadByte();
            });
        }

        /// <inheritdoc />
        public void Cancel()
        {
            _cancellationSource?.Cancel();
            _cancellationSource = null;
        }

        protected bool IsCancelled()
        {
            return _cancellationSource?.IsCancellationRequested ?? true;
        }

        /// <summary>
        /// SearchAsync a pattern in the data.
        /// </summary>
        /// <param name="length">The length of the data.</param>
        /// <param name="getByteFunc">The delegate to retrieve a byte from a source.</param>
        /// <returns>The offset of the first occurrence of the pattern.</returns>
        protected abstract int SearchInternal(int length, Func<int, byte> getByteFunc);

        private Task<int> SearchInit(int length, Func<int, byte> getByteFunc)
        {
            if (_cancellationSource != null)
                throw new InvalidOperationException("A search operation is already being processed.");

            _cancellationSource = new CancellationTokenSource();
            return Task.Run(() =>
            {
                var offset= SearchInternal(length, getByteFunc);
                _cancellationSource = null;

                return offset;
            }, _cancellationSource.Token);
        }
    }
}
