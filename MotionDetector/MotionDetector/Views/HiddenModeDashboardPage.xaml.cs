using System;
using System.ComponentModel;
using MotionDetector;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace MotionDetector.Views
{
    public sealed partial class HiddenModeDashboardPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public HiddenModeDashboardPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            DataContext = this;

            if (Window.Current.Content is Frame root && root.Content is MainPage main)
                main.SetNavigationHidden(true);

            try
            {
                // Improve compatibility where supported
                webBrowser.Settings.IsJavaScriptEnabled = true;
                webBrowser.Settings.IsIndexedDBEnabled = true;
            }
            catch { }

            hiddenDashboard.Navigate(typeof(DashboardPage), false);
            NavigateTo("https://www.bing.com");
            UpdateNavButtons();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (Window.Current.Content is Frame root && root.Content is MainPage main)
                main.SetNavigationHidden(false);

            try { hiddenDashboard.Navigate(typeof(BlankPage)); } catch { }
            DataContext = null;
            base.OnNavigatedFrom(e);
        }

        private void WebBrowser_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            ShowStatus("Loading...");
            if (args.Uri != null)
                AddressTextBox.Text = args.Uri.AbsoluteUri;
        }

        private void WebBrowser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            try
            {
                if (webBrowser.Source != null)
                    AddressTextBox.Text = webBrowser.Source.AbsoluteUri;
            }
            catch { }

            UpdateNavButtons();

            if (args.IsSuccess)
                HideStatus();
            else
                ShowStatus("Page failed to load. Try a full address like https://example.com");
        }

        private void WebBrowser_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            ShowStatus("Navigation failed: " + e.WebErrorStatus);
            UpdateNavButtons();
        }

        private void WebBrowser_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            // Open popups/new windows in the same browser control
            args.Handled = true;
            if (args.Uri != null)
                NavigateTo(args.Uri.AbsoluteUri);
        }

        private void AddressTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                NavigateTo(AddressTextBox.Text);
            }
        }

        private void NavigateGo_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(AddressTextBox.Text);
        }

        private void BrowserBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (webBrowser != null && webBrowser.CanGoBack)
                    webBrowser.GoBack();
            }
            catch { }
            UpdateNavButtons();
        }

        private void BrowserForward_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (webBrowser != null && webBrowser.CanGoForward)
                    webBrowser.GoForward();
            }
            catch { }
            UpdateNavButtons();
        }

        private void BrowserRefresh_Click(object sender, RoutedEventArgs e)
        {
            try { webBrowser?.Refresh(); } catch { }
        }

        private void ExitHiddenMode_Click(object sender, RoutedEventArgs e)
        {
            if (Window.Current.Content is Frame root && root.Content is MainPage main)
                main.SetNavigationHidden(false);

            if (Frame != null && Frame.CanGoBack)
                Frame.GoBack();
            else if (Frame != null)
                Frame.Navigate(typeof(DashboardPage));
        }

        private void NavigateTo(string input)
        {
            if (webBrowser == null) return;

            string url = BuildNavigateUrl(input);
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                AddressTextBox.Text = url;
                var uri = new Uri(url);
                webBrowser.Navigate(uri);
            }
            catch (Exception ex)
            {
                ShowStatus("Could not open address: " + ex.Message);
            }

            UpdateNavButtons();
        }

        private static string BuildNavigateUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "https://www.bing.com";

            string text = input.Trim();

            if (Uri.TryCreate(text, UriKind.Absolute, out Uri abs) &&
                (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps))
            {
                return abs.AbsoluteUri;
            }

            // www.example.com or example.com/path
            bool looksLikeHost =
                text.Contains(".") &&
                !text.Contains(" ") &&
                !text.StartsWith("?", StringComparison.Ordinal);

            if (looksLikeHost)
            {
                string withScheme = text.StartsWith("//", StringComparison.Ordinal)
                    ? "https:" + text
                    : "https://" + text.TrimStart('/');

                if (Uri.TryCreate(withScheme, UriKind.Absolute, out Uri hostUri) &&
                    (hostUri.Scheme == Uri.UriSchemeHttp || hostUri.Scheme == Uri.UriSchemeHttps) &&
                    !string.IsNullOrEmpty(hostUri.Host))
                {
                    return hostUri.AbsoluteUri;
                }
            }

            return "https://www.bing.com/search?q=" + Uri.EscapeDataString(text);
        }

        private void UpdateNavButtons()
        {
            try
            {
                if (BtnBack != null) BtnBack.IsEnabled = webBrowser != null && webBrowser.CanGoBack;
                if (BtnForward != null) BtnForward.IsEnabled = webBrowser != null && webBrowser.CanGoForward;
            }
            catch { }
        }

        private void ToggleHiddenDashboard(object sender, RoutedEventArgs e)
        {
            if (hiddenDashboard.Visibility == Visibility.Visible)
            {
                webBrowser.Visibility = Visibility.Visible;
                hiddenDashboard.Visibility = Visibility.Collapsed;
            }
            else
            {
                webBrowser.Visibility = Visibility.Collapsed;
                hiddenDashboard.Visibility = Visibility.Visible;
            }
        }

        private void ShowStatus(string msg)
        {
            if (BrowserStatusText == null || BrowserStatusBar == null) return;
            BrowserStatusText.Text = msg ?? "";
            BrowserStatusBar.Visibility = Visibility.Visible;
        }

        private void HideStatus()
        {
            if (BrowserStatusBar != null)
                BrowserStatusBar.Visibility = Visibility.Collapsed;
        }

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
