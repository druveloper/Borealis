using CommunityToolkit.Mvvm.ComponentModel;

namespace Borealis.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial int TimerSpeed { get; set; } = 1000;
}
