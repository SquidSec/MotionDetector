using System;
using MotionDetector.Models;
using MotionDetector.Utilities;
using MotionDetector.ViewModels;
using System.Windows.Input;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace MotionDetector.Views
{
    public sealed partial class DashboardPage : Page
    {
        private DispatcherTimer _adRotateTimer;
        private string _currentAdUrl = "https://squidscanner.com";

        public ICommand InitializeCaptureSinkCommand
        {
            get { return (ICommand)GetValue(InitializeCaptureSinkProperty); }
            set { SetValue(InitializeCaptureSinkProperty, value); }
        }

        public ICommand SaveImageCommand
        {
            get { return (ICommand)GetValue(SaveImageCommandProperty); }
            set { SetValue(SaveImageCommandProperty, value); }
        }

        public AlertDisplayImageModel SelectedImage
        {
            get { return (AlertDisplayImageModel)GetValue(SelectedImageProperty); }
            set { SetValue(SelectedImageProperty, value); }
        }

        public static readonly DependencyProperty SelectedImageProperty =
            DependencyProperty.Register(nameof(SelectedImage), typeof(AlertDisplayImageModel), typeof(DashboardPage), new PropertyMetadata(null));

        public static readonly DependencyProperty SaveImageCommandProperty =
            DependencyProperty.Register(nameof(SaveImageCommand), typeof(ICommand), typeof(DashboardPage), new PropertyMetadata(null));

        public static readonly DependencyProperty InitializeCaptureSinkProperty =
            DependencyProperty.Register(nameof(InitializeCaptureSinkCommand), typeof(ICommand), typeof(DashboardPage), new PropertyMetadata(null));

        public DashboardPage()
        {
            this.InitializeComponent();
        }

        private void ApplyAd(PromoAd ad)
        {
            if (ad == null || AdTitle == null) return;
            _currentAdUrl = ad.Url;
            AdTitle.Text = ad.Title;
            AdSubtitle.Text = ad.Subtitle;
            AdCtaButton.Content = ad.Cta;
            try
            {
                AdImage.Source = new BitmapImage(new Uri(ad.ImagePath));
            }
            catch { }
            try
            {
                var hex = (ad.AccentHex ?? "#00ff9f").TrimStart('#');
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                var brush = new SolidColorBrush(Color.FromArgb(255, r, g, b));
                SquidAdBanner.BorderBrush = brush;
                AdTitle.Foreground = brush;
                AdCtaButton.Background = brush;
            }
            catch { }
        }

        private async void SquidAd_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try { await Windows.System.Launcher.LaunchUriAsync(new Uri(_currentAdUrl)); } catch { }
        }

        private async void SquidAd_Click(object sender, RoutedEventArgs e)
        {
            try { await Windows.System.Launcher.LaunchUriAsync(new Uri(_currentAdUrl)); } catch { }
        }

        private void UpdateAdVisibility()
        {
            if (SquidAdBanner == null || MainContentGrid == null) return;
            bool hide = StoreServices.RemoveAds || StoreServices.IsPremium;
            SquidAdBanner.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
            // Reclaim ad space when ads are off
            MainContentGrid.Margin = hide
                ? new Thickness(0, 48, 0, 0)
                : new Thickness(0, 48, 0, 168);
            if (!hide)
            {
                ApplyAd(PromoAdServices.Random());
                if (_adRotateTimer == null)
                {
                    _adRotateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                    _adRotateTimer.Tick += (s, ev) => ApplyAd(PromoAdServices.Next());
                }
                _adRotateTimer.Interval = TimeSpan.FromSeconds(10);
                _adRotateTimer.Start();
            }
            else
            {
                _adRotateTimer?.Stop();
            }
        }

        private void DashboardPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            SetBinding(InitializeCaptureSinkProperty, new Binding() { Path = new PropertyPath("InitializeCaptureSinkCommand") });
            SetBinding(SaveImageCommandProperty, new Binding() { Path = new PropertyPath("SaveImageCommand") });
            SetBinding(SelectedImageProperty, new Binding() { Path = new PropertyPath("SelectedAlertImage"), Mode = BindingMode.TwoWay });
        }

        private void Current_EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            InitializeCaptureSinkCommand?.Execute(sender);
        }
        private void Current_LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            InitializeCaptureSinkCommand?.Execute(sender);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                var logPath = System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "dashboard_nav.txt");
                System.IO.File.WriteAllText(logPath, "Dashboard OnNavigatedTo at " + DateTime.Now);
            }
            catch { }

            App.Current.LeavingBackground += Current_LeavingBackground;
            App.Current.EnteredBackground += Current_EnteredBackground;
            this.DataContextChanged += DashboardPage_DataContextChanged;
            MotionDetectorViewModel viewModel = new MotionDetectorViewModel();
            this.DataContext = viewModel;

            await viewModel.Setup();
            viewModel.caputureSink.HorizontalAlignment = HorizontalAlignment.Stretch;
            viewModel.caputureSink.VerticalAlignment = VerticalAlignment.Stretch;
            this.captureElementControl.Content = viewModel.caputureSink;

            viewModel.secondaryCaptureSink.HorizontalAlignment = HorizontalAlignment.Stretch;
            viewModel.secondaryCaptureSink.VerticalAlignment = VerticalAlignment.Stretch;
            this.secondaryCaptureElementControl.Content = viewModel.secondaryCaptureSink;
            UpdateSecondaryCameraLayout(viewModel);

            viewModel.PropertyChanged += (s, ev) =>
            {
                if (ev.PropertyName == nameof(MotionDetectorViewModel.ShowSecondaryCamera))
                    UpdateSecondaryCameraLayout(viewModel);
            };

            UpdateAdVisibility();
        }

        private void UpdateSecondaryCameraLayout(MotionDetectorViewModel viewModel)
        {
            if (viewModel == null || SecondaryCameraBorder == null || SecondaryRow == null) return;
            if (viewModel.ShowSecondaryCamera)
            {
                SecondaryCameraBorder.Visibility = Visibility.Visible;
                SecondaryRow.Height = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                SecondaryCameraBorder.Visibility = Visibility.Collapsed;
                SecondaryRow.Height = new GridLength(0);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _adRotateTimer?.Stop();
            captureElementControl.Content = null;
            (this.DataContext as MotionDetectorViewModel)?.Destroyer();
            this.DataContext = null;
            App.Current.LeavingBackground -= Current_LeavingBackground;
            App.Current.EnteredBackground -= Current_EnteredBackground;
            this.DataContextChanged -= DashboardPage_DataContextChanged;
            this.DataContext = null;
            base.OnNavigatedFrom(e);
        }

        private void OnSaveContextMenuClicked(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem mfi && mfi.DataContext is AlertDisplayImageModel fromFlyout)
                SelectedImage = fromFlyout;

            if (SelectedImage == null && alertImageListView?.SelectedItem is AlertDisplayImageModel selected)
                SelectedImage = selected;

            SaveImageCommand?.Execute(SelectedImage);
        }

        private void alertImageListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            DependencyObject src = e.OriginalSource as DependencyObject;
            while (src != null && !(src is ListViewItem))
                src = Windows.UI.Xaml.Media.VisualTreeHelper.GetParent(src);

            AlertDisplayImageModel model = null;
            if (src is ListViewItem lvi)
            {
                model = lvi.Content as AlertDisplayImageModel
                    ?? (lvi.DataContext as AlertDisplayImageModel);
            }
            if (model == null && e.OriginalSource is FrameworkElement fe)
                model = fe.DataContext as AlertDisplayImageModel;

            if (model != null)
            {
                SelectedImage = model;
                alertImageListView.SelectedItem = model;
            }
        }
    }
}
