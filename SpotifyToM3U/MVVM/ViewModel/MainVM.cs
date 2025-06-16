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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing view: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void NavigationService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Navigation.CurrentView))
            {
                OnPropertyChanged(nameof(CurrentName));
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
                    break;

                case nameof(LibraryVM.ProgressValue):
                    ProgressValue = _libraryVM.ProgressValue;
                    UpdateTaskbarProgress();
                    break;

                case nameof(LibraryVM.ShowProgressBar):
                    UpdateTaskbarProgress();
                    break;
            }
        }

        private void SpotifyVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SpotifyVM.IsNext):
                    EnableExport = _spotifyVM.IsNext;
                    break;
            }
        }

        #endregion

        #region Private Methods

        private void UpdateTaskbarProgress()
        {
            if (_libraryVM.ShowProgressBar && ProgressValue > 0f && ProgressValue < 1f)
            {
                TaskbarState = TaskbarItemProgressState.Normal;
            }
            else if (_libraryVM.ShowProgressBar && ProgressValue >= 1f)
            {
                TaskbarState = TaskbarItemProgressState.Normal;
                // Brief indication of completion, then hide
                Task.Delay(2000).ContinueWith(_ =>
                {
                    if (!_libraryVM.ShowProgressBar) // Only hide if no longer processing
                        TaskbarState = TaskbarItemProgressState.None;
                });
            }
            else
            {
                TaskbarState = TaskbarItemProgressState.None;
            }
        }

        #endregion

        #region Keyboard Shortcut Commands

        [RelayCommand]
        private void LibraryAddFolder()
        {
            LibraryVM libraryVM = App.Current.ServiceProvider.GetRequiredService<LibraryVM>();
            if (libraryVM.AddFolderCommand.CanExecute(null))
                libraryVM.AddFolderCommand.Execute(null);
        }

        [RelayCommand]
        private void LibraryAddFile()
        {
            LibraryVM libraryVM = App.Current.ServiceProvider.GetRequiredService<LibraryVM>();
            if (libraryVM.AddFileCommand.CanExecute(null))
                libraryVM.AddFileCommand.Execute(null);
        }

        [RelayCommand]
        private void Export()
        {
            if (EnableExport)
            {
                ExportVM exportVM = App.Current.ServiceProvider.GetRequiredService<ExportVM>();
                if (exportVM.ExportCommand.CanExecute(null))
                    exportVM.ExportCommand.Execute(null);
            }
        }

        [RelayCommand]
        private void RefreshSpotify()
        {
            SpotifyVM spotifyVM = App.Current.ServiceProvider.GetRequiredService<SpotifyVM>();
            if (spotifyVM.IsAuthenticated && spotifyVM.LoadUserPlaylistsCommand.CanExecute(null))
                spotifyVM.LoadUserPlaylistsCommand.Execute(null);
        }

        [RelayCommand]
        private void Cancel()
        {
            LibraryVM libraryVM = App.Current.ServiceProvider.GetRequiredService<LibraryVM>();
            if (libraryVM.ShowProgressBar && libraryVM.CancelOperationCommand.CanExecute(null))
                libraryVM.CancelOperationCommand.Execute(null);
        }

        #endregion
    }
}