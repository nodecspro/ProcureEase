#region

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#endregion

namespace ProcureEase.Classes;

public class Request : INotifyPropertyChanged
{
    private ObservableCollection<RequestFile> _requestFiles;
    public int RequestId { get; set; }
    public string RequestName { get; set; }
    public string RequestType { get; set; }
    public string RequestStatus { get; set; }
    public string Notes { get; set; }
    public string DeclineReason { get; set; }
    public int UserId { get; set; }

    public ObservableCollection<RequestFile> RequestFiles
    {
        get => _requestFiles;
        set
        {
            if (_requestFiles != value)
            {
                _requestFiles = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}