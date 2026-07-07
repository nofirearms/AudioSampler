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



        public static readonly StyledProperty<IBrush> BarTopBrushProperty =
            AvaloniaProperty.Register<AudioGraph, IBrush>(nameof(BarTopBrush), Brushes.Orange);

        public IBrush BarTopBrush
        {
            get => this.GetValue(BarTopBrushProperty);
            set => SetValue(BarTopBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> BarBottomBrushProperty =
            AvaloniaProperty.Register<AudioGraph, IBrush>(nameof(BarBottomBrush), Brushes.Orange);

        public IBrush BarBottomBrush
        {
            get => this.GetValue(BarBottomBrushProperty);
            set => SetValue(BarBottomBrushProperty, value);
        }


        public static readonly StyledProperty<float> BarSpacingProperty =
            AvaloniaProperty.Register<AudioGraph, float>(nameof(BarSpacing), 1f);

        public float BarSpacing
        {
            get => this.GetValue(BarSpacingProperty);
            set => SetValue(BarSpacingProperty, value);
        }

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

            for (int i = 0; i < count; i++)
            {
                double amplitude = points[i];

                double barHeight = Math.Max(height * amplitude, 1) / 2;

                double x = i * (barWidth + BarSpacing);

                double middle_y = height / 2;

                double top_y = middle_y - barHeight;
                double bottom_y = middle_y;

                var top_rect = new Rect(Math.Round(x), Math.Round(top_y), Math.Round(barWidth), Math.Round(barHeight));
                var bottom_rect = new Rect(Math.Round(x), Math.Round(bottom_y), Math.Round(barWidth), Math.Round(barHeight));

                context.DrawRectangle(BarTopBrush, null, top_rect);
                context.DrawRectangle(BarBottomBrush, null, bottom_rect);

            }
        }

    }
}
