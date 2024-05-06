using System.Collections.ObjectModel;

namespace ProcureEase.Classes;

public class Request
{
    public int RequestId { get; init; }

    public string? RequestName { get; init; }

    public string? RequestType { get; init; }

    public string? RequestStatus { get; set; }

    public string? Notes { get; init; }

    public int UserId { get; init; }

    public ObservableCollection<RequestFile> RequestFiles { get; set; } = [];
}