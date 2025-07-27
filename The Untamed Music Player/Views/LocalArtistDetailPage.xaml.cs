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
using The_Untamed_Music_Player.Controls;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using EF = CommunityToolkit.WinUI.Animations.Expressions.ExpressionFunctions;

namespace The_Untamed_Music_Player.Views;

public sealed partial class LocalArtistDetailPage : Page
{
    public LocalArtistDetailViewModel ViewModel { get; }

    // 滚动进度的范围
    private int ClampSize => GetValue(50, 80, 107);

    // 背景在滚动时的缩放比例
    private float BackgroundScaleFactor => GetValue(0.80f, 0.68f, 0.61f);

    // 封面在滚动时的缩放比例
    private float CoverScaleFactor => GetValue(0.632479f, 0.528571f, 0.488888f);

    // 按钮面板在滚动时的偏移量
    private int ButtonPanelOffset => GetValue(50, 77, 79);

    // 偏移量
    private int SelectorBarOffset => GetValue(50, 72, 79);

    // 背景的高度
    private float BackgroundVisualHeight => (float)(Header.ActualHeight * 2.5);

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

    public LocalArtistDetailPage()
    {
        ViewModel = App.GetService<LocalArtistDetailViewModel>();
        _ = InitializeAsync();
        InitializeComponent();
    }

    private async Task InitializeAsync()
    {
        SelectionBarSelectedIndex = await ViewModel.LoadSelectionBarSelectedIndex();
    }

    /// <summary>
    /// 当页面导航到此页时，将调用此方法。
    /// </summary>
    /// <param name="e"></param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (Data.ShellViewModel!.NavigatePage == nameof(LocalArtistsPage))
        {
            var animation = ConnectedAnimationService
                .GetForCurrentView()
                .GetAnimation("ForwardConnectedAnimation");
            animation?.TryStart(CoverArt);
        }
    }

    /// <summary>
    /// 当页面从此页导航时，将调用此方法。
    /// </summary>
    /// <param name="e"></param>
    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        if (
            e.NavigationMode == NavigationMode.Back
            && Data.ShellViewModel!.NavigatePage == nameof(LocalArtistsPage)
        )
        {
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("BackConnectedAnimation", CoverArt);
        }
    }

    private void LocalArtistDetailPage_Loaded(object sender, RoutedEventArgs e)
    {
        var listScrollViewer = SharedScrollViewer;

        var listScrollerPropertySet =
            ElementCompositionPreview.GetScrollViewerManipulationPropertySet(listScrollViewer); // 获取 ScrollViewer 中包含滚动值的属性集
        _compositor = listScrollerPropertySet.Compositor; // 获取与 ScrollViewer 关联的 Compositor, Compositor 用于创建动画

        // 创建一个属性集，其中包含下面的 ExpressionAnimations 中引用的值
        _props = _compositor.CreatePropertySet();
        _props.InsertScalar("progress", 0); // 插入一个标量值, 用于跟踪滚动进度
        _props.InsertScalar("clampSize", ClampSize);
        _props.InsertScalar("backgroundScaleFactor", BackgroundScaleFactor);
        _props.InsertScalar("coverScaleFactor", CoverScaleFactor);
        _props.InsertScalar("buttonPanelOffset", ButtonPanelOffset);
        _props.InsertScalar("selectorBarOffset", SelectorBarOffset);
        _props.InsertScalar("headerPadding", 12);

        var listScrollingProperties =
            listScrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>(); // 获取属性集的引用节点，以便在表达式动画中使用

        CreateHeaderAnimation(_props, listScrollingProperties.Translation.Y);

        if (ViewModel.AlbumList.Count != 0)
        {
            var coverBytes = ViewModel.Artist.GetCoverBytes();
            if (coverBytes.Length != 0)
            {
                CreateImageBackgroundGradientVisual(
                    listScrollingProperties.Translation.Y,
                    coverBytes
                );
            }
        }
    }

    /// <summary>
    /// 创建头部的组合动画效果
    /// </summary>
    /// <param name="propSet"></param>
    /// <param name="scrollVerticalOffset"></param>
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

        // 获取封面的后备视觉效果，以便可以对其属性进行动画处理
        var coverArtVisual = ElementCompositionPreview.GetElementVisual(CoverArt);
        ElementCompositionPreview.SetIsTranslationEnabled(CoverArt, true);

        // 创建并启动一个表达式动画，以滚动位置缩放和移动封面
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
        var subtitleVisual = ElementCompositionPreview.GetElementVisual(SubtitleText);
        var captionVisual = ElementCompositionPreview.GetElementVisual(CaptionText);

        // 创建一个表达式动画，以开始使用附加文本块的阈值进行不透明度淡出动画
        var fadeThreshold = ExpressionValues.Constant.CreateConstantScalar("fadeThreshold", 0.6f);
        ExpressionNode textFadeAnimation =
            1 - EF.Conditional(progressNode < fadeThreshold, progressNode / fadeThreshold, 1);

        // 在附加文本块视觉上启动不透明度淡出动画
        subtitleVisual.StartAnimation("Opacity", textFadeAnimation);
        textFadeAnimation.SetScalarParameter("fadeThreshold", 0.2f);
        captionVisual.StartAnimation("Opacity", textFadeAnimation);

        // 获取按钮面板的后备视觉效果，以便可以对其属性进行动画处理
        var buttonVisual = ElementCompositionPreview.GetElementVisual(ButtonPanel);
        ElementCompositionPreview.SetIsTranslationEnabled(ButtonPanel, true);

        // 创建并启动一个表达式动画，以滚动位置移动按钮面板
        ExpressionNode buttonTranslationAnimation = progressNode * (-buttonPanelOffsetNode);
        buttonVisual.StartAnimation("Translation.Y", buttonTranslationAnimation);

        // 获取 SelectorBarPanel 的后备视觉效果
        var selectorBarVisual = ElementCompositionPreview.GetElementVisual(SelectorBarPanel);
        ElementCompositionPreview.SetIsTranslationEnabled(SelectorBarPanel, true);

        // 创建并启动 SelectorBarPanel 的平移动画
        ExpressionNode selectorBarTranslationAnimation = progressNode * (-selectorBarOffsetNode);
        selectorBarVisual.StartAnimation("Translation.Y", selectorBarTranslationAnimation);
    }

    private void CreateImageBackgroundGradientVisual(
        ScalarNode scrollVerticalOffset,
        byte[] imageBytes
    )
    {
        if (_compositor is null)
        {
            return;
        }
        using var stream = new MemoryStream(imageBytes);
        var imageSurface = LoadedImageSurface.StartLoadFromStream(stream.AsRandomAccessStream());
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
        if (e.ClickedItem is BriefLocalSongInfo info)
        {
            ViewModel.SongListView_ItemClick(info);
        }
    }

    private void SongListViewPlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.SongListViewPlayButton_Click(info);
        }
    }

    private void SongListViewPlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.SongListViewPlayNextButton_Click(info);
        }
    }

    private void SongListViewEditInfoButton_Click(object sender, RoutedEventArgs e) { }

    private async void SongListViewPropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            var song = new DetailedLocalSongInfo(info);
            if (song is not null)
            {
                var dialog = new PropertiesDialog(song) { XamlRoot = XamlRoot };
                await dialog.ShowAsync();
            }
        }
    }

    private void SongListViewShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.SongListViewShowAlbumButton_Click(info);
        }
    }

    private void SongListViewSelectButton_Click(object sender, RoutedEventArgs e) { }

    private void AlbumGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LocalArtistAlbumInfo info)
        {
            var localAlbumInfo = Data.MusicLibrary.Albums[info.Name];
            if (localAlbumInfo is not null)
            {
                var grid = (Grid)
                    (
                        (ContentControl)AlbumGridView.ContainerFromItem(e.ClickedItem)
                    ).ContentTemplateRoot;
                var border = (Border)grid.Children[1];
                ConnectedAnimationService
                    .GetForCurrentView()
                    .PrepareToAnimate("ForwardConnectedAnimation", border);
                Data.SelectedLocalAlbum = localAlbumInfo;
                Data.ShellPage!.Navigate(
                    nameof(LocalAlbumDetailPage),
                    nameof(LocalArtistDetailPage),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    private void AlbumGridViewPlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalArtistAlbumInfo info })
        {
            ViewModel.AlbumGridViewPlayButton_Click(info);
        }
    }

    private void AlbumGridViewPlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalArtistAlbumInfo info })
        {
            ViewModel.AlbumGridViewPlayNextButton_Click(info);
        }
    }

    private void AlbumGridViewEditInfoButton_Click(object sender, RoutedEventArgs e) { }

    private void AlbumGridViewShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalArtistAlbumInfo info })
        {
            var localAlbumInfo = Data.MusicLibrary.Albums[info.Name];
            if (localAlbumInfo is not null)
            {
                var grid = (Grid)
                    ((ContentControl)AlbumGridView.ContainerFromItem(info)).ContentTemplateRoot;
                var border = (Border)grid.Children[1];
                ConnectedAnimationService
                    .GetForCurrentView()
                    .PrepareToAnimate("ForwardConnectedAnimation", border);
                Data.SelectedLocalAlbum = localAlbumInfo;
                Data.ShellPage!.Navigate(
                    nameof(LocalAlbumDetailPage),
                    nameof(LocalArtistDetailPage),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    private void AlbumGridViewSelectButton_Click(object sender, RoutedEventArgs e) { }

    /*private void AlbumGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Data.SelectedBriefAlbum is not null && sender is GridView gridView)
        {
            gridView.ScrollIntoView(Data.SelectedBriefAlbum, ScrollIntoViewAlignment.Leading);
            gridView.UpdateLayout();
            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("BackConnectedAnimation");
            if (animation is not null)
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
                await gridView.TryStartConnectedAnimationAsync(animation, Data.SelectedBriefAlbum, "CoverBorder");
            }
            gridView.Focus(FocusState.Programmatic);
        }
    }*/
}
