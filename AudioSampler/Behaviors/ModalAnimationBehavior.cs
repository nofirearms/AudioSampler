using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;
using Avalonia.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace AudioSampler.Behaviors
{
    public class ModalAnimationBehavior : Behavior<Control>
    {

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.AttachedToVisualTree += OnAttachedToVisualTree2; ;

        }

        private void OnAttachedToVisualTree2(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (AssociatedObject == null) return;

            var compositionVisual = ElementComposition.GetElementVisual(AssociatedObject);
            if (compositionVisual == null) return;

            var compositor = compositionVisual.Compositor;


            var animation = compositor.CreateVector3DKeyFrameAnimation();

            animation.InsertKeyFrame(0f, new Vector3D(0, 100, 0));
            animation.InsertKeyFrame(1f, new Vector3D(0, 0, 0), new CubicEaseInOut());
            animation.Duration = TimeSpan.FromMilliseconds(100);



            AssociatedObject.Opacity = 0.2;
            compositionVisual.Opacity = 0.2f;

            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.Target = "Opacity";
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(200);
            opacityAnimation.InsertKeyFrame(1f, 1f, new CubicEaseInOut());

            compositionVisual.StartAnimation("Translation", animation);
            compositionVisual.StartAnimation("Opacity", opacityAnimation);
        }



        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.AttachedToVisualTree -= OnAttachedToVisualTree2;
        }

    }
}

