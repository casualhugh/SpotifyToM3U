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
        }

        private void CloseOnEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                App.Current.ServiceProvider.GetRequiredService<AddFolderVM>().Result = false;
                Hide();
            }
        }
    }
}
