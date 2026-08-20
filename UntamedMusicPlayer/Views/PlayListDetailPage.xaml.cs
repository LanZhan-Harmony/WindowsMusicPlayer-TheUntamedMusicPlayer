using System.Numerics;
using CommunityToolkit.WinUI;
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
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Helpers;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.Services;
using UntamedMusicPlayer.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using EF = CommunityToolkit.WinUI.Animations.Expressions.ExpressionFunctions;

namespace UntamedMusicPlayer.Views;

public sealed partial class PlayListDetailPage : Page
{
    public PlayListDetailViewModel ViewModel { get; } = App.GetService<PlayListDetailViewModel>();
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();

    // 滚动进度的范围
    private int ClampSize => GetValue(50, 82, 115);

    // 背景在滚动时的缩放比例
    private float BackgroundScaleFactor => GetValue(0.80f, 0.70f, 0.61f);

    // 封面在滚动时的缩放比例
    private float CoverScaleFactor => GetValue(0.632479f, 0.528571f, 0.488888f);

    // 按钮面板在滚动时的偏移量
    private int ButtonPanelOffset => GetValue(30, 40, 40);

    // 背景的高度
    private float BackgroundVisualHeight => (float)(Header.ActualHeight * 2.5);

    private CompositionPropertySet? _props;
    private Compositor? _compositor;
    private SpriteVisual? _backgroundVisual;
    private LoadedImageSurface? _imageSurface;

    public PlayListDetailPage()
    {
        InitializeComponent();
    }

    private void SongListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.Count > 0)
        {
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }

    private void SongListView_DragItemsCompleted(object sender, DragItemsCompletedEventArgs args)
    {
        if (args.DropResult == DataPackageOperation.Move && args.Items.Count > 0)
        {
            ViewModel.SongListView_DragItemsCompleted(args.Items.OfType<IndexedPlaylistSong>());
        }
    }

    private void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IndexedPlaylistSong info)
        {
            ViewModel.SongListView_ItemClick(info);
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is PlaylistInfo playlist)
        {
            ViewModel.Initialize(playlist);
        }

        if (_navigationService.NavigationSourcePage == nameof(PlayListsPage))
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
            && _navigationService.NavigationSourcePage == nameof(PlayListsPage)
            && ViewModel.Playlist is not null
        )
        {
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("BackConnectedAnimation", CoverArt);
        }
        Cleanup();
    }

    private void Cleanup()
    {
        if (_backgroundVisual is not null)
        {
            ElementCompositionPreview.SetElementChildVisual(BackgroundHost, null);
            _backgroundVisual.Dispose();
            _backgroundVisual = null;
        }
        _imageSurface?.Dispose();
        _imageSurface = null;
    }

    private void PlayListDetailPage_Loaded(object sender, RoutedEventArgs e)
    {
        var scrollViewer = SongListView.FindDescendant<ScrollViewer>();

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
        if (ViewModel.Playlist.IsCoverEdited && ViewModel.Playlist.CoverPaths.Count != 0)
        {
            CreateImageBackgroundGradientVisual(
                scrollingProperties.Translation.Y,
                ViewModel.Playlist.CoverPaths[0]
            );
        }
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
        ExpressionNode backgroundOpacityAnimation = progressNode * 0.7f;
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

    private void CreateImageBackgroundGradientVisual(
        ScalarNode scrollVerticalOffset,
        string imagePath
    )
    {
        if (_compositor is null)
        {
            return;
        }
        _imageSurface = LoadedImageSurface.StartLoadFromUri(
            new Uri(imagePath),
            new Size(1000, 1000)
        );
        var imageBrush = _compositor.CreateSurfaceBrush(_imageSurface);
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

    private void BackgroundHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_backgroundVisual is null)
        {
            return;
        }
        _backgroundVisual.Size = new Vector2((float)e.NewSize.Width, BackgroundVisualHeight);
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

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem { DataContext: IndexedPlaylistSong info } menuItem)
        {
            while (menuItem.Items.Count > 3)
            {
                menuItem.Items.RemoveAt(3);
            }
            foreach (var playlist in App.GetService<PlaylistLibrary>().Playlists)
            {
                var playlistMenuItem = new MenuFlyoutItem
                {
                    Text = playlist.Name,
                    DataContext = new Tuple<IBriefSongInfoBase, PlaylistInfo>(info.Song, playlist),
                };
                playlistMenuItem.Click += PlaylistMenuItem_Click;
                menuItem.Items.Add(playlistMenuItem);
            }
        }
    }

    private void PlaylistMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: Tuple<IBriefSongInfoBase, PlaylistInfo> tuple })
        {
            var (songInfo, playlist) = tuple;
            ViewModel.AddToPlaylistCommand.ExecuteAsync(
                new Tuple<IBriefSongInfoBase, PlaylistInfo>(songInfo, playlist)
            );
        }
    }

    private void AddToFlyout_Opened(object sender, object e)
    {
        if (sender is MenuFlyout flyout)
        {
            while (flyout.Items.Count > 3)
            {
                flyout.Items.RemoveAt(3);
            }
            foreach (var playlist in App.GetService<PlaylistLibrary>().Playlists)
            {
                var playlistMenuItem = new MenuFlyoutItem
                {
                    Text = playlist.Name,
                    DataContext = playlist,
                };
                playlistMenuItem.Click += AddToPlaylistFlyoutButton_Click;
                flyout.Items.Add(playlistMenuItem);
            }
        }
    }

    private void AddToPlaylistFlyoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: PlaylistInfo playlist })
        {
            ViewModel.AddAllToPlaylistCommand.ExecuteAsync(playlist);
        }
    }

    private void AddToPlayQueueFlyoutButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddAllToPlayQueueCommand.Execute(null);
    }

    private async void AddToNewPlaylistFlyoutButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
        {
            await ViewModel.AddAllToPlaylistCommand.ExecuteAsync(dialog.CreatedPlaylist);
        }
    }

    private async void EditInfoButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EditPlaylistInfoDialog(ViewModel.Playlist) { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = Application.Current.Resources["NormalContentDialogStyle"] as Style,
            RequestedTheme = ThemeSelectorService.IsDarkTheme
                ? ElementTheme.Dark
                : ElementTheme.Light,
            Title = new TextBlock
            {
                Text = ResourceExtensions.GetLocalized("PlayLists_DeleteDialogTitle"),
            },
            Content = "PlayLists_DeleteDialogContent".GetLocalizedWithReplace(
                "{title}",
                ViewModel.PlaylistName
            ),
            PrimaryButtonText = ResourceExtensions.GetLocalized("PlayLists_DeleteDialogPrimary"),
            CloseButtonText = ResourceExtensions.GetLocalized("PlayLists_DeleteDialogClose"),
            DefaultButton = ContentDialogButton.Close,
        };
        dialog.EnableLightDismiss();
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            App.GetService<INavigationService>().GoBackShell();
            App.GetService<PlaylistLibrary>().DeletePlaylist(ViewModel.Playlist);
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.PlayCommand.Execute(info.Song);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.PlayNextCommand.Execute(info.Song);
        }
    }

    private void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.AddToPlayQueueCommand.Execute(info.Song);
        }
    }

    private async void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
            {
                await ViewModel.AddToPlaylistCommand.ExecuteAsync(
                    new Tuple<IBriefSongInfoBase, PlaylistInfo>(info.Song, dialog.CreatedPlaylist)
                );
            }
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.RemoveCommand.ExecuteAsync(info);
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.MoveUpCommand.Execute(info);
        }
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.MoveDownCommand.Execute(info);
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.ShowAlbumCommand.ExecuteAsync(info.Song);
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            ViewModel.ShowArtistCommand.ExecuteAsync(info.Song);
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlaylistSong info })
        {
            var song = await CloudMusicModelFactory.CreateDetailedSongAsync(
                info.Song,
                App.GetService<CloudMusicApiService>()
            );
            var dialog = new PropertiesDialog(song) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
