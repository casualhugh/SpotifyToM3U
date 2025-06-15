using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotifyToM3U.Core;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace SpotifyToM3U.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for the Add Folder dialog
    /// </summary>
    internal partial class AddFolderVM : ViewModelObject
    {
        [ObservableProperty]
        private string _path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

        [ObservableProperty]
        private string _extensions = "mp3,flac,wma,wav,aac,m4a";

        [ObservableProperty]
        private bool _scanSubdirectories = true;

        [ObservableProperty]
        private bool _result = false;

        public event EventHandler? Close;

        public AddFolderVM(INavigationService navigation) : base(navigation) { }

        [RelayCommand]
        private void Browse()
        {
            FolderBrowserDialog folderBrowser = new()
            {
                ShowNewFolderButton = false
            };
            if (folderBrowser.ShowDialog() == DialogResult.OK)
                Path = folderBrowser.SelectedPath;
        }

        [RelayCommand]
        private void Scan()
        {
            string folder = Path.Trim();
            if (!Directory.Exists(folder))
                System.Windows.MessageBox.Show("Folder '" + folder + "' doesn't exist!", "Invalid folder", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            else
            {
                Result = true;
                Close?.Invoke(this, null!);
            }
        }

        [RelayCommand]
        private void Quit()
        {
            Result = false;
            Close?.Invoke(this, null!);
        }
    }
}