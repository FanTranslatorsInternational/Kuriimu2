namespace Konnect.Contract.Enums.Management.Files;

public enum LoadErrorReason
{
    None,
    Loading,
    Deprecated,
    MultiplePlugins,
    NoPlugin,
    NoOptions,
    NoArchive,
    StateCreateError,
    StateNoLoad,
    StateLoadError
}