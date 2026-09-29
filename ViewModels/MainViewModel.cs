using CommunityToolkit.Mvvm.ComponentModel;

namespace Borealis.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial int TimerSpeed { get; set; } = 1000;
    
    [ObservableProperty]
    public partial bool IsTimerStopped { get; set;} = true;
}
