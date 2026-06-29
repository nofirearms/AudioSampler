using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AudioSampler.Controls
{
    public class ListButton : Button
    {


        public static readonly StyledProperty<IList> ItemsProperty =
            AvaloniaProperty.Register<ListButton, IList>(nameof(Items), null);

        public IList Items
        {
            get => this.GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }



        public static readonly StyledProperty<object> SelectedItemProperty =
            AvaloniaProperty.Register<ListButton, object>(nameof(SelectedItem), null);

        public object SelectedItem 
        {
            get => this.GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }



        public ListButton()
        {
            ItemsProperty.Changed.AddClassHandler<ListButton>((x, e) => x.Update());
            SelectedItemProperty.Changed.AddClassHandler<ListButton>((x, e) => x.Update());

            Update();

            this.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            this.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;
        }


        protected override Type StyleKeyOverride => typeof(Button);

        public void Update()
        {
            Content = SelectedItem?.ToString() ?? "";
        }

        protected override void OnClick()
        {
            base.OnClick();

            if (Items == null || Items.Count == 0)
            {
                SelectedItem = null;
                return;
            }

            // Если ничего не выбрано или выбранный элемент не в списке - берём первый
            if (SelectedItem == null || !Items.Contains(SelectedItem))
            {
                SelectedItem = Items[0];
                return;
            }

            // Ищем текущий индекс
            int currentIndex = Items.IndexOf(SelectedItem);

            // Если не нашли (страховка) - берём первый
            if (currentIndex == -1)
            {
                SelectedItem = Items[0];
                return;
            }

            // Переходим к следующему (циклически)
            int nextIndex = (currentIndex + 1) % Items.Count;
            SelectedItem = Items[nextIndex];
        }
    }
}
