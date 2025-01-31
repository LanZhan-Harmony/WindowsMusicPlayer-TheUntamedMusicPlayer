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
using The_Untamed_Music_Player.ViewModels;
using Windows.Storage.Streams;
using Windows.UI;
using EF = CommunityToolkit.WinUI.Animations.Expressions.ExpressionFunctions;

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

    private const int ClampSize = 96;
    private const float BackgroundScaleFactor = 0.625f;
    private const float CoverScaleFactor = 0.5f;
    private const int ButtonPanelOffset = 64;
    private float BackgroundVisualHeight => (float)(Header.ActualHeight * 2.5);

    private CompositionPropertySet? _props;
    private CompositionPropertySet? _scrollerPropertySet;
    private Compositor? _compositor;
    private SpriteVisual? _backgroundVisual;
    private ScrollViewer? _scrollViewer;

    public AlbumDetailPage()
    {
        ViewModel = App.GetService<AlbumDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("ForwardConnectedAnimation");
        animation?.TryStart(CoverArt);
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        if (e.NavigationMode == NavigationMode.Back)
        {
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("BackConnectedAnimation", CoverArt);
        }
    }

    private async void AlbumDetailsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        // Retrieve the ScrollViewer that the GridView is using internally
        var scrollViewer = _scrollViewer = SongListView.FindDescendant<ScrollViewer>() ??
                                                    throw new Exception("Cannot find ScrollViewer in ListView");

        // Get the PropertySet that contains the scroll values from the ScrollViewer
        _scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
        _compositor = _scrollerPropertySet.Compositor;

        // Create a PropertySet that has values to be referenced in the ExpressionAnimations below
        _props = _compositor.CreatePropertySet();
        _props.InsertScalar("progress", 0);
        _props.InsertScalar("clampSize", ClampSize);
        _props.InsertScalar("backgroundScaleFactor", BackgroundScaleFactor);
        _props.InsertScalar("coverScaleFactor", CoverScaleFactor);
        _props.InsertScalar("buttonPanelOffset", ButtonPanelOffset);
        _props.InsertScalar("headerPadding", 12);

        // Get references to our property sets for use with ExpressionNodes
        var scrollingProperties = _scrollerPropertySet.GetSpecializedReference<ManipulationPropertySetReferenceNode>();

        CreateHeaderAnimation(_props, scrollingProperties.Translation.Y);

        if (ViewModel.SongList.Count == 0)
        {
            return;
        }
        var coverBytes = ViewModel.Album.GetCoverBytes();
        if (coverBytes.Length != 0)
        {
            await CreateImageBackgroundGradientVisual(scrollingProperties.Translation.Y, coverBytes);
        }
    }

    private void CreateHeaderAnimation(CompositionPropertySet propSet, ScalarNode scrollVerticalOffset)
    {
        var props = propSet.GetReference();
        var progressNode = props.GetScalarProperty("progress");
        var clampSizeNode = props.GetScalarProperty("clampSize");
        var backgroundScaleFactorNode = props.GetScalarProperty("backgroundScaleFactor");
        var coverScaleFactorNode = props.GetScalarProperty("coverScaleFactor");
        var buttonPanelOffsetNode = props.GetScalarProperty("buttonPanelOffset");
        var headerPaddingNode = props.GetScalarProperty("headerPadding");

        // Create and start an ExpressionAnimation to track scroll progress over the desired distance
        ExpressionNode progressAnimation = EF.Clamp(-scrollVerticalOffset / clampSizeNode, 0, 1);
        propSet.StartAnimation("progress", progressAnimation);

        // Get the backing visual for the background in the header so that its properties can be animated
        var backgroundVisual = ElementCompositionPreview.GetElementVisual(BackgroundAcrylic);

        // Create and start an ExpressionAnimation to scale and opacity fade in the backgound behind the header
        ExpressionNode backgroundScaleAnimation = EF.Lerp(1, backgroundScaleFactorNode, progressNode);
        ExpressionNode backgroundOpacityAnimation = progressNode;
        backgroundVisual.StartAnimation("Scale.Y", backgroundScaleAnimation);
        backgroundVisual.StartAnimation("Opacity", backgroundOpacityAnimation);

        // Get the backing visuals for the content container so that its properties can be animated
        var contentVisual = ElementCompositionPreview.GetElementVisual(ContentContainer);
        ElementCompositionPreview.SetIsTranslationEnabled(ContentContainer, true);

        // Create and start an ExpressionAnimation to move the content container with scroll position
        ExpressionNode contentTranslationAnimation = progressNode * headerPaddingNode;
        contentVisual.StartAnimation("Translation.Y", contentTranslationAnimation);

        // Get the backing visual for the cover art visual so that its properties can be animated
        var coverArtVisual = ElementCompositionPreview.GetElementVisual(CoverArt);
        ElementCompositionPreview.SetIsTranslationEnabled(CoverArt, true);

        // Create and start an ExpressionAnimation to scale and move the cover art with scroll position
        ExpressionNode coverArtScaleAnimation = EF.Lerp(1, coverScaleFactorNode, progressNode);
        ExpressionNode coverArtTranslationAnimation = progressNode * headerPaddingNode;
        coverArtVisual.StartAnimation("Scale.X", coverArtScaleAnimation);
        coverArtVisual.StartAnimation("Scale.Y", coverArtScaleAnimation);
        coverArtVisual.StartAnimation("Translation.X", coverArtTranslationAnimation);

        // Get the backing visual for the text panel so that its properties can be animated
        var textVisual = ElementCompositionPreview.GetElementVisual(TextPanel);
        ElementCompositionPreview.SetIsTranslationEnabled(TextPanel, true);

        // Create and start an ExpressionAnimation to move the text panel with scroll position
        ExpressionNode textTranslationAnimation = progressNode * (-clampSizeNode + headerPaddingNode);
        textVisual.StartAnimation("Translation.X", textTranslationAnimation);

        // Get backing visuals for the additional text blocks so that their properties can be animated
        var subtitleVisual = ElementCompositionPreview.GetElementVisual(SubtitleText);
        var captionVisual = ElementCompositionPreview.GetElementVisual(CaptionText);

        // Create an ExpressionAnimation that start opacity fade out animation with threshold for the additional text blocks
        var fadeThreshold = ExpressionValues.Constant.CreateConstantScalar("fadeThreshold", 0.6f);
        ExpressionNode textFadeAnimation = 1 - EF.Conditional(progressNode < fadeThreshold, progressNode / fadeThreshold, 1);

        // Start opacity fade out animation on the additional text block visuals
        subtitleVisual.StartAnimation("Opacity", textFadeAnimation);
        textFadeAnimation.SetScalarParameter("fadeThreshold", 0.2f);
        captionVisual.StartAnimation("Opacity", textFadeAnimation);

        // Get the backing visual for the button panel so that its properties can be animated
        var buttonVisual = ElementCompositionPreview.GetElementVisual(ButtonPanel);
        ElementCompositionPreview.SetIsTranslationEnabled(ButtonPanel, true);

        // Create and start an ExpressionAnimation to move the button panel with scroll position
        ExpressionNode buttonTranslationAnimation = progressNode * (-buttonPanelOffsetNode);
        buttonVisual.StartAnimation("Translation.Y", buttonTranslationAnimation);
    }

    private async Task CreateImageBackgroundGradientVisual(ScalarNode scrollVerticalOffset, byte[] imageBytes)
    {
        if (_compositor == null)
        {
            return;
        }
        var memoryStream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(memoryStream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(imageBytes);
            await writer.StoreAsync();
        }
        memoryStream.Seek(0);
        var imageSurface = LoadedImageSurface.StartLoadFromStream(memoryStream);
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

    private void AlbumArt_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _props?.InsertScalar("clampSize", ClampSize);
        _props?.InsertScalar("backgroundScaleFactor", BackgroundScaleFactor);
        _props?.InsertScalar("coverScaleFactor", CoverScaleFactor);
        _props?.InsertScalar("buttonPanelOffset", ButtonPanelOffset);
    }

    private void BackgroundHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_backgroundVisual == null)
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
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Visible;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Visible;
        }
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Collapsed;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Collapsed;
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.PlayButton_Click(sender, e);
    }

    public Brush GetAlternateBackgroundBrush(bool isDarkTheme)
    {
        if (isDarkTheme)
        {
            return new SolidColorBrush(Color.FromArgb(240, 48, 53, 57));
        }
        else
        {
            return new SolidColorBrush(Color.FromArgb(240, 253, 254, 254));
        }
    }
}
