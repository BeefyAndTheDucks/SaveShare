using Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Client.ViewModels;

public partial class CloudSaveInfoViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Name { get; set; } = "???";
    
    [ObservableProperty]
    public partial bool IsAvailableForDownload { get; set; }
    
    [ObservableProperty]
    public partial string? IsNotAvailableForDownloadReason { get; set; }
    
    public SaveId SaveId { get; init; }
}