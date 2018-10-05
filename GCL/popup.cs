using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GCL
{
    public class popup : Window
    {
        public popup(UIElement parent = null, String title = "popup")
        {
            Title = title;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;

            if (parent != null)
            {
                parent.IsEnabled = false;
                Closed += (s, ev) => { parent.IsEnabled = true; };
            }
        }
    }

    public class textinput_popup : popup
    {
        public textinput_popup(UIElement parent = null, String title = "Text input popup", String default_text_value = "")
            : base(parent, title)
        {
            SizeToContent = SizeToContent.Height;
            Width = 300;

            var stackPanel = new System.Windows.Controls.StackPanel { Name = "content", Orientation = Orientation.Vertical };
            stackPanel.Children.Add(new TextBox { Name = "input", Text = default_text_value });
            var button_validate = new Button { Content = "Validate" };
            
            button_validate.Click += (s, ev) => { canceled = false; this.Close(); };
            stackPanel.Children.Add(button_validate);
            Content = stackPanel;

            var input_text_box = (stackPanel.Children[0] as TextBox);
            input_text_box.KeyDown += (sender, ev) =>
            {
                if (ev.Key == Key.Enter)
                {
                    canceled = false;
                    this.Close();
                }
            };
            if (input_text_box.Text.Length != 0)
            {
                input_text_box.SelectionStart = input_text_box.Text.Length;
                input_text_box.SelectionLength = 0;
            }
            input_text_box.Focus();

            Closed += (s, ev) =>
            {
                if (canceled)
                    return;

                var content = Content as StackPanel;
                var input = content.Children[0] as TextBox;

                text_input = input.Text;
            };
        }

        public bool canceled = true;
        public String text_input = "";
    }
}
