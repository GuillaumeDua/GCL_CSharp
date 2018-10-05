using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Threading;

namespace GCL.Task
{
    public class ui
    {
        static public func_out wait_for_process_popup<func_out>(string action_name, Func<func_out> func)
        {
            var process_popup = new GCL.popup(null, action_name);
            var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
            stackPanel.Children.Add(new Label { Content = String.Format("Please wait until {0} is complete", action_name) });
            stackPanel.Children.Add(new ProgressBar { IsIndeterminate = true });
            process_popup.Content = stackPanel;

            // todo : add progress bar + use on bills loading

            func_out return_value;
            process_popup.Show();
            return_value = func.Invoke();
            process_popup.Close();
            return return_value;
        }
    }
}
