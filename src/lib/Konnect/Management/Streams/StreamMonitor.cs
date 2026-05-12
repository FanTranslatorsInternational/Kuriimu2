using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Streams;
using Konnect.FileSystem;
using Serilog;

namespace Konnect.Management.Streams;

internal class StreamMonitor : IDisposable
{
    private const string TempFolder_ = "tmp";

    private readonly object _elapsedLocked = new();
    private readonly object _streamManagersLock = new();
    private bool _isCollecting;

    private readonly System.Timers.Timer _temporaryContainerCollectionTimer;

    private readonly List<IStreamManager> _streamManagers = [];
    private readonly ConcurrentDictionary<IFileSystem, (IStreamManager, UPath)> _temporaryFileSystemMapping = [];
    private readonly ConcurrentDictionary<IStreamManager, (IFileSystem?, UPath)> _streamManagerMapping = [];

    private ILogger? _logger;

    public ILogger? Logger
    {
        get => _logger;
        set => SetLogger(value);
    }

    public StreamMonitor()
    {
        _temporaryContainerCollectionTimer = new System.Timers.Timer(1000.0);
        _temporaryContainerCollectionTimer.Elapsed += TemporaryContainerCollectionTimer_Elapsed;
        _temporaryContainerCollectionTimer.Start();
    }

    public IStreamManager CreateStreamManager()
    {
        var streamManager = new StreamManager { Logger = _logger };

        lock (_streamManagersLock)
            _streamManagers.Add(streamManager);

        return streamManager;
    }

    public IFileSystem CreateTemporaryFileSystem()
    {
        var streamManager = CreateStreamManager();

        var tempDirectory = CreateTemporaryDirectory();
        var temporaryFileSystem = FileSystemFactory.CreatePhysicalSubFileSystem(tempDirectory, streamManager);

        _temporaryFileSystemMapping.GetOrAdd(temporaryFileSystem, _ => (streamManager, tempDirectory));
        _streamManagerMapping.GetOrAdd(streamManager, _ => (temporaryFileSystem, tempDirectory));

        return temporaryFileSystem;
    }

    public IStreamManager GetStreamManager(IFileSystem fileSystem)
    {
        if (_temporaryFileSystemMapping.TryGetValue(fileSystem, out var element))
            return element.Item1;

        throw new InvalidOperationException("The file system was not created by this instance.");
    }

    public bool Manages(IStreamManager manager)
    {
        lock (_streamManagersLock)
            return _streamManagers.Contains(manager);
    }

    public void RemoveStreamManager(IStreamManager streamManager)
    {
        if (_streamManagerMapping.ContainsKey(streamManager))
            throw new InvalidOperationException("The given stream manager is used for a temporary file system. Release the temporary file system instead.");

        lock (_streamManagersLock)
        {
            if (!_streamManagers.Contains(streamManager))
                throw new InvalidOperationException("The given stream manager is not monitored by this instance.");

            _streamManagers.Remove(streamManager);
        }
    }

    public void ReleaseTemporaryFileSystem(IFileSystem temporaryFileSystem)
    {
        if (!_temporaryFileSystemMapping.TryRemove(temporaryFileSystem, out var element))
            return;

        _streamManagerMapping.AddOrUpdate(element.Item1, _ => (null, element.Item2), (_, _) => (null, element.Item2));
    }

    public void Dispose()
    {
        _temporaryContainerCollectionTimer.Dispose();

        foreach (var streamManager in _streamManagers)
            streamManager.ReleaseAll();

        foreach (var mapping in _streamManagerMapping)
            RemoveDirectory(mapping.Value.Item2.FullName);

        _streamManagers.Clear();
        _streamManagerMapping.Clear();
        _temporaryFileSystemMapping.Clear();
    }

    /// <summary>
    /// Acts as the garbage collection process per interval for this instance.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TemporaryContainerCollectionTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_elapsedLocked)
        {
            if (_isCollecting)
                return;

            _isCollecting = true;
        }

        foreach (var streamManager in _streamManagerMapping.ToArray().Where(x => x.Value.Item1 == null).Select(x => x.Key))
        {
            if (streamManager.Count > 0)
                continue;

            if (!_streamManagerMapping.TryRemove(streamManager, out var element))
                continue;

            RemoveDirectory(element.Item2.FullName);
        }

        lock (_elapsedLocked)
            _isCollecting = false;
    }

    private static void RemoveDirectory(string? path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    private static string CreateTemporaryDirectory()
    {
        var currentDirectory = GetCurrentDirectory();
        return Path.Combine(currentDirectory, TempFolder_, Guid.NewGuid().ToString("D"));
    }

    private static string GetCurrentDirectory()
    {
        var process = Process.GetCurrentProcess().MainModule;
        return process == null || process.FileVersionInfo.InternalName == ".NET Host"
            ? AppDomain.CurrentDomain.BaseDirectory
            : Path.GetDirectoryName(process.FileName) ?? string.Empty;
    }

    private void SetLogger(ILogger? logger)
    {
        _logger = logger;

        lock (_streamManagersLock)
        {
            foreach (var manager in _streamManagers)
                manager.Logger = logger;
        }
    }
}