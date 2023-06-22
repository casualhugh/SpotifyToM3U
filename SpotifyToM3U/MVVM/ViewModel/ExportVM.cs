using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SpotifyToM3U.Core;
using SpotifyToM3U.MVVM.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpotifyToM3U.MVVM.ViewModel
{
    internal partial class ExportVM : ViewModelObject
    {
        private SpotifyVM _spotifyVM;
        private LibraryVM _libraryVM;

        [ObservableProperty]
        private string _exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Playlists\\");
        private string _exportPathOld = string.Empty;
        private string _exportRoot = string.Empty;

        [ObservableProperty]
        private bool _exportAsRelativ = false;

        [ObservableProperty]
        private bool _exportIsVisible = false;

        [ObservableProperty]
        private bool _canAsRelativ = true;

        public ExportVM(INavigationService navigation) : base(navigation)
        {
            _spotifyVM = App.Current.ServiceProvider.GetRequiredService<SpotifyVM>();
            _libraryVM = App.Current.ServiceProvider.GetRequiredService<LibraryVM>();
            Navigation.PropertyChanged += Navigation_PropertyChanged;
            _libraryVM.AudioFilesModifified += LibraryVM_AudioFilesModifified;
            PropertyChanged += OnTextBoxPropertyChanged;
        }

        private void Navigation_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Navigation.CurrentView))
            {
                ExportIsVisible = false;
            }
        }

        public void LibraryVM_AudioFilesModifified(object? sender, EventArgs e)
        {
            if (_libraryVM.RootPathes.Count == 0)
                return;
            Task.Run(() =>
            {
                string[][] collection = _libraryVM.RootPathes.ToList().Select(x => x.Split("\\")).ToArray();
                List<string> rootPath = new();

                for (int j = 0; j < collection[0].Length; j++)
                    if (collection.All(x => x[j] == collection[0][j]))
                        rootPath.Add(collection[0][j]);
                string path = string.Join("\\", rootPath);
                if (IOManager.TryGetFullPath(path, out path))
                {
                    _exportRoot = path + "\\";
                    CanAsRelativ = true;
                }
                else CanAsRelativ = false;
            });
        }

        private void OnTextBoxPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ExportPath))
            {
                ExportIsVisible = false;
                if (IOManager.TryGetFullPath(ExportPath, out string path))
                    ExportPath = path;
                else
                    MessageBox.Show("Invalid Path", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        [RelayCommand]
        private void Relativ()
        {
            if (ExportAsRelativ)
            {
                _exportPathOld = ExportPath;
                ExportPath = _exportRoot;
            }
            else
            {
                ExportPath = _exportPathOld;
            }
        }

        [RelayCommand]
        private void Export()
        {
            if (ExportPath.Length < 2)
                Directory.CreateDirectory(ExportPath);
            Directory.CreateDirectory(ExportPath);
            File.Create(Path.Combine(ExportPath, IOManager.RemoveInvalidFileNameChars(_spotifyVM.PlaylistName) + ".m3u8")).Close();

            List<string> files = new();
            files.Add("#EXTM3U");
            files.Add("#" + _spotifyVM.PlaylistName + ".m3u8");
            for (int i = 0; i < _spotifyVM.PlaylistTracks.Count; i++)
            {
                if (_spotifyVM.PlaylistTracks[i].IsLocal)
                    files.Add(ExportAsRelativ ? _spotifyVM.PlaylistTracks[i].Path.Remove(0, _exportRoot.Length) : _spotifyVM.PlaylistTracks[i].Path);
            }
            File.WriteAllLines(Path.Combine(ExportPath, IOManager.RemoveInvalidFileNameChars(_spotifyVM.PlaylistName) + ".m3u8"), files);

            ExportIsVisible = true;
        }
        [RelayCommand]
        private void Browse()
        {
            FolderBrowserDialog folderBrowser = new()
            {
                ShowNewFolderButton = false
            };
            if (folderBrowser.ShowDialog() == DialogResult.OK)
                if (IOManager.TryGetFullPath(folderBrowser.SelectedPath, out string path))
                    ExportPath = path;
                else
                    MessageBox.Show("Invalid Path", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }



    }
}
