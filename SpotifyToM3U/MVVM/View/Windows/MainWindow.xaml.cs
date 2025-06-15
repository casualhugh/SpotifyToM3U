using SpotifyToM3U.Core;
using System.Windows;

namespace SpotifyToM3U.MVVM.View.Windows
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WindowMaximizationHelper.EnableProperMaximization(this);
        }
    }
}