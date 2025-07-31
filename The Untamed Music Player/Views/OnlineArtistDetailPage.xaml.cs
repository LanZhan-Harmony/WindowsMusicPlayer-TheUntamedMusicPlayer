using System.Numerics;
using CommunityToolkit.WinUI.Animations.Expressions;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Controls;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using EF = CommunityToolkit.WinUI.Animations.Expressions.ExpressionFunctions;

namespace The_Untamed_Music_Player.Views;

public sealed partial class OnlineArtistDetailPage : Page
{
    public OnlineArtistDetailViewModel ViewModel { get; }

    // 滚动进度的范围
    private int ClampSize => GetValue(50, 80, 107);

    // 背景在滚动时的缩放比例
    private float BackgroundScaleFactor => GetValue(0.80f, 0.68f, 0.61f);

    // 封面在滚动时的缩放比例
    private float CoverScaleFactor => GetValue(0.632479f, 0.528571f, 0.488888f);

    // 按钮面板在滚动时的偏移量
    private int ButtonPanelOffset => GetValue(50, 76, 105);

    // 偏移量
    private int SelectorBarOffset => GetValue(50, 77, 79);

    // 背景的高度
    private float BackgroundVisualHeight => (float)(Header.ActualHeight * 2.5);

    private ScrollViewer? _scrollViewer;
    private CompositionPropertySet? _props;
    private Compositor? _compositor;
    private SpriteVisual? _backgroundVisual;

    private int SelectionBarSelectedIndex
    {
        get;
        set
        {
            field = value;
            ViewModel.SaveSelectionBarSelectedIndex(value);
        }
    } = 0;

    public OnlineArtistDetailPage()
    {
        ViewModel = App.GetService<OnlineArtistDetailViewModel>();
        _ = InitializeAsync();
        InitializeComponent();
    }

    private async Task InitializeAsync()
    {
        SelectionBarSelectedIndex = await ViewModel.LoadSelectionBarSelectedIndex();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (Data.ShellViewModel!.NavigatePage == nameof(OnlineArtistsPage))
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
            && Data.ShellViewModel!.NavigatePage == nameof(OnlineArtistsPage)
        )
        {
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("BackConnectedAnimation", CoverArt);
        }
    }

    private void OnlineArtistDetailPage_Loaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer = SharedScrollViewer;
        var listScrollerPropertySet =
            ElementCompositionPreview.GetScrollViewerManipulationPropertySet(_scrollViewer);
        _compositor = listScrollerPropertySet.Compositor;
        _props = _compositor.CreatePropertySet();
        _props.InsertScalar("progress", 0);
        _props.InsertScalar("clampSize", ClampSize);
        _props.InsertScalar("backgroundScaleFactor", BackgroundScaleFactor);
        _props.InsertScalar("coverScaleFactor", CoverScaleFactor);
        _props.InsertScalar("buttonPanelOffset", ButtonPanelOffset);
        _props.InsertScalar("selectorBarOffset", SelectorBarOffset);
        _props.InsertScalar("headerPadding", 12);
        var listScrollingProperties =
            listScrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>();
        CreateHeaderAnimation(_props, listScrollingProperties.Translation.Y);

        CreateImageBackgroundGradientVisual(
            listScrollingProperties.Translation.Y,
            ViewModel.BriefArtist.CoverPath
        );

        _scrollViewer.ViewChanged += async (s, e) =>
        {
            if (
                !ViewModel.Artist.HasAllLoaded
                && _scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight
                    >= _scrollViewer.ExtentHeight - 50
            )
            {
                await ViewModel.SearchMore();
                await Task.Delay(3000);
            }
        };
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
        var selectorBarOffsetNode = props.GetScalarProperty("selectorBarOffset");
        ExpressionNode progressAnimation = EF.Clamp(-scrollVerticalOffset / clampSizeNode, 0, 1);
        propSet.StartAnimation("progress", progressAnimation);
        var backgroundVisual = ElementCompositionPreview.GetElementVisual(BackgroundAcrylic);
        ExpressionNode backgroundScaleAnimation = EF.Lerp(
            1,
            backgroundScaleFactorNode,
            progressNode
        );
        ExpressionNode backgroundOpacityAnimation = progressNode;
        backgroundVisual.StartAnimation("Scale.Y", backgroundScaleAnimation);
        backgroundVisual.StartAnimation("Opacity", backgroundOpacityAnimation);
        var contentVisual = ElementCompositionPreview.GetElementVisual(ContentContainer);
        ElementCompositionPreview.SetIsTranslationEnabled(ContentContainer, true);
        ExpressionNode contentTranslationAnimation = progressNode * headerPaddingNode;
        contentVisual.StartAnimation("Translation.Y", contentTranslationAnimation);
        var coverArtVisual = ElementCompositionPreview.GetElementVisual(CoverArt);
        ElementCompositionPreview.SetIsTranslationEnabled(CoverArt, true);
        ExpressionNode coverArtScaleAnimation = EF.Lerp(1, coverScaleFactorNode, progressNode);
        ExpressionNode coverArtTranslationAnimation = progressNode * headerPaddingNode;
        coverArtVisual.StartAnimation("Scale.X", coverArtScaleAnimation);
        coverArtVisual.StartAnimation("Scale.Y", coverArtScaleAnimation);
        coverArtVisual.StartAnimation("Translation.X", coverArtTranslationAnimation);
        var textVisual = ElementCompositionPreview.GetElementVisual(TextPanel);
        ElementCompositionPreview.SetIsTranslationEnabled(TextPanel, true);
        ExpressionNode textTranslationAnimation =
            progressNode * (-clampSizeNode + headerPaddingNode);
        textVisual.StartAnimation("Translation.X", textTranslationAnimation);
        var subtitleVisual = ElementCompositionPreview.GetElementVisual(SubtitleText);
        var captionVisual = ElementCompositionPreview.GetElementVisual(CaptionText);
        var introductionVisual = ElementCompositionPreview.GetElementVisual(IntroductionText);
        var fadeThreshold = ExpressionValues.Constant.CreateConstantScalar("fadeThreshold", 0.6f);
        ExpressionNode textFadeAnimation =
            1 - EF.Conditional(progressNode < fadeThreshold, progressNode / fadeThreshold, 1);
        subtitleVisual.StartAnimation("Opacity", textFadeAnimation);
        textFadeAnimation.SetScalarParameter("fadeThreshold", 0.2f);
        captionVisual.StartAnimation("Opacity", textFadeAnimation);
        introductionVisual.StartAnimation("Opacity", textFadeAnimation);
        var buttonVisual = ElementCompositionPreview.GetElementVisual(ButtonPanel);
        ElementCompositionPreview.SetIsTranslationEnabled(ButtonPanel, true);
        ExpressionNode buttonTranslationAnimation = progressNode * (-buttonPanelOffsetNode);
        buttonVisual.StartAnimation("Translation.Y", buttonTranslationAnimation);
        var selectorBarVisual = ElementCompositionPreview.GetElementVisual(SelectorBarPanel);
        ElementCompositionPreview.SetIsTranslationEnabled(SelectorBarPanel, true);
        ExpressionNode selectorBarTranslationAnimation = progressNode * (-selectorBarOffsetNode);
        selectorBarVisual.StartAnimation("Translation.Y", selectorBarTranslationAnimation);
    }

    private void CreateImageBackgroundGradientVisual(
        ScalarNode scrollVerticalOffset,
        string? coverPath
    )
    {
        if (_compositor is null || string.IsNullOrEmpty(coverPath))
        {
            return;
        }
        var imageSurface = LoadedImageSurface.StartLoadFromUri(new Uri(coverPath));
        var imageBrush = _compositor.CreateSurfaceBrush(imageSurface);
        imageBrush.HorizontalAlignmentRatio = 0.5f;
        imageBrush.VerticalAlignmentRatio = 0.25f;
        imageBrush.Stretch = CompositionStretch.UniformToFill;
        var gradientBrush = _compositor.CreateLinearGradientBrush();
        gradientBrush.EndPoint = new Vector2(0, 1);
        gradientBrush.MappingMode = CompositionMappingMode.Relative;
        gradientBrush.ColorStops.Add(_compositor.CreateColorGradientStop(0.4f, Colors.White));
        gradientBrush.ColorStops.Add(_compositor.CreateColorGradientStop(1, Colors.Transparent));
        var maskBrush = _compositor.CreateMaskBrush();
        maskBrush.Source = imageBrush;
        maskBrush.Mask = gradientBrush;
        var visual = _backgroundVisual = _compositor.CreateSpriteVisual();
        visual.Size = new Vector2((float)BackgroundHost.ActualWidth, BackgroundVisualHeight);
        visual.Opacity = 0.15f;
        visual.Brush = maskBrush;
        visual.StartAnimation("Offset.Y", scrollVerticalOffset);
        imageBrush.StartAnimation("Offset.Y", -scrollVerticalOffset * 0.8f);
        ElementCompositionPreview.SetElementChildVisual(BackgroundHost, visual);
    }

    private void CoverArt_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _props?.InsertScalar("clampSize", ClampSize);
        _props?.InsertScalar("backgroundScaleFactor", BackgroundScaleFactor);
        _props?.InsertScalar("coverScaleFactor", CoverScaleFactor);
        _props?.InsertScalar("buttonPanelOffset", ButtonPanelOffset);
        _props?.InsertScalar("selectorBarOffset", SelectorBarOffset);
    }

    private void BackgroundHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_backgroundVisual is null)
        {
            return;
        }
        _backgroundVisual.Size = new Vector2((float)e.NewSize.Width, BackgroundVisualHeight);
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var menuButton = grid?.FindName("MenuButton") as Button;
        checkBox?.Visibility = Visibility.Visible;
        playButton?.Visibility = Visibility.Visible;
        menuButton?.Visibility = Visibility.Visible;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var menuButton = grid?.FindName("MenuButton") as Button;
        checkBox?.Visibility = Visibility.Collapsed;
        playButton?.Visibility = Visibility.Collapsed;
        menuButton?.Visibility = Visibility.Collapsed;
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

    private void SelectorBar_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is SelectorBar selectorBar)
        {
            var selectedItem = selectorBar.Items[SelectionBarSelectedIndex];
            selectorBar.SelectedItem = selectedItem;
        }
    }

    private void SelectorBar_SelectionChanged(
        SelectorBar sender,
        SelectorBarSelectionChangedEventArgs args
    )
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        if (currentSelectedIndex == 0)
        {
            AlbumListView.Visibility = Visibility.Visible;
            AlbumGridView.Visibility = Visibility.Collapsed;
        }
        else
        {
            AlbumListView.Visibility = Visibility.Collapsed;
            AlbumGridView.Visibility = Visibility.Visible;
        }
        SelectionBarSelectedIndex = currentSelectedIndex;
    }

    private void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IBriefSongInfoBase info)
        {
            ViewModel.SongListView_ItemClick(info);
        }
    }

    private void SongListViewPlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.SongListViewPlayButton_Click(info);
        }
    }

    private void SongListViewPlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.SongListViewPlayNextButton_Click(info);
        }
    }

    private async void SongListViewDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            await DownloadHelper.DownloadOnlineSongAsync(info);
        }
    }

    private async void SongListViewPropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            var song = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(info);
            if (song is not null)
            {
                var dialog = new PropertiesDialog(song) { XamlRoot = XamlRoot };
                await dialog.ShowAsync();
            }
        }
    }

    private void SongListViewShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.SongListViewShowAlbumButton_Click(info);
        }
    }

    private void SongListViewSelectButton_Click(object sender, RoutedEventArgs e) { }

    private void AlbumGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IOnlineArtistAlbumInfo info)
        {
            var onlineAlbumInfo = IBriefOnlineAlbumInfo.CreateFromArtistAlbumAsync(info);
            if (onlineAlbumInfo is not null)
            {
                var grid = (Grid)
                    (
                        (ContentControl)AlbumGridView.ContainerFromItem(e.ClickedItem)
                    ).ContentTemplateRoot;
                var border = (Border)grid.Children[1];
                ConnectedAnimationService
                    .GetForCurrentView()
                    .PrepareToAnimate("ForwardConnectedAnimation", border);
                Data.SelectedOnlineAlbum = onlineAlbumInfo;
                Data.ShellPage!.Navigate(
                    nameof(OnlineAlbumDetailPage),
                    nameof(OnlineArtistDetailPage),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    private void AlbumGridViewPlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IOnlineArtistAlbumInfo info })
        {
            ViewModel.AlbumGridViewPlayButton_Click(info);
        }
    }

    private void AlbumGridViewPlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IOnlineArtistAlbumInfo info })
        {
            ViewModel.AlbumGridViewPlayNextButton_Click(info);
        }
    }

    private void AlbumGridViewShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IOnlineArtistAlbumInfo info })
        {
            var onlineAlbumInfo = IBriefOnlineAlbumInfo.CreateFromArtistAlbumAsync(info);
            if (onlineAlbumInfo is not null)
            {
                var grid = (Grid)
                    ((ContentControl)AlbumGridView.ContainerFromItem(info)).ContentTemplateRoot;
                var border = (Border)grid.Children[1];
                ConnectedAnimationService
                    .GetForCurrentView()
                    .PrepareToAnimate("ForwardConnectedAnimation", border);
                Data.SelectedOnlineAlbum = onlineAlbumInfo;
                Data.ShellPage!.Navigate(
                    nameof(OnlineAlbumDetailPage),
                    nameof(OnlineArtistDetailPage),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    private void AlbumGridViewSelectButton_Click(object sender, RoutedEventArgs e) { }
}
