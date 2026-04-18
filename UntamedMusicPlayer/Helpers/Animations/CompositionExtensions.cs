using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace UntamedMusicPlayer.Helpers.Animations;

public static class CompositionExtensions
{
    extension(UIElement element)
    {
        public UIElement EnableTranslation(bool enable)
        {
            ElementCompositionPreview.SetIsTranslationEnabled(element, enable);
            return element;
        }

        public UIElement SetTranslation(Vector3 value)
        {
            element
                .GetElementVisual()!
                .Properties.InsertVector3(CompositionFactory.TRANSLATION, value);
            return element;
        }
    }

    extension(Visual visual)
    {
        public Visual WithStandardTranslation()
        {
            return CompositionFactory.EnableStandardTranslation(visual);
        }
    }

    extension(Compositor compositor)
    {
        /// <summary>
        /// Returns a cached instance of the LinearEase function
        /// </summary>
        /// <param name="compositor"></param>
        /// <returns></returns>
        public CompositionEasingFunction GetLinearEase()
        {
            return compositor.GetCached(
                "LINEAREASE",
                () => compositor.CreateLinearEasingFunction()
            );
        }
    }

    //------------------------------------------------------
    //
    // Expression Animation : SIZE LINKING
    //
    //------------------------------------------------------

    #region Linked Size Expression

    /*
     * An expression that matches the size of a visual to another.
     * Useful for keeping shadows etc. in size sync with their target.
     */

    private static string LINKED_SIZE_EXPRESSION { get; } =
        $"{nameof(Visual)}.{nameof(Visual.Size)}";

    extension(FrameworkElement sourceElement)
    {
        public ExpressionAnimation CreateLinkedSizeExpression()
        {
            return CreateLinkedSizeExpression(sourceElement.GetElementVisual()!);
        }
    }

    public static ExpressionAnimation CreateLinkedSizeExpression(Visual sourceVisual)
    {
        return sourceVisual
            .CreateExpressionAnimation(nameof(Visual.Size))
            .SetParameter(nameof(Visual), sourceVisual)
            .SetExpression(LINKED_SIZE_EXPRESSION);
    }

    extension<T>(T targetVisual)
        where T : Visual
    {
        /// <summary>
        /// Starts an Expression Animation that links the size of <paramref name="sourceVisual"/> to the <paramref name="targetVisual"/>
        /// </summary>
        /// <param name="targetVisual">Element whose size you want to automatically change</param>
        /// <param name="sourceVisual"></param>
        /// <returns></returns>
        public T LinkSize(Visual sourceVisual)
        {
            targetVisual.StartAnimation(CreateLinkedSizeExpression(sourceVisual));
            return targetVisual;
        }

        /// <summary>
        /// Starts an Expression Animation that links the size of <paramref name="element"/> to the <paramref name="targetVisual"/>
        /// </summary>
        /// <param name="targetVisual">Element whose size you want to automatically change</param>
        /// <param name="element">Element whose size will change <paramref name="targetVisual"/>s size</param>
        /// <returns></returns>
        public T LinkSize(FrameworkElement element)
        {
            targetVisual.StartAnimation(CreateLinkedSizeExpression(element.GetElementVisual()!));
            return targetVisual;
        }
    }

    extension<T>(T targetVisual)
        where T : CompositionGeometry
    {
        public T LinkShapeSize(Visual sourceVisual)
        {
            targetVisual.StartAnimation(CreateLinkedSizeExpression(sourceVisual));
            return targetVisual;
        }
    }

    #endregion
}
