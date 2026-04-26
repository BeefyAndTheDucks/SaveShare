using Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Client.ViewModels;

public partial class LocalSaveInfoViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Name { get; set; } = "???";
    
    [ObservableProperty] public partial string? InUseBy { get; set; }

    [ObservableProperty] public partial bool ExistsOnServer { get; set; } = true;

    [ObservableProperty] public partial string? CurrentOperation { get; set; }

    [ObservableProperty] public partial double CurrentOperationProgress { get; set; }


    [ObservableProperty] public partial string? CurrentSubOperation { get; set; }

    [ObservableProperty] public partial bool CurrentSubOperationIndeterminate { get; set; }

    [ObservableProperty] public partial double CurrentSubOperationProgress { get; set; }
    
    public SaveId Id { get; set; }
}