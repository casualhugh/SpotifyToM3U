using Microsoft.Extensions.DependencyInjection;
using SpotifyToM3U.Core;
using SpotifyToM3U.MVVM.Model;
using SpotifyToM3U.MVVM.View.Windows;
using SpotifyToM3U.MVVM.ViewModel;
using System;
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

            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<Func<Type, ViewModelObject>>(provider => viewModelType => (ViewModelObject)provider.GetRequiredService(viewModelType));

            _serviceProvider = services.BuildServiceProvider();


        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _serviceProvider.GetRequiredService<INavigationService>().NavigateTo<LibraryVM>();
            MainWindow window = _serviceProvider.GetRequiredService<MainWindow>();
            _IOManager = new IOManager();
            window.Show();
        }

    }
}
