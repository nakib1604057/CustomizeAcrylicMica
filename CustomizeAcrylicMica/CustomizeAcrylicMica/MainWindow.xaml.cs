using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using WinRT;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CustomizeAcrylicMica.Helpers;
using static CustomizeAcrylicMica.Helpers.Win32;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CustomizeAcrylicMica
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private WindowsSystemDispatcherQueueHelper? m_wsdqHelper;
        private POINT? minWindowSize = null;
        private POINT? maxWindowSize = null;
        private static WinProc newWndProc = null;
        private static IntPtr oldWndProc = IntPtr.Zero;
        // See separate sample below for implementation
        private BackdropType currentBackdrop;
        Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController acrylicController;
        Microsoft.UI.Composition.SystemBackdrops.MicaController micaController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;
        Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController m_acrylicController;


        private SystemBackdropConfiguration? configurationSource;
        public double TintOpacityValue { get; set; }
        public double TintLimonacityValue { get; set; }
        public MainWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            //this.SystemBackdrop = new DesktopAcrylicBackdrop();
            SetWindowMaxMinSize(new POINT() { x = 500, y = 500 });
            m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();
            TintOpacityValue = 0.0;
            TintLimonacityValue = 0.1;
            SetBackdrop(BackdropType.Acrylic);
            currentBackdrop = BackdropType.Acrylic;

        }
        public enum BackdropType
        {
            None,
            Mica,
            MicaAlt,
            Acrylic,
            AcrylicThin
        }
        public void SetBackdrop(BackdropType type)
        {
            // Reset to default color. If the requested type is supported, we'll update to that.
            // Note: This sample completely removes any previous controller to reset to the default
            //       state. This is done so this sample can show what is expected to be the most
            //       common pattern of an app simply choosing one controller type which it sets at
            //       startup. If an app wants to toggle between Mica and Acrylic it could simply
            //       call RemoveSystemBackdropTarget() on the old controller and then setup the new
            //       controller, reusing any existing m_configurationSource and Activated/Closed
            //       event handlers.

            //Reset the backdrop


            micaController?.Dispose();
            micaController = null;
            m_acrylicController?.Dispose();
            m_acrylicController = null;
            configurationSource = null;

            //Set the backdrop
            if (type == BackdropType.Mica)
            {
                if (TrySetMicaBackdrop(false))
                {

                }

                else
                {
                    // Mica isn't supported. Try Acrylic.
                    type = BackdropType.Acrylic;
                    //tbChangeStatus.Text += "  Mica isn't supported. Trying Acrylic.";
                }
            }
            if (type == BackdropType.MicaAlt)
            {
                if (TrySetMicaBackdrop(true))
                {

                }

                else
                {
                    // MicaAlt isn't supported. Try Acrylic.
                    type = BackdropType.Acrylic;
                    //tbChangeStatus.Text += "  MicaAlt isn't supported. Trying Acrylic.";
                }
            }
            if (type == BackdropType.Acrylic)
            {
                if (TrySetAcrylicBackdrop(false))
                {

                }
                //currentBackdrop = type;
                else
                {
                    // Acrylic isn't supported, so take the next option, which is DefaultColor, which is already set.
                    //tbChangeStatus.Text += "  Acrylic Base isn't supported. Switching to default color.";
                }
            }
            if (type == BackdropType.AcrylicThin)
            {
                if (TrySetAcrylicBackdrop(true))
                {

                }
                //currentBackdrop = type;
                else
                {
                    // Acrylic isn't supported, so take the next option, which is DefaultColor, which is already set.
                    //tbChangeStatus.Text += "  Acrylic Thin isn't supported. Switching to default color.";
                }
            }

            //Fix the none backdrop
            SetNoneBackdropBackground();

            //Announce visual change to automation
            // UIHelper.AnnounceActionForAccessibility(backdropComboBox, $"Background changed to {currentBackdrop}", "BackgroundChangedNotificationActivityId");
        }
        //Fixes the background color not changing when switching between themes.
        void SetNoneBackdropBackground()
        {
            //if (currentBackdrop == BackdropType.None && themeComboBox.SelectedIndex != 0)
            //    ((Grid)Content).Background = new SolidColorBrush(themeComboBox.SelectedIndex == 1 ? Colors.White : Colors.Black);
            //else
            ((Grid)Content).Background = new SolidColorBrush(Colors.Transparent);
        }
        bool TrySetAcrylicBackdrop(bool useAcrylicThin)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {


                // Hooking up the policy object
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_acrylicController = new Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController();


                m_acrylicController.TintOpacity = (float)TintOpacityValue;
                m_acrylicController.TintColor = new Windows.UI.Color()
                {
                    R = ColorPicker.Color.R,
                    G = ColorPicker.Color.G,
                    B = ColorPicker.Color.B,
                    A = ColorPicker.Color.A
                };
                m_acrylicController.LuminosityOpacity = (float)TintLimonacityValue;
                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                m_acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // Succeeded.
            }

            return false; // Acrylic is not supported on this system.
        }



        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }
            this.Activated -= Window_Activated;
        }

        bool TrySetMicaBackdrop(bool useMicaAlt)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {


                // Hooking up the policy object
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

                micaController.Kind = useMicaAlt ? Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt : Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base;

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                micaController.TintOpacity = (float)TintOpacityValue;
                micaController.LuminosityOpacity = (float)TintLimonacityValue;
                micaController.TintColor = new Windows.UI.Color()
                {
                    R = ColorPicker.Color.R,
                    G = ColorPicker.Color.G,
                    B = ColorPicker.Color.B,
                    A = ColorPicker.Color.A
                };
                micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                micaController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // Succeeded.
            }

            return false; // Mica is not supported on this system.
        }



        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }












        void SetWindowMaxMinSize(POINT? minWindowSize = null, POINT? maxWindowSize = null)
        {
            try
            {
                this.minWindowSize = minWindowSize;
                this.maxWindowSize = maxWindowSize;
                var hwnd = GetWindowHandleForCurrentWindow(this);

                newWndProc = new WinProc(WndProc);
                oldWndProc = SetWindowLongPtr(hwnd, WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
            }
            catch (Exception ex)
            {

            }
        }
        private IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, newProc));
        }
        private IntPtr WndProc(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam)
        {
            switch (Msg)
            {
                case WindowMessage.WM_GETMINMAXINFO:
                    var dpi = GetDpiForWindow(hWnd);
                    var scalingFactor = (float)dpi / 96;

                    var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    if (minWindowSize != null)
                    {
                        minMaxInfo.ptMinTrackSize.x = (int)(minWindowSize.Value.x * scalingFactor);
                        minMaxInfo.ptMinTrackSize.y = (int)(minWindowSize.Value.y * scalingFactor);
                    }
                    if (maxWindowSize != null)
                    {
                        minMaxInfo.ptMaxTrackSize.x = (int)(maxWindowSize.Value.x * scalingFactor);
                        minMaxInfo.ptMaxTrackSize.y = (int)(maxWindowSize.Value.y * scalingFactor);
                    }

                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

            }
            return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
        }
        private static IntPtr GetWindowHandleForCurrentWindow(object target) =>
                WinRT.Interop.WindowNative.GetWindowHandle(target);
        [StructLayout(LayoutKind.Sequential)]

        internal struct POINT
        {
            public int x;
            public int y;
        }
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        private void backdropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (backdropComboBox.SelectedIndex == 0)
            {
                SetBackdrop(BackdropType.Mica);
            }
            else
            {
                SetBackdrop(BackdropType.Acrylic);

            }

        }

        private void themeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TintOpacity_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                TintOpacityValue = slider.Value / 10.0;
                SetBackdrop(currentBackdrop);
            }
        }

        private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            ColorPicker element = sender as ColorPicker;
            if (element != null)
            {
                SetBackdrop(currentBackdrop);
                //var color = ColorPicker.GetColor();
                //int A = (color >> 24) & 0xff; // or color >>> 24
                //int R = (color >> 16) & 0xff;
                //int G = (color >> 8) & 0xff;
                //int B = (color) & 0xff;
            }
        }

        private void TimtLimonacity_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {

            Slider slider = sender as Slider;
            if (slider != null)
            {
                TintLimonacityValue = slider.Value / 10.0;
                SetBackdrop(currentBackdrop);
            }
        }
    }
}
