using Microsoft.Extensions.DependencyInjection;
using SpotifyToM3U.MVVM.ViewModel;
using System;
using System.Windows;
using System.Windows.Input;

namespace SpotifyToM3U.MVVM.View.Windows
{
    /// <summary>
    /// Interaktionslogik für AddFolderWindow.xaml
    /// </summary>
    public partial class AddFolderWindow : Window
    {
        private AddFolderVM AddFolderVM =
            App.Current.ServiceProvider.GetRequiredService<AddFolderVM>();

        public AddFolderWindow()
        {
            InitializeComponent();
            AddFolderVM.Close += (_, _) => Hide();
            if (Application.Current.MainWindow != null && Application.Current.MainWindow != this)
                Owner = Application.Current.MainWindow;
        }

        private void CloseOnEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                App.Current.ServiceProvider.GetRequiredService<AddFolderVM>().Result = false;
                Hide();
            }
        }

        private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            if (Owner != null)
            {
                WindowState = WindowState.Normal;
                Activate();
                Focus();
            }
        }
    }
}