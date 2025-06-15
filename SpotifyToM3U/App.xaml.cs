using Microsoft.Extensions.DependencyInjection;
using SpotifyToM3U.Core;
using SpotifyToM3U.MVVM.Model;
using SpotifyToM3U.MVVM.View.Windows;
using SpotifyToM3U.MVVM.ViewModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace SpotifyToM3U
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;
        public ServiceProvider ServiceProvider => _serviceProvider;
        internal MainVM MainVM { get; set; } = null!;
        internal IOManager _IOManager = null!;
        internal static new App Current => (App)Application.Current;

        public App()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(provider => new MainWindow { DataContext = provider.GetRequiredService<MainVM>() });
            services.AddSingleton(provider => new AddFolderWindow { DataContext = provider.GetRequiredService<AddFolderVM>() });
            services.AddSingleton<MainVM>();
            services.AddSingleton<LibraryVM>();
            services.AddSingleton<SpotifyVM>();
            services.AddSingleton<ExportVM>();
            services.AddSingleton<AddFolderVM>();
            services.AddTransient<SpotifySetupVM>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ISpotifyService, SpotifyService>();
            services.AddSingleton<Func<Type, ViewModelObject>>(provider => viewModelType => (ViewModelObject)provider.GetRequiredService(viewModelType));
            _serviceProvider = services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Show window first, then initialize in background
            _serviceProvider.GetRequiredService<INavigationService>().NavigateTo<LibraryVM>();
            MainWindow window = _serviceProvider.GetRequiredService<MainWindow>();
            _IOManager = new IOManager();
            window.Show();

            // Initialize Spotify service in background
            _ = Task.Run(async () => await InitializeSpotifyServiceAsync());
        }

        private async Task InitializeSpotifyServiceAsync()
        {
            try
            {
                ISpotifyService spotifyService = _serviceProvider.GetRequiredService<ISpotifyService>();
                await spotifyService.InitializeAsync();

                System.Diagnostics.Debug.WriteLine($"Spotify initialization completed. Authenticated: {spotifyService.IsAuthenticated}, User: '{spotifyService.CurrentUserName}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Spotify initialization error: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Debug.WriteLine("App shutting down - preserving authentication state");
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}