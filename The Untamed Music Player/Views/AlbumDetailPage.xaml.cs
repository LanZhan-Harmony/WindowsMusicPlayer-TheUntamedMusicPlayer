using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace The_Untamed_Music_Player.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AlbumDetailPage : Page
{
    public AlbumDetailViewModel ViewModel
    {
        get;
    }
    public AlbumDetailPage()
    {
        ViewModel = App.GetService<AlbumDetailViewModel>();
        InitializeComponent();
    }
}
