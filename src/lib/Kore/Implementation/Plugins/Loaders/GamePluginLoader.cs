namespace Kore.Implementation.Plugins.Loaders
{
    //class CsGamePluginLoader : CsPluginLoader, IPluginLoader<IGameAdapter>
    //{
    //    /// <inheritdoc />
    //    public IReadOnlyList<PluginLoadError> LoadErrors { get; }

    //    /// <inheritdoc />
    //    public IReadOnlyList<IGameAdapter> Plugins { get; private set; }

    //    public CsGamePluginLoader(params Assembly[] pluginAssemblies)
    //    {
    //        if (!TryLoadPlugins<IGameAdapter>(pluginAssemblies, out var plugins, out var errors))
    //            LoadErrors = errors;

    //        Plugins = plugins;
    //    }

    //    public CsGamePluginLoader(params string[] pluginPaths)
    //    {
    //        if (!TryLoadPlugins<IGameAdapter>(pluginPaths, out var plugins, out var errors))
    //            LoadErrors = errors;

    //        Plugins = plugins;
    //    }

    //    /// <inheritdoc />
    //    public bool Exists(Guid pluginId)
    //    {
    //        return Plugins.Any(p => p.PluginId == pluginId);
    //    }

    //    /// <inheritdoc />
    //    public IGameAdapter GetPlugin(Guid pluginId)
    //    {
    //        return Plugins.FirstOrDefault(ep => ep.PluginId == pluginId);
    //    }
    //}
}
