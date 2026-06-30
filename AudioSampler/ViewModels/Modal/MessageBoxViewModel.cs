using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.ViewModels.Modal
{
    public partial class MessageBoxViewModel : BaseModalViewModel<string>
    {
        public string[] Buttons { get; set; }
        public string Message { get; set; }

        public MessageBoxViewModel(string header, string message, string[] buttons)
        {
            Buttons = buttons;
            Message = message;
            Header = header;

            ModalHorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        }

        [RelayCommand]
        public void ButtonPressed(string button)
        {
            Close(true, button, button);
        }
    }
}
