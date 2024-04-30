namespace ProcureEase.Classes;

public class RequestFile
{
    public int RequestFileId { get; set; }

    public int RequestId { get; set; }

    public string FileName { get; init; }

    public byte[]? FileData { get; init; }

    public override string ToString()
    {
        return
            $"{nameof(RequestFileId)}: {RequestFileId}, {nameof(RequestId)}: {RequestId}, {nameof(FileName)}: {FileName}, {nameof(FileData)}: {FileData}";
    }
}