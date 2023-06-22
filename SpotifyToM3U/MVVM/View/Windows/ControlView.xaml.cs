using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace SpotifyToM3U.MVVM.View.Windows
{
    /// <summary>
    /// Interaktionslogik für ControlView.xaml
    /// </summary>
    public partial class ControlView : UserControl
    {
        public ControlView()
        {
            InitializeComponent();
            MainWindow window = (MainWindow)Application.Current.MainWindow;
            Loaded += (o, e) =>
            {
                HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                source.AddHook(new HwndSourceHook(HwndSourceHook));
            };
            window.Closed += (s, o) =>
            {
                Application.Current.Shutdown();
            };
            CloseCommand = new RelayCommand(() =>
            {
                window.Close();
            });
            MinimizeCommand = new RelayCommand(() => window.WindowState = WindowState.Minimized);
            MaximizeCommand = new RelayCommand(() => { window.WindowState = window.WindowState != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal; });

        }

        private readonly SolidColorBrush _hoverColor = new((Color)ColorConverter.ConvertFromString("#FFD3D3D3"));
        private readonly SolidColorBrush _transparentColor = new(Color.FromArgb(0, 0, 0, 0));

        /// <summary>
        /// Commamd that Close the window
        /// </summary>
        public RelayCommand CloseCommand { get; }

        /// <summary>
        /// Command that Minimize the window
        /// </summary>
        public RelayCommand MinimizeCommand { get; }

        /// <summary>
        /// Command that Maximize the window
        /// </summary>
        public RelayCommand MaximizeCommand { get; }

        private const int WM_NCHITTEST = 0x0084;
        private const int HTMAXBUTTON = 9;

        private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                    try
                    {
                        SnapLayout(lparam, ref handled);
                        return new IntPtr(HTMAXBUTTON);
                    }
                    catch (OverflowException)
                    {
                        handled = true;
                    }
                    break;
                case 0x00A1:
                    MaxClicked(lparam, ref handled);
                    break;
            }
            return IntPtr.Zero;
        }

        private void MaxClicked(IntPtr lparam, ref bool handled)
        {
            int x = lparam.ToInt32() & 0xffff;
            int y = lparam.ToInt32() >> 16;
            Rect rect = new(Maximize.PointToScreen(new Point()), new Size(Maximize.Width, Maximize.Height));
            if (rect.Contains(new Point(x, y)))
                MaximizeCommand.Execute(null);
        }

        private void SnapLayout(IntPtr lparam, ref bool handled)
        {
            _ = lparam.ToInt32();
            int x = lparam.ToInt32() & 0xffff;
            int y = lparam.ToInt32() >> 16;
            Rect rect = new(Maximize.PointToScreen(new Point()), new Size(Maximize.Width, Maximize.Height));
            if (rect.Contains(new Point(x, y)))
            {
                Maximize.Background = _hoverColor;
                handled = true;
            }
            else
                Maximize.Background = _transparentColor;
        }
    }
}

