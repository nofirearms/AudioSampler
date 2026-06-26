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
using System.Numerics;
using System.Text;

namespace AudioSampler.Behaviors
{
    public class ModalAnimationBehavior : Behavior<Control>
    {

        protected override void OnAttached()
        {
            base.OnAttached();
            // Вместо AttachedToVisualTree используем Loaded, 
            // к этому моменту Avalonia на Android точно знает размеры элемента
            AssociatedObject.AttachedToVisualTree += OnAttachedToVisualTree2;

        }

        private void OnAttachedToVisualTree2(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (AssociatedObject == null) return;

            // 1. Получаем низкоуровневый визуал элемента из слоя композиции
            var compositionVisual = ElementComposition.GetElementVisual(AssociatedObject);
            if (compositionVisual == null) return;

            var compositor = compositionVisual.Compositor;

            // Настраиваем точку, относительно которой будет масштабироваться окно (центр элемента)
            compositionVisual.CenterPoint = new Vector3(
                (float)(AssociatedObject.Bounds.Width / 2),
                (float)(AssociatedObject.Bounds.Height / 2),
                0);

            // Изначально делаем элемент невидимым и уменьшенным на уровне композитора
            AssociatedObject.Opacity = 0.2;
            compositionVisual.Opacity = 0.2f;
            compositionVisual.Offset = new Vector3D(100, 0, 0);
            //compositionVisual.Scale = new Vector3(0.85f, 0.85f, 1f);


            // 3. Создаем низкоуровневую анимацию прозрачности (Opacity)
            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.Target = "Opacity";
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(200);
            opacityAnimation.InsertKeyFrame(1f, 1f);

            // 4. Запускаем обе анимации на GPU
            compositionVisual.StartAnimation("Opacity", opacityAnimation);
        }

        private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (AssociatedObject is null)
                return;

            var transform = new TranslateTransform();
            AssociatedObject.RenderTransform = transform;
            AssociatedObject.Opacity = 0;

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(100),
                FillMode = FillMode.Forward,
                Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0d),
                        new Setter(TranslateTransform.YProperty, 100d)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 1d),
                        new Setter(TranslateTransform.YProperty, 0d)
                    }
                }
            }
            };

            await animation.RunAsync(AssociatedObject);
        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.AttachedToVisualTree -= OnAttachedToVisualTree;
        }

    }
}

