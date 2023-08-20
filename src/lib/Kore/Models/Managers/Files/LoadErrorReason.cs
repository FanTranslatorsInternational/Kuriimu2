namespace Kore.Models.Managers.Files
{
    public enum LoadErrorReason
    {
        None,
        Loading,
        NoPlugin,
        NoArchive,
        StateCreateError,
        StateNoLoad,
        StateLoadError
    }
}
