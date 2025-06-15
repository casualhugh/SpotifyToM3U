using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace SpotifyToM3U.Core
{
    /// <summary>
    /// Helper class to properly handle window maximization while respecting the taskbar
    /// Uses WM_GETMINMAXINFO message to constrain maximized window to working area
    /// </summary>
    public static class WindowMaximizationHelper
    {
        #region Win32 API and Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>
            /// x coordinate of point.
            /// </summary>
            public int x;
            /// <summary>
            /// y coordinate of point.
            /// </summary>
            public int y;

            /// <summary>
            /// Construct a point of coordinates (x,y).
            /// </summary>
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            /// <summary> Win32 </summary>
            public int left;
            /// <summary> Win32 </summary>
            public int top;
            /// <summary> Win32 </summary>
            public int right;
            /// <summary> Win32 </summary>
            public int bottom;

            /// <summary> Win32 </summary>
            public static readonly RECT Empty = new();

            /// <summary> Win32 </summary>
            public int Width
            {
                get { return Math.Abs(right - left); }  // Abs needed for BIDI OS
            }
            /// <summary> Win32 </summary>
            public int Height
            {
                get { return bottom - top; }
            }

            /// <summary> Win32 </summary>
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            /// <summary> Win32 </summary>
            public RECT(RECT rcSrc)
            {
                this.left = rcSrc.left;
                this.top = rcSrc.top;
                this.right = rcSrc.right;
                this.bottom = rcSrc.bottom;
            }

            /// <summary> Win32 </summary>
            public bool IsEmpty
            {
                get
                {
                    // BUGBUG : On Bidi OS (hebrew arabic) left > right
                    return left >= right || top >= bottom;
                }
            }

            /// <summary> Return a user friendly representation of this struct </summary>
            public override string ToString()
            {
                if (this == RECT.Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }

            /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
            public override bool Equals(object obj)
            {
                if (!(obj is RECT)) { return false; }
                return (this == (RECT)obj);
            }

            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode()
            {
                return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            }

            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2)
            {
                return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
            }

            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2)
            {
                return !(rect1 == rect2);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            /// <summary>
            /// </summary>            
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            /// <summary>
            /// </summary>            
            public RECT rcMonitor = new();

            /// <summary>
            /// </summary>            
            public RECT rcWork = new();

            /// <summary>
            /// </summary>            
            public int dwFlags = 0;
        }

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        #endregion

        private const int WM_GETMINMAXINFO = 0x0024;
        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        /// <summary>
        /// Sets up proper maximization behavior for a window
        /// </summary>
        /// <param name="window">The window to configure</param>
        public static void EnableProperMaximization(Window window)
        {
            if (window == null) return;

            window.SourceInitialized += (sender, e) =>
            {
                IntPtr handle = (new WindowInteropHelper(window)).Handle;
                HwndSource.FromHwnd(handle)?.AddHook(new HwndSourceHook(WindowProc));
            };
        }

        private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_GETMINMAXINFO:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;

                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);

                // Also set the max track size to prevent resizing beyond working area
                mmi.ptMaxTrackSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxTrackSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }

            // CRITICAL: Enforce MinWidth and MinHeight from the WPF window
            // This is what was missing - WPF's MinWidth/MinHeight don't work with WindowStyle=None
            Window? window = HwndSource.FromHwnd(hwnd)?.RootVisual as Window;
            if (window != null)
            {
                DpiScale dpiScale = VisualTreeHelper.GetDpi(window);
                int minWidth = (int)(window.MinWidth * dpiScale.DpiScaleX);
                int minHeight = (int)(window.MinHeight * dpiScale.DpiScaleY);

                // Enforce minimum tracking size (prevents resizing smaller than MinWidth/MinHeight)
                if (minWidth > 0)
                    mmi.ptMinTrackSize.x = Math.Max(mmi.ptMinTrackSize.x, minWidth);
                if (minHeight > 0)
                    mmi.ptMinTrackSize.y = Math.Max(mmi.ptMinTrackSize.y, minHeight);
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        /// <summary>
        /// Alternative method using Screen.FromHandle (requires reference to System.Windows.Forms)
        /// This is a simpler approach that also works well
        /// </summary>
        /// <param name="window">The window to configure</param>
        public static void EnableProperMaximizationSimple(Window window)
        {
            if (window == null) return;

            window.SourceInitialized += (sender, e) =>
            {
                nint handle = (new WindowInteropHelper(window)).Handle;
                HwndSource handleSource = HwndSource.FromHwnd(handle);
                if (handleSource == null)
                    return;
                handleSource.AddHook(WindowProcSimple);
            };
        }

        private static IntPtr WindowProcSimple(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_GETMINMAXINFO: /* WM_GETMINMAXINFO */
                    WmGetMinMaxInfoSimple(hwnd, lParam);
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        private static void WmGetMinMaxInfoSimple(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            System.Windows.Forms.Screen currentScreen = System.Windows.Forms.Screen.FromHandle(hwnd);
            System.Drawing.Rectangle workArea = currentScreen.WorkingArea;
            System.Drawing.Rectangle monitorArea = currentScreen.Bounds;

            mmi.ptMaxPosition.x = Math.Abs(workArea.Left - monitorArea.Left);
            mmi.ptMaxPosition.y = Math.Abs(workArea.Top - monitorArea.Top);
            mmi.ptMaxSize.x = Math.Abs(workArea.Right - workArea.Left);
            mmi.ptMaxSize.y = Math.Abs(workArea.Bottom - workArea.Top);

            // Also set the max track size to prevent resizing beyond working area
            mmi.ptMaxTrackSize.x = Math.Abs(workArea.Width);
            mmi.ptMaxTrackSize.y = Math.Abs(workArea.Height);

            Marshal.StructureToPtr(mmi, lParam, true);
        }
    }
}