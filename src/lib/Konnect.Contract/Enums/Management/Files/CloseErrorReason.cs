namespace Konnect.Contract.Enums.Management.Files;

public enum CloseErrorReason
{
    None,
    //$"File {fileState.AbsoluteDirectory / fileState.FilePath.ToRelative()} is already closing."
    Closing,
    //$"File {fileState.AbsoluteDirectory / fileState.FilePath.ToRelative()} is currently saving."
    Saving,
    //"The given file is not loaded anymore."
    NotLoaded
}