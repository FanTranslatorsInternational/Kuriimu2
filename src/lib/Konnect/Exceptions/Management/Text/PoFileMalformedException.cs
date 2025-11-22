namespace Konnect.Exceptions.Management.Text;

public class PoFileMalformedException(int line) : Exception($"PO file is malformed. (Line {line})");