namespace Kore.Models.Managers.Files
{
    public enum SaveErrorReason
    {
        None,
        Closed,
        Saving,
        Closing,
        NotLoaded,
        NoChanges,
        StateSaveError,
        DestinationNotExist,
        FileReplaceError,
        FileCopyError,
        StateReloadError
    }
}
