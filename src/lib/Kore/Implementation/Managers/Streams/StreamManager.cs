﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using Kontract.Interfaces.Managers.Streams;
using Kontract.Models.FileSystem;
using Kore.Models.Managers.Streams;
using Serilog;

namespace Kore.Implementation.Managers.Streams
{
    // TODO: Make internal again
    /// <summary>
    /// Provides and manages streams and their lifetime.
    /// </summary>
    public class StreamManager : IStreamManager
    {
        private readonly Timer _streamCollectionTimer;
        private readonly object _releaseLock = new object();

        private readonly Guid _guid;

        private readonly IList<System.IO.Stream> _streams;
        private readonly IDictionary<System.IO.Stream, System.IO.Stream> _parentStreams;

        public const string TemporaryDirectory = "tmp";

        /// <inheritdoc />
        public ILogger Logger { get; set; }

        /// <inheritdoc />
        public int Count => _streams.Count(x => !IsStreamClosed(x));

        public StreamManager()
        {
            _streamCollectionTimer = new Timer(1000.0);
            _streamCollectionTimer.Elapsed += StreamCollectionTimer_Elapsed;
            _streamCollectionTimer.Start();

            _guid = Guid.NewGuid();

            _streams = new List<System.IO.Stream>();
            _parentStreams = new Dictionary<System.IO.Stream, System.IO.Stream>();
        }

        /// <inheritdoc />
        public ITemporaryStreamProvider CreateTemporaryStreamProvider()
        {
            var tempDirectory = UPath.Combine(TemporaryDirectory, _guid.ToString("D"));
            return new TemporaryStreamProvider(Path.GetFullPath(tempDirectory.FullName), this);
        }

        /// <inheritdoc />
        public System.IO.Stream WrapUndisposable(System.IO.Stream wrap)
        {
            var undisposable = new UndisposableStream(wrap);

            if (_streams.Contains(wrap))
                _parentStreams[undisposable] = wrap;
            _streams.Add(undisposable);

            return undisposable;
        }

        /// <inheritdoc />
        public void Register(System.IO.Stream stream, System.IO.Stream parent = null)
        {
            if (ContainsStream(stream))
                throw new InvalidOperationException("The stream is already managed by this provider.");

            if (parent != null && !ContainsStream(parent))
                throw new InvalidOperationException("The parent stream has to be managed by this provider.");

            _streams.Add(stream);
            if (parent != null)
                _parentStreams[parent] = stream;
        }

        /// <inheritdoc />
        public bool ContainsStream(System.IO.Stream stream)
        {
            return _streams.Contains(stream);
        }

        /// <inheritdoc />
        public void Release(System.IO.Stream release, bool recursive = false)
        {
            if (release == null)
            {
                Logger?.Error("Probable race condition in stream manager.");
                return;
            }

            if (!ContainsStream(release))
                throw new InvalidOperationException("The stream is not managed by this provider.");

            // Close all children of the given stream too
            if (recursive && _parentStreams.ContainsKey(release))
            {
                Release(_parentStreams[release], true);
                _parentStreams.Remove(release);
            }

            // Release the given stream
            release.Dispose();
            _streams.Remove(release);
        }

        /// <inheritdoc />
        public void ReleaseAll()
        {
            _parentStreams.Clear();

            foreach (var stream in _streams.ToList())
            {
                lock (_releaseLock)
                {
                    if (ContainsStream(stream))
                        Release(stream);
                }
            }
        }

        /// <inheritdoc cref="Dispose"/>
        public void Dispose()
        {
            _streamCollectionTimer?.Dispose();
            ReleaseAll();
        }

        /// <summary>
        /// Acts as the garbage collection process per interval for this instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StreamCollectionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var stream in _streams.Where(x => x != null).ToList())
            {
                if (!IsStreamClosed(stream))
                    continue;

                lock (_releaseLock)
                {
                    if (ContainsStream(stream))
                        Release(stream, true);
                }
            }
        }

        /// <summary>
        /// Checks if a given stream is already closed.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private bool IsStreamClosed(System.IO.Stream stream)
        {
            return !stream.CanRead && !stream.CanWrite && !stream.CanSeek;
        }
    }
}
