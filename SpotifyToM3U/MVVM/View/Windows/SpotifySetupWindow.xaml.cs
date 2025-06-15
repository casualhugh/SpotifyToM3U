using SpotifyToM3U.MVVM.ViewModel;
using System;
using System.Windows;
using System.Windows.Input;

namespace SpotifyToM3U.MVVM.View.Windows
{
    /// <summary>
    /// Interaction logic for SpotifySetupWindow.xaml
    /// </summary>
    public partial class SpotifySetupWindow : Window
    {
        public bool ConfigurationSaved { get; private set; } = false;

        public SpotifySetupWindow()
        {
            InitializeComponent();

            SpotifySetupVM viewModel = new();
            DataContext = viewModel;

            // Subscribe to ViewModel events
            viewModel.RequestClose += OnRequestClose;

            // Allow dragging the window
            MouseLeftButtonDown += (s, e) => DragMove();
        }

        private void OnRequestClose(object? sender, EventArgs e)
        {
            if (DataContext is SpotifySetupVM vm)
            {
                ConfigurationSaved = vm.ConfigurationSaved;
            }
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (DataContext is SpotifySetupVM vm)
                {
                    vm.CancelCommand.Execute(null);
                }
            }
            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            if (DataContext is SpotifySetupVM vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}