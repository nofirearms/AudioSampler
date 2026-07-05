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
            UseLayoutRounding = false;
        }


        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var points = Points;
            if (points == null || points.Count == 0) return;

            double width = Bounds.Width;
            double height = Bounds.Height;
            int count = points.Count;

            // 1. Вычисляем идеальную дробную ширину
            double totalSpacing = BarSpacing * (count - 1);
            double rawBarWidth = (width - totalSpacing) / count;
            if (rawBarWidth <= 0) return;

            // 2. ЖЕСТКО округляем ширину и шаг до пикселей один раз.
            // Теперь ВСЕ столбики и ВСЕ зазоры будут гарантированно одинаковыми на экране.
            double finalBarWidth = Math.Max(Math.Round(rawBarWidth), 1);
            double finalSpacing = Math.Round(BarSpacing);

            // 3. Считаем, сколько пикселей займет вся наша железная сетка
            double totalWaveformWidth = (count * finalBarWidth) + ((count - 1) * finalSpacing);

            // 4. Находим смещение, чтобы отцентрировать волну (если из-за округлений остался пустой край)
            double offsetX = Math.Round((width - totalWaveformWidth) / 2);
            if (offsetX < 0) offsetX = 0;

            double middle_y = Math.Round(height / 2);

            for (int i = 0; i < count; i++)
            {
                double amplitude = points[i];

                // 5. Шагаем строго на одинаковое пиксельное расстояние
                double x = offsetX + i * (finalBarWidth + finalSpacing);

                // Расчет высоты (минимум 2 пикселя, чтобы половинки были хотя бы по 1 пикселю)
                double barHeight = Math.Max(height * amplitude, 2) / 2;

                double roundedTopY = Math.Round(middle_y - barHeight);
                double roundedFinalHeight = Math.Max(middle_y - roundedTopY, 1);

                // Формируем ректы. Координаты X и Width теперь идеальные целые числа.
                var top_rect = new Rect(x, roundedTopY, finalBarWidth, roundedFinalHeight);
                var bottom_rect = new Rect(x, middle_y, finalBarWidth, roundedFinalHeight);

                context.DrawRectangle(BarTopBrush, null, top_rect);
                context.DrawRectangle(BarBottomBrush, null, bottom_rect);
            }
        }

    }
}
