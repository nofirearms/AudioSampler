using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace AudioSampler.Controls;

public partial class Playhead : UserControl
{


    public static readonly StyledProperty<double> PlaybackPositionPercentProperty =
        AvaloniaProperty.Register<Playhead, double>(nameof(PlaybackPositionPercent));


    public double PlaybackPositionPercent
    {
        get => this.GetValue(PlaybackPositionPercentProperty);
        set => SetValue(PlaybackPositionPercentProperty, value);
    }



    public Playhead()
    {
        InitializeComponent();

        PlaybackPositionPercentProperty.Changed.AddClassHandler<Playhead>((sender, args) => sender.Update());
}

    private void Update()
    {
        var position = this.Bounds.Width * PlaybackPositionPercent;

        Canvas.SetLeft(PlaybackBar, position);
    }
}