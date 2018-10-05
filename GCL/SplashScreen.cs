using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GCL
{
    public class SplashScreen
    {
        public SplashScreen(string imgPath)
        {
            var imgSource = new BitmapImage(new Uri(imgPath));
            var img = new Image();
            img.Source = imgSource;

            var grid = new Grid();
            grid.Width = img.Width + 10;
            grid.Width = img.Height + 10;
            grid.Background.Opacity = .0;
            grid.Children.Add(img);

            var textBlock = new TextBlock();
            textBlock.Width = img.Width;

            grid.Children.Add(textBlock);

            window = new Window();
            window.Content = grid;
            window.ResizeMode = ResizeMode.NoResize;
            window.Focusable = false;
            window.Opacity = .0;
        }

        public void Show()
        {
            window.Show();
        }

        // todo : Events => TextBlock update

        private Window window;
    }
}
