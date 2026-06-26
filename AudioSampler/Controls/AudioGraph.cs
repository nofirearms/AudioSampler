using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Controls
{
    public class AudioGraph : Control
    {


        public static readonly StyledProperty<IReadOnlyList<float>> PointsProperty =
            AvaloniaProperty.Register<AudioGraph, IReadOnlyList<float>>(nameof(Points), null);

        public IReadOnlyList<float> Points
        {
            get => this.GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        // Кисти для кастомизации цветов
        public IBrush BarBrush { get; set; } = Brushes.Orange;
        public float BarSpacing { get; set; } = 2f; // Расстояние между столбиками в пикселях

        public AudioGraph()
        {
            PointsProperty.Changed.AddClassHandler<AudioGraph>((x, e) => x.InvalidateVisual());
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var points = Points;
            if (points == null || points.Count == 0) return;

            double width = Bounds.Width;
            double height = Bounds.Height;
            int count = points.Count;

            // Вычисляем ширину одного столбика с учетом отступов
            double totalSpacing = BarSpacing * (count - 1);
            double barWidth = (width - totalSpacing) / count;

            if (barWidth <= 0) return;

            // Рисуем каждый столбик центрированным по вертикали (как в SoundCloud)
            for (int i = 0; i < count; i++)
            {
                double amplitude = points[i];

                // Вычисляем высоту столбика (минимум 2 пикселя, чтобы не пропадал совсем)
                double barHeight = Math.Max(height * amplitude, 2);

                double x = i * (barWidth + BarSpacing);
                double y = (height - barHeight) / 2; // Центрирование по вертикали

                // Округляем для четкости на мобилках (anti-aliasing)
                var rect = new Rect(Math.Round(x), Math.Round(y), Math.Round(barWidth), Math.Round(barHeight));

                // Рисуем скругленный столбик (CornerRadius)
                context.DrawRectangle(BarBrush, null, rect, 2, 2);
            }
        }
    }
}
