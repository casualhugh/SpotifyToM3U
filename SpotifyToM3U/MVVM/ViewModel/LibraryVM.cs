using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Requests;
using SpotifyToM3U.Core;
using SpotifyToM3U.MVVM.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SpotifyToM3U.MVVM.ViewModel
{
    internal partial class LibraryVM : ViewModelObject
    {

        public AudioFileCollection AudioFiles
        {
            get => _audioFiles; set => SetProperty(ref _audioFiles, value);
        }
        private AudioFileCollection _audioFiles = new();

        [ObservableProperty]
        private bool _isNext = false;

        [ObservableProperty]
        private bool _showProgressBar = false;

        [ObservableProperty]
        public float _progressValue = 0f;
        private long _processedFiles = 0;
        private long _totalFiles = 0;

        public BlockingCollection<string> RootPathes { get; set; } = new();

        public RequestHandler RequestHandler { get; } = new()
        {
            StaticDegreeOfParallelism = Environment.ProcessorCount
        };

        public event EventHandler? AudioFilesModifified;

        public LibraryVM(INavigationService navigation) : base(navigation) => BindingOperations.EnableCollectionSynchronization(AudioFiles, new object());

        [RelayCommand]
        private void AddFolder()
        {
            View.Windows.AddFolderWindow addFolderWindow = App.Current.ServiceProvider.GetRequiredService<View.Windows.AddFolderWindow>();
            AddFolderVM vm = ((AddFolderVM)addFolderWindow.DataContext);

            addFolderWindow.ShowDialog();
            if (vm.Result)
            {
                char[] sep = new char[3] { ',', ';', ' ' };
                string[] exts = vm.Extensions.Split(sep, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < exts.Length; ++i)
                    exts[i] = exts[i].ToLower();
                IsNext = false;
                int count = AudioFiles.Count;
                string d1 = vm.Path;
                bool d2 = vm.ScanSubdirectories;
                RootPathes.Add(d1);
                ShowProgressBar = true;
                Task t = Task.Run(async () => await LoadFolderFiles(exts, d1, d2));
                t.ContinueWith(at =>
                {
                    IsNext = AudioFiles.Count > 0;
                    MessageBox.Show(AudioFiles.Count - count + " files added.", "Info", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    ShowProgressBar = false;
                    _processedFiles = 0;
                    ProgressValue = 0;
                    AudioFilesModifified?.Invoke(this, null!);
                });
            }

        }

        private async Task LoadFolderFiles(string[] exts, string root, bool recursive)
        {
            Stopwatch w = Stopwatch.StartNew();

            _totalFiles = GetFiles(new DirectoryInfo(root), "\\." + string.Join("|\\.", exts), recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Count();
            if (_totalFiles <= 0)
                return;
            RequestContainer<OwnRequest> container = new();
            container.Add(new OwnRequest((t) => { ScanFolder(root, recursive, exts, container); OnPropertyChanged(nameof(ProgressValue)); return Task.FromResult(true); }, new()
            {
                Handler = RequestHandler
            }));
            await Task.Delay(100);
            do
            {
                await container.Task;
                await Task.Delay(200);
            } while (container.State == Requests.Options.RequestState.Running);
            w.Stop();
            Debug.WriteLine(w.Elapsed.TotalSeconds);
        }

        public static IEnumerable<FileInfo> GetFiles(DirectoryInfo directory,
                    string searchPatternExpression = "",
                    SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Regex reSearchPattern = new(searchPatternExpression, RegexOptions.IgnoreCase);
            return directory.EnumerateFiles("*", searchOption)
                            .Where(file =>
                                     reSearchPattern.IsMatch(file.Extension));
        }

        private void ScanFolder(string folder, bool recurse, string[] extensions, RequestContainer<OwnRequest> container)
        {
            DirectoryInfo di = new(folder);
            IEnumerable<FileInfo> files = GetFiles(di, "\\." + string.Join("|\\.", extensions));
            foreach (FileInfo fi in files)
            {
                if (!AudioFiles.ContainsFile(fi.FullName)) AudioFiles.Add(new AudioFile(fi.FullName));
                Interlocked.Increment(ref _processedFiles);
            }

            if (!recurse) return;

            foreach (DirectoryInfo fi in di.EnumerateDirectories())
            {
                OwnRequest req = new((t) =>
                {
                    ScanFolder(fi.FullName, recurse, extensions, container);
                    ProgressValue = (float)Interlocked.Read(ref _processedFiles) / Interlocked.Read(ref _totalFiles); return Task.FromResult(true);
                }, new()
                {
                    Handler = RequestHandler
                });
                container.Add(req);
            }
        }

        [RelayCommand]
        private void Clear()
        {
            AudioFiles.Clear();
            IsNext = false;

            while (RootPathes.TryTake(out _)) { }
            AudioFilesModifified?.Invoke(this, null!);
        }

        [RelayCommand]
        private void AddFile()
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new();
            fileDialog.Multiselect = true;
            fileDialog.Title = "Select Audio Files";
            fileDialog.Filter = "Audio Files (*.MP3, *.FLAC, *.WMA, *.WAV)|*.MP3;*.FLAC;*.aac;*m4a;*.WMA;*.WAV|All Files (*.*)|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                _totalFiles = fileDialog.FileNames.Length;
                IsNext = false;
                Task.Run(async () =>
                {
                    RequestContainer<OwnRequest> container = new();
                    foreach (string? path in fileDialog.FileNames)
                        container.Add(new OwnRequest((t) => { if (!AudioFiles.ContainsFile(path)) AudioFiles.Add(new AudioFile(path)); Interlocked.Increment(ref _processedFiles); return Task.FromResult(true); }, new()
                        {
                            Handler = RequestHandler,
                            RequestCompleated = (s, e) =>
                            {
                                RootPathes.Add(path);
                                OnPropertyChanged(nameof(ProgressValue));
                            }
                        }));
                    await Task.Delay(400);
                    do
                    {
                        await container.Task;
                        await Task.Delay(400);
                    } while (container.State == Requests.Options.RequestState.Running);

                    IsNext = AudioFiles.Count > 0;
                    AudioFilesModifified?.Invoke(this, null!);
                });


            }
        }

        [RelayCommand]
        private void Spotify() => Navigation.NavigateTo<SpotifyVM>();

    }
}
