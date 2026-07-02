using AudioSampler.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Security.Cryptography;

namespace AudioSampler.Controls;

public partial class CircularProgressbar : UserControl
{



    public static readonly StyledProperty<double> MinValueProperty =
        AvaloniaProperty.Register<CircularProgressbar, double>(nameof(MinValue));

    public double MinValue
    {
        get => this.GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public static readonly StyledProperty<double> MaxValueProperty =
        AvaloniaProperty.Register<CircularProgressbar, double>(nameof(MaxValue));

    public double MaxValue
    {
        get => this.GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<CircularProgressbar, double>(nameof(Progress));

    public double Progress
    {
        get => this.GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }


    public CircularProgressbar()
    {
        InitializeComponent();

        ProgressProperty.Changed.AddClassHandler<CircularProgressbar>((x, e) => x.OnIsPlayingChanged(e));
    }

    private void OnIsPlayingChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if(e.NewValue is double progress)
        {
            progress = Math.Clamp((progress - MinValue) / (MaxValue - MinValue), 0, 1);

            ProgressArc.SweepAngle = progress * 360;

        }
        
    }



}