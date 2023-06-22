using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SpotifyToM3U.Core;
using System;
using System.ComponentModel;
using System.Windows.Shell;

namespace SpotifyToM3U.MVVM.ViewModel
{
    internal partial class MainVM : ViewModelObject
    {
        [ObservableProperty]
        private bool _enableSpotify = false;
        private LibraryVM _libraryVM;

        [ObservableProperty]
        private bool _enableExport = false;
        private SpotifyVM _spotifyVM;

        [ObservableProperty]
        private float _progressValue = 0f;

        [ObservableProperty]
        private TaskbarItemProgressState _taskbarState = TaskbarItemProgressState.None;
        public MainVM(INavigationService navigation) : base(navigation)
        {
            Navigation.PropertyChanged += NavigationService_PropertyChanged;
            _libraryVM = App.Current.ServiceProvider.GetRequiredService<LibraryVM>();
            _spotifyVM = App.Current.ServiceProvider.GetRequiredService<SpotifyVM>();
            _libraryVM.PropertyChanged += LibraryVM_PropertyChanged;
            _spotifyVM.PropertyChanged += SpotifyVM_PropertyChanged;

        }


        private void SpotifyVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsNext")
                EnableExport = _spotifyVM.IsNext;
        }
        private void LibraryVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsNext")
            {
                EnableSpotify = _libraryVM.IsNext;
                _spotifyVM.IsNext = false;
            }
            else if (e.PropertyName == "ProgressValue")
            {
                ProgressValue = _libraryVM.ProgressValue;
                if (ProgressValue is > 0f and < 1f)
                    TaskbarState = TaskbarItemProgressState.Normal;
                else TaskbarState = TaskbarItemProgressState.None;
            }
        }

        public string CurrentName => Navigation.CurrentView.GetType().Name;

        [RelayCommand]
        private void ChangeView(string name)
        {
            Navigation.NavigateTo(Type.GetType($"SpotifyToM3U.MVVM.ViewModel.{name}")!);
            OnPropertyChanged(nameof(CurrentName));
        }

        private void NavigationService_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentView")
                OnPropertyChanged(nameof(CurrentName));
        }

    }
}
