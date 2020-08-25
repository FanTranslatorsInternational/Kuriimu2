using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kontract;
using Kore.Utilities.Models;
using Kore.Utilities.Text.TextSearcher;

namespace Kore.Utilities.Text
{
    public class FoundMatchEventArgs : EventArgs
    {
        public SequenceSearchResult Result { get; }

        public FoundMatchEventArgs(SequenceSearchResult result)
        {
            ContractAssertions.IsNotNull(result, nameof(result));
            Result = result;
        }
    }

    /// <summary>
    /// The main implementation for text sequence searching.
    /// </summary>
    public class TextSequenceSearcher
    {
        private CancellationTokenSource _cancellationSource;
        private ITextSearcher _textSearcher;

        /// <summary>
        /// Gets invoked when a match was found.
        /// </summary>
        public event EventHandler<FoundMatchEventArgs> FoundMatch;

        /// <summary>
        /// The directory to search in.
        /// </summary>
        public string SearchDirectory { get; }

        /// <summary>
        /// The max size of files to search through.
        /// </summary>
        public int FileSizeLimitInBytes { get; }

        /// <summary>
        /// Will sub directories be searched too.
        /// </summary>
        public bool IsSearchSubDirectories { get; set; }

        /// <summary>
        /// The encoding used to search the pattern.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.ASCII;

        /// <summary>
        /// Creates a new instance of <see cref="TextSequenceSearcher"/>.
        /// </summary>
        /// <param name="searchDirectory">The directory to search in.</param>
        /// <param name="fileSizeLimitInBytes">The max size of files to search through.</param>
        public TextSequenceSearcher(string searchDirectory, int fileSizeLimitInBytes)
        {
            if (string.IsNullOrEmpty(searchDirectory))
                throw new ArgumentException(nameof(searchDirectory));
            if (!Directory.Exists(searchDirectory))
                throw new DirectoryNotFoundException(searchDirectory);
            if (fileSizeLimitInBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(fileSizeLimitInBytes));

            SearchDirectory = searchDirectory;
            FileSizeLimitInBytes = fileSizeLimitInBytes;
        }

        public Task SearchAsync(string toFind)
        {
            return SearchInit(toFind);
        }

        public void Cancel()
        {
            _textSearcher?.Cancel();

            _cancellationSource?.Cancel();
            _cancellationSource = null;
        }

        private Task SearchInit(string toFind)
        {
            _cancellationSource = new CancellationTokenSource();

            _textSearcher = new KmpSearcher(Encoding.GetBytes(toFind));
            return Task.Run(() => SearchInDirectory(_textSearcher, SearchDirectory), _cancellationSource.Token);
        }

        // TODO: Make async enumerable with c# 8 and netcoreapp31
        private async Task SearchInDirectory(ITextSearcher textSearcher, string directory)
        {
            foreach (var file in Directory.EnumerateFiles(SearchDirectory))
            {
                if (_cancellationSource?.IsCancellationRequested ?? true)
                    return;

                if (new FileInfo(file).Length > FileSizeLimitInBytes)
                    continue;

                // SearchAsync string in file
                var foundOffset = await textSearcher.SearchAsync(new BinaryReader(File.OpenRead(file)));
                if (foundOffset >= 0)
                    OnFoundMatch(new SequenceSearchResult(file, foundOffset));
            }

            if (!IsSearchSubDirectories)
                return;

            foreach (var dir in Directory.EnumerateDirectories(directory))
                await SearchInDirectory(textSearcher, dir);
        }

        private void OnFoundMatch(SequenceSearchResult result)
        {
            FoundMatch?.Invoke(this, new FoundMatchEventArgs(result));
        }
    }
}
