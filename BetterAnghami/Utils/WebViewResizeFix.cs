using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MRK
{
    /// <summary>
    /// Fixes resize-handle hit-testing for WPF windows hosting a WebView2 control.
    /// <para>
    /// WebView2 uses a Win32 child HWND that sits on top of the WPF visual layer and swallows
    /// <c>WM_NCHITTEST</c> before the parent window can respond. This class subclasses every child
    /// HWND and returns <c>HTTRANSPARENT</c> for the resize-border strip so Windows looks through
    /// to the parent, restoring normal drag-to-resize behaviour without any visual gaps.
    /// </para>
    /// </summary>
    internal sealed class WebViewResizeFix
    {
        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(
            IntPtr parentHwnd,
            EnumChildProc lpEnumFunc,
            IntPtr lParam
        );

        private delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(
            IntPtr lpPrevWndFunc,
            IntPtr hWnd,
            uint msg,
            IntPtr wParam,
            IntPtr lParam
        );

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left,
                Top,
                Right,
                Bottom;
        }

        private delegate IntPtr ChildWndProcDelegate(
            IntPtr hWnd,
            uint msg,
            IntPtr wParam,
            IntPtr lParam
        );

        private const int GwlpWndProc = -4;
        private const uint WmNcHitTest = 0x0084;
        private const int HtTransparent = -1;

        private readonly Window _owner;
        private readonly int _resizeBorder;

        private ChildWndProcDelegate? _wndProcDelegate; // kept alive as a field to prevent GC
        private readonly Dictionary<IntPtr, IntPtr> _subclassedHwnds = new(); // hwnd -> original WndProc

        public WebViewResizeFix(Window owner, int resizeBorder = 6)
        {
            _owner = owner;
            _resizeBorder = resizeBorder;
        }

        /// <summary>Subclasses all current child HWNDs. Call once after <c>EnsureCoreWebView2Async</c>.</summary>
        public void Attach()
        {
            _wndProcDelegate ??= WndProc;
            var procPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            var parentHwnd = new WindowInteropHelper(_owner).Handle;

            EnumChildWindows(
                parentHwnd,
                (hWnd, _) =>
                {
                    if (!_subclassedHwnds.ContainsKey(hWnd))
                    {
                        var origProc = SetWindowLongPtr(hWnd, GwlpWndProc, procPtr);
                        if (origProc != IntPtr.Zero)
                            _subclassedHwnds[hWnd] = origProc;
                    }
                    return true;
                },
                IntPtr.Zero
            );
        }

        /// <summary>Restores all original WndProcs. Call before the window is destroyed.</summary>
        public void Detach()
        {
            foreach (var (hwnd, origProc) in _subclassedHwnds)
                SetWindowLongPtr(hwnd, GwlpWndProc, origProc);
            _subclassedHwnds.Clear();
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (!_subclassedHwnds.TryGetValue(hWnd, out var origProc))
                return IntPtr.Zero;

            if (msg == WmNcHitTest)
            {
                var x = unchecked((short)(long)lParam);
                var y = unchecked((short)((long)lParam >> 16));
                GetWindowRect(new WindowInteropHelper(_owner).Handle, out var wr);

                if (
                    x <= wr.Left + _resizeBorder
                    || x >= wr.Right - _resizeBorder
                    || y >= wr.Bottom - _resizeBorder
                )
                    return new IntPtr(HtTransparent);
            }

            return CallWindowProc(origProc, hWnd, msg, wParam, lParam);
        }
    }
}
