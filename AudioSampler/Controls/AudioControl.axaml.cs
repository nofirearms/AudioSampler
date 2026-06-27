using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;

namespace AudioSampler.Controls;

public partial class AudioControl : UserControl
{
    // Регистрируем свойства начала и конца (от 0.0 до 1.0)
    public static readonly StyledProperty<double> StartProperty =
        AvaloniaProperty.Register<AudioControl, double>(nameof(Start), defaultValue: 0.0);

    public static readonly StyledProperty<double> EndProperty =
        AvaloniaProperty.Register<AudioControl, double>(nameof(End), defaultValue: 1.0);

    public double Start
    {
        get => GetValue(StartProperty);
        set => SetValue(StartProperty, value);
    }

    public double End
    {
        get => GetValue(EndProperty);
        set => SetValue(EndProperty, value);
    }

    private Grid? _activeThumb;

    public AudioControl()
    {
        InitializeComponent();

        // Следим за изменениями свойств, чтобы сдвинуть флажки при загрузке дефолтных значений
        StartProperty.Changed.AddClassHandler<AudioControl>((x, e) => x.UpdateGridColumns());
        EndProperty.Changed.AddClassHandler<AudioControl>((x, e) => x.UpdateGridColumns());

        // Также обновляем при изменении размера самого контрола
        this.SizeChanged += (s, e) => UpdateGridColumns();
    }

    private void UpdateGridColumns()
    {
        if (SliderGrid == null) return;

        double start = Math.Clamp(Start, 0.0, 1.0);
        double end = Math.Clamp(End, start, 1.0); // Конец не может быть левее начала

        // Настраиваем пропорции колонок Grid
        SliderGrid.ColumnDefinitions[0].Width = new GridLength(start, GridUnitType.Star);
        SliderGrid.ColumnDefinitions[1].Width = new GridLength(end - start, GridUnitType.Star);
        SliderGrid.ColumnDefinitions[2].Width = new GridLength(1.0 - end, GridUnitType.Star);
    }

    private void Thumb_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _activeThumb = sender as Grid; 
        e.Pointer.Capture(MainGrid); // Захватываем мышь, чтобы не терять флажок при быстром движении
    }

    private void MainGrid_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_activeThumb == null || MainGrid.Bounds.Width == 0) return;

        // Получаем текущую координату мыши относительно всего контрола
        var currentPos = e.GetPosition(MainGrid);
        double percentage = currentPos.X / MainGrid.Bounds.Width;
        percentage = Math.Clamp(percentage, 0.0, 1.0);

        if (_activeThumb.Name == "StartTarget")
        {
            // Ограничиваем, чтобы старт не ушел правее конца
            Start = Math.Min(percentage, End);
        }
        else if (_activeThumb.Name == "EndTarget")
        {
            // Ограничиваем, чтобы конец не ушел левее старта
            End = Math.Max(percentage, Start);
        }
    }

    private void MainGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_activeThumb != null)
        {
            _activeThumb = null;
            e.Pointer.Capture(null); // Освобождаем мышь
        }
    }




}