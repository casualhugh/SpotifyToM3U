using System;
using System.Windows;
using System.Windows.Controls;

namespace SpotifyToM3U.MVVM.View.Windows
{
    /// <summary>
    /// Modern window control view with enhanced functionality and styling
    /// </summary>
    public partial class ControlView : UserControl
    {
        private Window? _parentWindow;

        public ControlView()
        {
            InitializeComponent();
            Loaded += OnControlLoaded;
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _parentWindow.StateChanged += OnParentWindowStateChanged;
                UpdateMaximizeButton();
            }
        }

        private void OnParentWindowStateChanged(object? sender, EventArgs e)
        {
            UpdateMaximizeButton();
        }

        private void UpdateMaximizeButton()
        {
            if (_parentWindow != null && MaximizeIcon != null)
            {
                if (_parentWindow.WindowState == WindowState.Maximized)
                {
                    MaximizeIcon.Text = "\uE923"; // Restore icon
                    MaximizeButton.ToolTip = "Restore";
                }
                else
                {
                    MaximizeIcon.Text = "\uE922"; // Maximize icon
                    MaximizeButton.ToolTip = "Maximize";
                }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null)
            {
                _parentWindow.WindowState = WindowState.Minimized;
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null)
            {
                _parentWindow.WindowState = _parentWindow.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null)
            {
                _parentWindow.Close();
            }
        }
    }
}