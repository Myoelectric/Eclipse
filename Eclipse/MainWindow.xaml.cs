﻿using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NLog;
using Newtonsoft.Json;

namespace Eclipse
{
    public partial class MainWindow : Window
    {
        public static MainWindow mywindow { get; private set; } // for static functions
        private static RightClickMenu rcm;
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        /*   KEY EVENT HOOKS   */
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private static LowLevelKeyboardProc llkp = HookCallback;
        private static IntPtr hwndid = IntPtr.Zero;

        /*   MENU SETTINGS   */
        private static double MenuSize = Properties.Settings.Default.MenuSize;
        private const double MenuTipSize = 120;
        private const double MenuItemCount = 8;
        private const double MenuItemPadding = 2;
        private const double MenuOpenSpeed = 300; //ms
        private const double MenuCloseSpeed = 200; //ms
        private const double MenuCancelSpeed = 200; //ms
        private const double MenuItemAngle = (360 / MenuItemCount);
        private const double MenuCenterSection = 1;
        private static SolidColorBrush MenuOuterColor = new SolidColorBrush(Color.FromRgb(20, 136, 214));

        /*   MENU TRACKING   */
        private static bool MenuOpen;
        private static DateTime KeyDownTime;
        private static Point MenuCenter;
        //private static readonly List<Path> MenuItems = new List<Path>();

        public MainWindow()
        {
            InitializeComponent();

            mywindow = this;
            rcm = new RightClickMenu();

            Topmost = true;
            ShowInTaskbar = false;
            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;
            Background = Brushes.Transparent;
            Width = MenuSize + MenuTipSize;
            Height = MenuSize + MenuTipSize;

            Container.RenderTransform = new ScaleTransform(0, 0);
            MenuCenter = new Point(Width / 2, Height / 2);

            _logger.Debug("Init center {0},{1} and size {2},{3}", MenuCenter.X, MenuCenter.Y, Width, Height);

            // Initialize Menu
            for(int i = 0; i < MenuItemCount; i++)
            {
                Path TempMenuTip = CreateMenuItemTip(i);
                TempMenuTip.MouseEnter += MenuItem_MouseEnter;
                TempMenuTip.MouseLeave += MenuItem_MouseLeave;
                TempMenuTip.MouseRightButtonUp += MenuItem_MouseRightButtonUp;
                //MenuItems.Add(TempMenuTip);
                Container.Children.Add(TempMenuTip);
            }
            _logger.Debug("Added {0} menu items", MenuItemCount);

            hwndid = SetHook(llkp);

            if(Properties.Settings.Default.Clips != null)
            {
                foreach (string clip in Properties.Settings.Default.Clips)
                {
                    string breaking = clip;
                }
            }
            else
            {
                Properties.Settings.Default.Clips = new System.Collections.Specialized.StringCollection();
            }
        }

        private void MenuItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Matrix mymatrix = PresentationSource.FromVisual(mywindow).CompositionTarget.TransformFromDevice;
            Point mouse = mymatrix.Transform(GetMousePosition());
            rcm.Left = mouse.X;
            rcm.Top = mouse.Y;
            if (rcm.Shown)
            {
                rcm.Hide();
                rcm.Shown = false;
                return;
            }

            int clipCount = (Properties.Settings.Default.Clips.Count > 5) ? 5 : Properties.Settings.Default.Clips.Count;
            for (int i = 0; i < clipCount; i++)
            {
                string clipText = Properties.Settings.Default.Clips[i].ToString();
                if (clipText.Length > 100) { clipText.Substring(0, 100); }
                rcm.listBoxClips.Items.Add(new ListBoxItem() { Content = clipText });
            }

            rcm.Shown = true;
            rcm.Topmost = true;
            rcm.Show();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            //// clipboard hook
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            NativeMethods.AddClipboardFormatListener(windowHandle);
        }

        private void MenuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            Path CurrentPath = (Path)sender;
            CurrentPath.StrokeThickness = 0;
        }

        private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            Path CurrentPath = (Path)sender;
            CurrentPath.Stroke = Brushes.Cyan;
            CurrentPath.StrokeThickness = 4;
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private void OnClipboardChanged()
        {
            _logger.Debug("Clipboard caught");
            //TaskbarIcon notifyicon = (TaskbarIcon)FindResource("NotifyIcon");
            //notifyicon.ShowBalloonTip("Eclipse", "Contents saved to Eclipse.", BalloonIcon.Info);
            try
            {
                if (Clipboard.ContainsText())
                {
                    Clip currentClip = new Clip()
                    {
                        ClipData = Clipboard.GetText()
                    };

                    // get window that copy came from
                    const int nChars = 256;
                    StringBuilder Buff = new StringBuilder(nChars);
                    IntPtr handle = GetForegroundWindow();

                    if (GetWindowText(handle, Buff, nChars) > 0)
                    {
                        // get window name, process id and icon
                        string windowName = Buff.ToString();
                        int windowTID = (int)GetWindowThreadProcessId(handle, out uint windowPID);
                        if (windowPID < 0)
                        {
                            _logger.Error("Failed to get windowPID.");
                            return;
                        }
                        Process windowProcess = Process.GetProcessById((int)windowPID);
                        _logger.Debug("Process name {0}", windowProcess.ProcessName);
                        try
                        {
                            currentClip.ProcessName = windowProcess.ProcessName;
                        }
                        catch(Exception exc)
                        {
                            _logger.Error(exc, "Failed to save process name.");
                            return;
                        }
                        try
                        {
                            string windowPath = windowProcess.MainModule.FileName;
                            _logger.Debug("Process path {0}", windowPath);
                            System.Drawing.Icon windowIcon = System.Drawing.Icon.ExtractAssociatedIcon(windowPath); //ambiguity between Window.Icon and System.Drawing.Icon
                            ImageSource imageIconSource = Imaging.CreateBitmapSourceFromHIcon(
                                windowIcon.Handle,
                                new Int32Rect(0, 0, windowIcon.Width, windowIcon.Height),
                                BitmapSizeOptions.FromEmptyOptions());
                            Image imageIcon = new Image()
                            {
                                Name = windowProcess.ProcessName,
                                Source = imageIconSource,
                                Width = 32,
                                Height = 32,
                                Margin = new Thickness(0, 0, 0, 0) // todo: get actual coords from math
                            };
                            Container.Children.Add(imageIcon);
                            using (System.IO.MemoryStream ms = new System.IO.MemoryStream()) // ambiguity between System.IO and System.Windows.Shapes
                            {
                                windowIcon.Save(ms);
                                currentClip.ProcessIcon = Convert.ToBase64String(ms.ToArray());
                            }
                        }
                        catch(Exception exc)
                        {
                            _logger.Error(exc, "Failed to add icon. " + exc.Message);
                            return;
                        }
                    }
                    else
                    {
                        currentClip.ProcessName = "Eclipse";
                        currentClip.ProcessIcon = "";
                    }

                    Properties.Settings.Default.Clips.Add(JsonConvert.SerializeObject(currentClip, Formatting.None));
                    Properties.Settings.Default.Save();

                    _logger.Debug("Saved text data to clipboard.");
                }
                else if (Clipboard.ContainsImage())
                {
                    Popup popimg = new Popup();
                    ImageSource img;
                    _logger.Debug("Saved image data to clipboard.");
                    //System.IO.MemoryStream msimg = Clipboard.GetData("DeviceIndependentBitmap") as System.IO.MemoryStream;
                    //msimg.WriteTo(new System.IO.FileStream("c:\\users\\clanum\\downloads\\test.bmp", System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite));
                    //Console.WriteLine("Saved new image.");
                    //System.Windows.Forms.IDataObject clipboardData = System.Windows.Forms.Clipboard.GetDataObject();
                    //if (clipboardData != null)
                    //{
                    //    if (clipboardData.GetDataPresent(System.Windows.Forms.DataFormats.Bitmap))
                    //    {
                    //        System.Drawing.Bitmap bitmap = (System.Drawing.Bitmap)clipboardData.GetData(System.Windows.Forms.DataFormats.Bitmap);
                    //        ImageUIElement.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    //        Console.WriteLine("Clipboard copied to UIElement");
                    //    }
                    //}
                    //IDataObject clipboardData = Clipboard.GetDataObject();
                    //if (clipboardData != null)
                    //{
                    //    if (clipboardData.GetDataPresent(System.Windows.Forms.DataFormats.Bitmap))
                    //    {
                    //        System.Drawing.Bitmap bitmap = (System.Drawing.Bitmap)clipboardData.GetData(System.Windows.Forms.DataFormats.Bitmap);
                    //        IntPtr hBitmap = bitmap.GetHbitmap();
                    //        try
                    //        {
                    //            ImageUIElement.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    //            Console.WriteLine("Clipboard copied to UIElement");
                    //        }
                    //        finally
                    //        {
                    //            DeleteObject(hBitmap)
                    //        }
                    //    }
                    //}
                }
            }
            catch (COMException ex)
            {
                _logger.Error(ex, "Clipboard event error.");
                return;
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            //char cKey = GetCharFromKey(iParam);
            // 160 - lshift, 161 - rshift
            // 162 - lcntrl, 163 - rcntrl
            // 164 - lalt, 165 - ralt
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    int iParam = Marshal.ReadInt32(lParam);
                    if (iParam == 162 && MenuOpen == false)
                    {
                        if (KeyDownTime != DateTime.MinValue)
                        {
                            TimeSpan KeyDownLength = (DateTime.Now - KeyDownTime);
                            if (KeyDownLength.TotalMilliseconds > Properties.Settings.Default.OpenDelay)
                            {
                                // open menu
                                MenuOpen = true;
                                Matrix mymatrix = PresentationSource.FromVisual(mywindow).CompositionTarget.TransformFromDevice;
                                Point mouse = mymatrix.Transform(GetMousePosition());
                                mywindow.Left = mouse.X - mywindow.Width / 2;
                                mywindow.Top = mouse.Y - mywindow.Height / 2;

                                ScaleTransform st = new ScaleTransform();
                                st.CenterX = MenuCenter.X;
                                st.CenterY = MenuCenter.Y;
                                mywindow.Container.RenderTransform = st;
                                DoubleAnimation da = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(MenuOpenSpeed));
                                st.BeginAnimation(ScaleTransform.ScaleXProperty, da);
                                st.BeginAnimation(ScaleTransform.ScaleYProperty, da);
                            }
                        }
                        else
                        {
                            KeyDownTime = DateTime.Now;
                        }
                    }
                }
                if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    int iParam = Marshal.ReadInt32(lParam);
                    if (iParam == 162 && KeyDownTime != DateTime.MinValue) { KeyDownTime = DateTime.MinValue; }
                    else if (iParam == 162 && KeyDownTime == DateTime.MinValue && MenuOpen == true)
                    {
                        // close menu
                        MenuOpen = false;
                        ScaleTransform st = new ScaleTransform();
                        st.CenterX = MenuCenter.X;
                        st.CenterY = MenuCenter.Y;
                        mywindow.Container.RenderTransform = st;
                        DoubleAnimation da = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(MenuCloseSpeed));
                        st.BeginAnimation(ScaleTransform.ScaleXProperty, da);
                        st.BeginAnimation(ScaleTransform.ScaleYProperty, da);
                    }
                }
            }

            return CallNextHookEx(hwndid, nCode, wParam, lParam);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            UnhookWindowsHookEx(hwndid);
        }

        public static char GetCharFromKey(int virtualKey)
        {
            char ch = ' ';

            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        [DllImport("user32.dll")]
        public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                OnClipboardChanged();
                handled = true;
            }

            return IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3
        }

        private static Point ComputeCartesianCoordinate(Point center, double angle, double radius)
        {
            // Converts to radians
            double radiansAngle = (Math.PI / 180.0) * (angle - 90);
            double x = radius * Math.Cos(radiansAngle);
            double y = radius * Math.Sin(radiansAngle);
            return new Point(x + center.X, y + center.Y);
        }

        private Path CreateMenuItemTip(int index)
        {
            // menu item info
            double MenuItemStartAngle = MenuItemAngle * index;
            double MenuItemRotation = MenuItemStartAngle + MenuItemAngle / 2;
            double OuterRingRadius = MenuSize / 2;
            double InnerRingRadius = (MenuSize - MenuTipSize) / 2;
            double OuterAngleDiff = (180 * (MenuItemPadding / OuterRingRadius)) / Math.PI;
            double InnerAngleDiff = (180 * (MenuItemPadding / InnerRingRadius)) / Math.PI;
            double OuterRingAngle = MenuItemStartAngle + OuterAngleDiff;
            double OuterAngleDelta = MenuItemAngle - (OuterAngleDiff * 2);
            double InnerRingAngle = MenuItemStartAngle + InnerAngleDiff;
            double InnerAngleDelta = MenuItemAngle - (InnerAngleDiff * 2);

            Point OuterArcStart = ComputeCartesianCoordinate(MenuCenter, OuterRingAngle, OuterRingRadius + MenuCenterSection);
            Point OuterArcEnd = ComputeCartesianCoordinate(MenuCenter, OuterRingAngle + OuterAngleDelta, OuterRingRadius + MenuCenterSection);
            Point InnerArcStart = ComputeCartesianCoordinate(MenuCenter, InnerRingAngle, InnerRingRadius + MenuCenterSection);
            Point InnerArcEnd = ComputeCartesianCoordinate(MenuCenter, InnerRingAngle + InnerAngleDelta, InnerRingRadius + MenuCenterSection);

            bool LargeOuterArc = OuterAngleDelta > 180.0;
            bool LargeInnerArc = InnerAngleDelta > 180.0;

            // Create path
            Path TempPath = new Path();
            TempPath.Fill = MenuOuterColor;
            TempPath.Opacity = 0.7;
            TempPath.StrokeThickness = 0;

            // create streamgeometry for path
            StreamGeometry geom = new StreamGeometry();
            geom.FillRule = FillRule.EvenOdd;
            using (StreamGeometryContext ctx = geom.Open())
            {
                if (MenuItemCount > 1)
                {
                    ctx.BeginFigure(InnerArcStart, true, true);
                    ctx.LineTo(OuterArcStart, true, true);
                    ctx.ArcTo(OuterArcEnd, new Size(OuterRingRadius, OuterRingRadius), 0, LargeOuterArc, SweepDirection.Clockwise, true, true);
                    ctx.LineTo(InnerArcEnd, true, true);
                    ctx.ArcTo(InnerArcStart, new Size(InnerRingRadius, InnerRingRadius), 0, LargeInnerArc, SweepDirection.Counterclockwise, true, true);
                }
            }

            geom.Freeze();
            TempPath.Data = geom;

            return TempPath;
        }
    }

    // http://stackoverflow.com/a/33018459
    internal static class NativeMethods
    {
        // See http://msdn.microsoft.com/en-us/library/ms649021%28v=vs.85%29.aspx
        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        // See http://msdn.microsoft.com/en-us/library/ms632599%28VS.85%29.aspx#message_only
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);
    }
}