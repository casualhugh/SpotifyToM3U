using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace SpotifyToM3U.MVVM.View.Windows
{
    /// <summary>
    /// Modern window control view with enhanced functionality and styling
    /// </summary>
    public partial class ControlView : UserControl
    {
        #region Fields

        private Window? _parentWindow;
        private HwndSource? _hwndSource;

        #endregion

        #region Properties

        /// <summary>
        /// Command that closes the window
        /// </summary>
        public RelayCommand CloseCommand { get; private set; }

        /// <summary>
        /// Command that minimizes the window
        /// </summary>
        public RelayCommand MinimizeCommand { get; private set; }

        /// <summary>
        /// Command that maximizes/restores the window
        /// </summary>
        public RelayCommand MaximizeCommand { get; private set; }

        #endregion

        #region Constructor

        public ControlView()
        {
            InitializeComponent();
            InitializeCommands();
            Loaded += OnControlLoaded;
            Unloaded += OnControlUnloaded;
        }

        #endregion

        #region Event Handlers

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _parentWindow = Window.GetWindow(this);
                if (_parentWindow == null) return;

                // Set up window hook for snap layout support
                SetupWindowHook();

                // Wire up window events
                _parentWindow.Closed += OnParentWindowClosed;
                _parentWindow.StateChanged += OnParentWindowStateChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up window controls: {ex.Message}");
            }
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            CleanupWindowHook();

            if (_parentWindow != null)
            {
                _parentWindow.Closed -= OnParentWindowClosed;
                _parentWindow.StateChanged -= OnParentWindowStateChanged;
            }
        }

        private void OnParentWindowClosed(object? sender, EventArgs e)
        {
            // Ensure application shuts down properly
            if (Application.Current?.MainWindow == _parentWindow)
            {
                Application.Current.Shutdown();
            }
        }

        private void OnParentWindowStateChanged(object? sender, EventArgs e)
        {
            // Update button states or perform other actions when window state changes
            UpdateButtonStates();
        }

        #endregion

        #region Private Methods

        private void InitializeCommands()
        {
            CloseCommand = new RelayCommand(ExecuteClose, CanExecuteWindowCommand);
            MinimizeCommand = new RelayCommand(ExecuteMinimize, CanExecuteWindowCommand);
            MaximizeCommand = new RelayCommand(ExecuteMaximize, CanExecuteWindowCommand);
        }

        private bool CanExecuteWindowCommand()
        {
            return _parentWindow != null;
        }

        private void ExecuteClose()
        {
            try
            {
                _parentWindow?.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing window: {ex.Message}");
            }
        }

        private void ExecuteMinimize()
        {
            try
            {
                if (_parentWindow != null)
                {
                    _parentWindow.WindowState = WindowState.Minimized;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error minimizing window: {ex.Message}");
            }
        }

        private void ExecuteMaximize()
        {
            try
            {
                if (_parentWindow == null) return;

                _parentWindow.WindowState = _parentWindow.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error maximizing/restoring window: {ex.Message}");
            }
        }

        private void UpdateButtonStates()
        {
            // Notify commands that their execution state might have changed
            CloseCommand.NotifyCanExecuteChanged();
            MinimizeCommand.NotifyCanExecuteChanged();
            MaximizeCommand.NotifyCanExecuteChanged();
        }

        private void SetupWindowHook()
        {
            try
            {
                if (_parentWindow == null) return;

                WindowInteropHelper windowInteropHelper = new(_parentWindow);
                nint hwnd = windowInteropHelper.Handle;

                if (hwnd != IntPtr.Zero)
                {
                    _hwndSource = HwndSource.FromHwnd(hwnd);
                    _hwndSource?.AddHook(WindowMessageHook);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up window hook: {ex.Message}");
            }
        }

        private void CleanupWindowHook()
        {
            try
            {
                _hwndSource?.RemoveHook(WindowMessageHook);
                _hwndSource = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up window hook: {ex.Message}");
            }
        }

        private IntPtr WindowMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                // Handle Windows 11 snap layout functionality
                const int WM_NCHITTEST = 0x0084;
                const int WM_NCLBUTTONDOWN = 0x00A1;
                const int HTMAXBUTTON = 9;

                switch (msg)
                {
                    case WM_NCHITTEST:
                        if (IsOverMaximizeButton(lParam))
                        {
                            handled = true;
                            return new IntPtr(HTMAXBUTTON);
                        }
                        break;

                    case WM_NCLBUTTONDOWN:
                        if (IsOverMaximizeButton(lParam))
                        {
                            ExecuteMaximize();
                            handled = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in window message hook: {ex.Message}");
            }

            return IntPtr.Zero;
        }

        private bool IsOverMaximizeButton(IntPtr lParam)
        {
            try
            {
                // Extract coordinates from lParam
                int x = lParam.ToInt32() & 0xFFFF;
                int y = (lParam.ToInt32() >> 16) & 0xFFFF;

                // Get maximize button bounds in screen coordinates
                Rect buttonBounds = MaximizeButton.TransformToAncestor(_parentWindow!)
                    .TransformBounds(new Rect(0, 0, MaximizeButton.ActualWidth, MaximizeButton.ActualHeight));

                Point screenBounds = _parentWindow!.PointToScreen(buttonBounds.TopLeft);
                Rect rect = new(screenBounds.X, screenBounds.Y, buttonBounds.Width, buttonBounds.Height);

                return rect.Contains(new Point(x, y));
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the state of all window control commands
        /// </summary>
        public void RefreshCommands()
        {
            UpdateButtonStates();
        }

        /// <summary>
        /// Gets information about the current window state
        /// </summary>
        public WindowControlInfo GetWindowInfo()
        {
            return new WindowControlInfo
            {
                CanClose = CanExecuteWindowCommand(),
                CanMinimize = CanExecuteWindowCommand(),
                CanMaximize = CanExecuteWindowCommand(),
                WindowState = _parentWindow?.WindowState ?? WindowState.Normal,
                IsActive = _parentWindow?.IsActive ?? false
            };
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Information about window control state
    /// </summary>
    public class WindowControlInfo
    {
        public bool CanClose { get; set; }
        public bool CanMinimize { get; set; }
        public bool CanMaximize { get; set; }
        public WindowState WindowState { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion
}