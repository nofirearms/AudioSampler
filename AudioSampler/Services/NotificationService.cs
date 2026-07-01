using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Xaml.Interactions.Custom;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Collections.Generic;
using System.Text;
using Tmds.DBus.Protocol;
using static System.Net.Mime.MediaTypeNames;

namespace AudioSampler.Services
{
    public class NotificationService
    {
        private readonly FileService _fileService;
        private readonly WindowNotificationManager _manager;

        public NotificationService(FileService fileService)
        {
            _fileService = fileService;

            var topLevel = _fileService.GetTopLevel();

            _manager = new WindowNotificationManager(topLevel)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 3,
            };

            
        }

        public void Show(MaterialIconKind materialIcon, string messageNormal, string messageSmall, NotificationType type)
        {
            // 1. Создаем Material-иконку из пакета
            var icon = new MaterialIcon
            {
                Kind = materialIcon,
                Width = 24,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(5, 0, 10, 0),
                Foreground = type switch
                {
                    NotificationType.Success => Brushes.Green,   // Ну или LightGreen, смотря какая тема
                    NotificationType.Error => Brushes.Crimson,
                    NotificationType.Warning => Brushes.Orange,
                    NotificationType.Information => Brushes.DodgerBlue,
                    _ => Brushes.White        // Дефолтный цвет, если тип левый
                }
            };

            // 2. Создаем текст сообщения
            var textBlock = new TextBlock
            {
                Text = messageNormal,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            };

            var textBlock2 = new TextBlock
            {
                Text = messageSmall,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 10,
                Foreground = new SolidColorBrush(Colors.Gray)
            };

            var textPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Children = { textBlock, textBlock2 }
            };

            // 3. Пакуем всё в горизонтальную StackPanel
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { icon, textPanel },
            };

            _manager.Show(panel, NotificationType.Success, expiration: TimeSpan.FromSeconds(2)); 
        }

    }
}
