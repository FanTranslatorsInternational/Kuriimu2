namespace Konnect.Contract.Enums.Management.Files;

public enum LoadErrorReason
{
    None,
    Loading,
    Deprecated,
    NoPlugin,
    NoArchive,
    StateCreateError,
    StateNoLoad,
    StateLoadError
}