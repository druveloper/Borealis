using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avaTest.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private WriteableBitmap? _mainBitmap;

    public MainViewModel()
    {
        //_mainBitmap = newBitmap;
    }
}
