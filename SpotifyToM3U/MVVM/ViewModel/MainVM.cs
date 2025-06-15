using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SpotifyToM3U.Core;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Shell;

namespace SpotifyToM3U.MVVM.ViewModel
{
    internal partial class MainVM : ViewModelObject
    {
        #region Fields

        private readonly LibraryVM _libraryVM;
        private readonly SpotifyVM _spotifyVM;
        private readonly ExportVM _exportVM;

        #endregion

        #region Properties

        [ObservableProperty]
        private bool _enableSpotify = false;

        [ObservableProperty]
        private bool _enableExport = false;

        [ObservableProperty]
        private float _progressValue = 0f;

        [ObservableProperty]
        private TaskbarItemProgressState _taskbarState = TaskbarItemProgressState.None;

        [ObservableProperty]
        private string _currentStatusText = "Ready";

        [ObservableProperty]
        private bool _isProcessing = false;

        public string CurrentName => Navigation.CurrentView?.GetType().Name ?? "LibraryVM";

        #endregion

        #region Constructor

        public MainVM(INavigationService navigation) : base(navigation)
        {
            // Get view models from DI container
            _libraryVM = App.Current.ServiceProvider.GetRequiredService<LibraryVM>();
            _spotifyVM = App.Current.ServiceProvider.GetRequiredService<SpotifyVM>();
            _exportVM = App.Current.ServiceProvider.GetRequiredService<ExportVM>();

            // Wire up event handlers
            Navigation.PropertyChanged += NavigationService_PropertyChanged;
            _libraryVM.PropertyChanged += LibraryVM_PropertyChanged;
            _spotifyVM.PropertyChanged += SpotifyVM_PropertyChanged;

            // Initialize state
            UpdateCurrentStatusText();
        }

        #endregion

        #region Commands

        [RelayCommand]
        private async Task ChangeViewAsync(string viewName)
        {
            try
            {
                Type? targetType = Type.GetType($"SpotifyToM3U.MVVM.ViewModel.{viewName}");
                if (targetType != null)
                {
                    // Add a small delay for smooth transition
                    await Task.Delay(50);
                    Navigation.NavigateTo(targetType);
                    OnPropertyChanged(nameof(CurrentName));
                    UpdateCurrentStatusText();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing view: {ex.Message}");
            }
        }

        [RelayCommand]
        private void RefreshStatus()
        {
            UpdateCurrentStatusText();
            UpdateTaskbarProgress();
        }

        #endregion

        #region Event Handlers

        private void NavigationService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Navigation.CurrentView))
            {
                OnPropertyChanged(nameof(CurrentName));
                UpdateCurrentStatusText();
            }
        }

        private void LibraryVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(LibraryVM.IsNext):
                    EnableSpotify = _libraryVM.IsNext;
                    // Reset Spotify state when library changes
                    if (_libraryVM.IsNext)
                    {
                        _spotifyVM.IsNext = false;
                        EnableExport = false;
                    }
                    UpdateCurrentStatusText();
                    break;

                case nameof(LibraryVM.ProgressValue):
                    ProgressValue = _libraryVM.ProgressValue;
                    UpdateTaskbarProgress();
                    break;

                case nameof(LibraryVM.ShowProgressBar):
                    IsProcessing = _libraryVM.ShowProgressBar;
                    UpdateTaskbarProgress();
                    break;

                case nameof(LibraryVM.StatusText):
                    if (CurrentName == "LibraryVM")
                    {
                        CurrentStatusText = _libraryVM.StatusText;
                    }
                    break;
            }
        }

        private void SpotifyVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SpotifyVM.IsNext):
                    EnableExport = _spotifyVM.IsNext;
                    UpdateCurrentStatusText();
                    break;

                case nameof(SpotifyVM.PlaylistName):
                case nameof(SpotifyVM.PlaylistFound):
                case nameof(SpotifyVM.PlaylistLength):
                    if (CurrentName == "SpotifyVM")
                    {
                        UpdateCurrentStatusText();
                    }
                    break;
            }
        }

        #endregion

        #region Private Methods

        private void UpdateCurrentStatusText()
        {
            CurrentStatusText = CurrentName switch
            {
                "LibraryVM" => GetLibraryStatus(),
                "SpotifyVM" => GetSpotifyStatus(),
                "ExportVM" => GetExportStatus(),
                _ => "Ready"
            };
        }

        private string GetLibraryStatus()
        {
            if (_libraryVM.ShowProgressBar)
            {
                return _libraryVM.StatusText;
            }

            int fileCount = _libraryVM.AudioFiles.Count;
            return fileCount switch
            {
                0 => "No audio files loaded. Add files or folders to begin.",
                1 => "1 audio file loaded and ready.",
                _ => $"{fileCount:N0} audio files loaded and ready."
            };
        }

        private string GetSpotifyStatus()
        {
            if (string.IsNullOrEmpty(_spotifyVM.PlaylistName))
            {
                return "Enter a Spotify playlist ID or URL to analyze.";
            }

            int found = _spotifyVM.PlaylistFound;
            int total = _spotifyVM.PlaylistLength;

            if (total == 0)
            {
                return $"Playlist '{_spotifyVM.PlaylistName}' loaded but contains no tracks.";
            }

            if (found == 0)
            {
                return $"Playlist '{_spotifyVM.PlaylistName}' - {total} tracks, none found in library.";
            }

            double percentage = (found * 100.0) / total;
            return $"Playlist '{_spotifyVM.PlaylistName}' - {found}/{total} tracks found ({percentage:F1}%).";
        }

        private string GetExportStatus()
        {
            if (_spotifyVM.PlaylistFound == 0)
            {
                return "No matching tracks to export. Go back to find more matches.";
            }

            return $"Ready to export {_spotifyVM.PlaylistFound} matching tracks to M3U playlist.";
        }

        private void UpdateTaskbarProgress()
        {
            if (IsProcessing && ProgressValue > 0f && ProgressValue < 1f)
            {
                TaskbarState = TaskbarItemProgressState.Normal;
            }
            else if (IsProcessing && ProgressValue >= 1f)
            {
                TaskbarState = TaskbarItemProgressState.None;
                // Brief indication of completion
                Task.Delay(1000).ContinueWith(_ => TaskbarState = TaskbarItemProgressState.None);
            }
            else
            {
                TaskbarState = TaskbarItemProgressState.None;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets navigation information for UI binding
        /// </summary>
        public NavigationInfo GetNavigationInfo()
        {
            return new NavigationInfo
            {
                CurrentView = CurrentName,
                CanGoToSpotify = EnableSpotify,
                CanGoToExport = EnableExport,
                IsProcessing = IsProcessing,
                StatusText = CurrentStatusText
            };
        }

        #endregion
    }

    #region Helper Classes

    public class NavigationInfo
    {
        public string CurrentView { get; set; } = string.Empty;
        public bool CanGoToSpotify { get; set; }
        public bool CanGoToExport { get; set; }
        public bool IsProcessing { get; set; }
        public string StatusText { get; set; } = string.Empty;
    }

    #endregion
}