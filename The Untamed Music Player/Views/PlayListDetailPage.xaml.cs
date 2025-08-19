using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations.Expressions;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Controls;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using EF = CommunityToolkit.WinUI.Animations.Expressions.ExpressionFunctions;

namespace The_Untamed_Music_Player.Views;

public sealed partial class PlayListDetailPage : Page
{
    public PlayListDetailViewModel ViewModel { get; } = App.GetService<PlayListDetailViewModel>();

    // 滚动进度的范围
    private int ClampSize => GetValue(50, 82, 115);

    // 背景在滚动时的缩放比例
    private float BackgroundScaleFactor => GetValue(0.80f, 0.70f, 0.61f);

    // 封面在滚动时的缩放比例
    private float CoverScaleFactor => GetValue(0.632479f, 0.528571f, 0.488888f);

    // 按钮面板在滚动时的偏移量
    private int ButtonPanelOffset => GetValue(30, 40, 40);

    private CompositionPropertySet? _props;
    private Compositor? _compositor;

    public PlayListDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (Data.ShellViewModel!.NavigatePage == nameof(PlayListsPage))
        {
            var animation = ConnectedAnimationService
                .GetForCurrentView()
                .GetAnimation("ForwardConnectedAnimation");
            animation?.TryStart(CoverArt);
        }
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        if (
            e.NavigationMode == NavigationMode.Back
            && Data.ShellViewModel!.NavigatePage == nameof(PlayListsPage)
            && Data.SelectedPlaylist is not null
        )
        {
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("BackConnectedAnimation", CoverArt);
        }
    }

    private void PlayListDetailPage_Loaded(object sender, RoutedEventArgs e)
    {
        var scrollViewer =
            SongListView.FindDescendant<ScrollViewer>()
            ?? throw new Exception("Cannot find ScrollViewer in ListView"); // 检索 ListView 内部使用的 ScrollViewer

        var scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(
            scrollViewer
        ); // 获取 ScrollViewer 中包含滚动值的属性集
        _compositor = scrollerPropertySet.Compositor; // 获取与 ScrollViewer 关联的 Compositor, Compositor 用于创建动画

        // 创建一个属性集，其中包含下面的 ExpressionAnimations 中引用的值
        _props = _compositor.CreatePropertySet();
        _props.InsertScalar("progress", 0); // 插入一个标量值, 用于跟踪滚动进度
        _props.InsertScalar("clampSize", ClampSize);
        _props.InsertScalar("backgroundScaleFactor", BackgroundScaleFactor);
        _props.InsertScalar("coverScaleFactor", CoverScaleFactor);
        _props.InsertScalar("buttonPanelOffset", ButtonPanelOffset);
        _props.InsertScalar("headerPadding", 12);

        var scrollingProperties =
            scrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>(); // 获取属性集的引用节点，以便在表达式动画中使用

        CreateHeaderAnimation(_props, scrollingProperties.Translation.Y);
    }

    private void CreateHeaderAnimation(
        CompositionPropertySet propSet,
        ScalarNode scrollVerticalOffset
    )
    {
        var props = propSet.GetReference();
        var progressNode = props.GetScalarProperty("progress");
        var clampSizeNode = props.GetScalarProperty("clampSize");
        var backgroundScaleFactorNode = props.GetScalarProperty("backgroundScaleFactor");
        var coverScaleFactorNode = props.GetScalarProperty("coverScaleFactor");
        var buttonPanelOffsetNode = props.GetScalarProperty("buttonPanelOffset");
        var headerPaddingNode = props.GetScalarProperty("headerPadding");

        // 创建并启动一个表达式动画，以跟踪滚动进度
        ExpressionNode progressAnimation = EF.Clamp(-scrollVerticalOffset / clampSizeNode, 0, 1);
        propSet.StartAnimation("progress", progressAnimation);

        // 获取头部背景的后备视觉效果，以便可以对其属性进行动画处理
        var backgroundVisual = ElementCompositionPreview.GetElementVisual(BackgroundAcrylic);

        // 创建并启动一个表达式动画，以缩放和淡入标题后面的背景
        ExpressionNode backgroundScaleAnimation = EF.Lerp(
            1,
            backgroundScaleFactorNode,
            progressNode
        );
        ExpressionNode backgroundOpacityAnimation = progressNode;
        backgroundVisual.StartAnimation("Scale.Y", backgroundScaleAnimation);
        backgroundVisual.StartAnimation("Opacity", backgroundOpacityAnimation);

        // 获取内容容器的后备视觉效果，以便可以对其属性进行动画处理
        var contentVisual = ElementCompositionPreview.GetElementVisual(ContentContainer);
        ElementCompositionPreview.SetIsTranslationEnabled(ContentContainer, true);

        // 创建并启动一个表达式动画，以滚动位置移动内容容器
        ExpressionNode contentTranslationAnimation = progressNode * headerPaddingNode;
        contentVisual.StartAnimation("Translation.Y", contentTranslationAnimation);

        // 获取封面艺术视觉的后备视觉效果，以便可以对其属性进行动画处理
        var coverArtVisual = ElementCompositionPreview.GetElementVisual(CoverArt);
        ElementCompositionPreview.SetIsTranslationEnabled(CoverArt, true);

        // 创建并启动一个表达式动画，以滚动位置缩放和移动封面艺术
        ExpressionNode coverArtScaleAnimation = EF.Lerp(1, coverScaleFactorNode, progressNode);
        ExpressionNode coverArtTranslationAnimation = progressNode * headerPaddingNode;
        coverArtVisual.StartAnimation("Scale.X", coverArtScaleAnimation);
        coverArtVisual.StartAnimation("Scale.Y", coverArtScaleAnimation);
        coverArtVisual.StartAnimation("Translation.X", coverArtTranslationAnimation);

        // 获取文本面板的后备视觉效果，以便可以对其属性进行动画处理
        var textVisual = ElementCompositionPreview.GetElementVisual(TextPanel);
        ElementCompositionPreview.SetIsTranslationEnabled(TextPanel, true);

        // 创建并启动一个表达式动画，以滚动位置移动文本面板
        ExpressionNode textTranslationAnimation =
            progressNode * (-clampSizeNode + headerPaddingNode);
        textVisual.StartAnimation("Translation.X", textTranslationAnimation);

        // 获取附加文本块后备视觉效果，以便可以对其属性进行动画处理
        var captionVisual = ElementCompositionPreview.GetElementVisual(CaptionText);

        // 创建一个表达式动画，以开始使用附加文本块的阈值进行不透明度淡出动画
        var fadeThreshold = ExpressionValues.Constant.CreateConstantScalar("fadeThreshold", 0.6f);
        ExpressionNode textFadeAnimation =
            1 - EF.Conditional(progressNode < fadeThreshold, progressNode / fadeThreshold, 1);

        // 在附加文本块视觉上启动不透明度淡出动画
        textFadeAnimation.SetScalarParameter("fadeThreshold", 0.2f);
        captionVisual.StartAnimation("Opacity", textFadeAnimation);

        // 获取按钮面板的后备视觉效果，以便可以对其属性进行动画处理
        var buttonVisual = ElementCompositionPreview.GetElementVisual(ButtonPanel);
        ElementCompositionPreview.SetIsTranslationEnabled(ButtonPanel, true);

        // 创建并启动一个表达式动画，以滚动位置移动按钮面板
        ExpressionNode buttonTranslationAnimation = progressNode * (-buttonPanelOffsetNode);
        buttonVisual.StartAnimation("Translation.Y", buttonTranslationAnimation);
    }

    private void PlaylistArt_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _props?.InsertScalar("clampSize", ClampSize);
        _props?.InsertScalar("backgroundScaleFactor", BackgroundScaleFactor);
        _props?.InsertScalar("coverScaleFactor", CoverScaleFactor);
        _props?.InsertScalar("buttonPanelOffset", ButtonPanelOffset);
    }

    private T GetValue<T>(T small, T medium, T large)
    {
        if (ActualWidth < 641)
        {
            return small;
        }
        else if (ActualWidth < 850)
        {
            return medium;
        }
        else
        {
            return large;
        }
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        checkBox?.Visibility = Visibility.Visible;
        playButton?.Visibility = Visibility.Visible;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        checkBox?.Visibility = Visibility.Collapsed;
        playButton?.Visibility = Visibility.Collapsed;
    }

    private async void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new RenamePlaylistInfoDialog(ViewModel.Playlist) { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.PlayButton_Click(info.Song);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.PlayNextButton_Click(info.Song);
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.RemoveButton_Click(info);
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.MoveUpButton_Click(info);
        }
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.MoveDownButton_Click(info);
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.ShowAlbumButton_Click(info.Song);
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.ShowArtistButton_Click(info.Song);
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            var song = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(info.Song);
            var dialog = new PropertiesDialog(song) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
