using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.ComponentModel;
using MotionDetector.Views;
using MotionDetector.Utilities;
using MotionDetector.Models;

namespace MotionDetector
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private TutorialModel _tutorialModels = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var logPath = System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "mainpage_loaded.txt");
                System.IO.File.WriteAllText(logPath, "MainPage_Loaded started at " + DateTime.Now);
            }
            catch { }

            MainDisplayFrame.Navigate(typeof(DashboardPage));

            try
            {
                await StoreServices.SetupStoreServices();
                StoreServices.CheckFreemiumStatus();
                StoreServices.CheckForPremiumStatus();
                if (StoreServices.IsPremium)
                    StoreServices.RemoveAds = true;
                _tutorialModels = ConfigurationServices.GetTutorialLinks();
            }
            catch (Exception ex)
            {
                try
                {
                    var logPath = System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "mainpage_crash.txt");
                    System.IO.File.WriteAllText(logPath, "Exception in MainPage_Loaded: " + ex);
                }
                catch { }
            }

            UpdatePremiumNavState();
            this.DataContext = this;

            try { await RatingNagServices.ShowNagIfNeededAsync(); } catch { }
        }

        public void SetNavigationHidden(bool hidden)
        {
            if (NavigationPane == null) return;
            if (hidden)
            {
                NavigationPane.IsPaneOpen = false;
                NavigationPane.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                NavigationPane.IsPaneToggleButtonVisible = false;
            }
            else
            {
                NavigationPane.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
                NavigationPane.IsPaneToggleButtonVisible = true;
                NavigationPane.IsPaneOpen = true;
            }
        }

        private void SetPremiumItem(NavigationViewItem item, bool premium)
        {
            if (item == null) return;
            item.IsEnabled = premium;
        }

        private void UpdatePremiumNavState()
        {
            bool premium = StoreServices.IsPremium;
            bool adsRemoved = StoreServices.RemoveAds || premium;

            if (NavRemoveAds != null)
            {
                NavRemoveAds.IsEnabled = !adsRemoved;
                NavRemoveAds.Content = adsRemoved ? "Ads Removed" : "Remove Ads - $0.99";
            }
            if (NavUnlockPremium != null)
            {
                NavUnlockPremium.IsEnabled = !premium;
                NavUnlockPremium.Content = premium ? "Premium Unlocked" : "Unlock Premium Features- $1.99";
            }

            SetPremiumItem(NavHiddenMode, premium);
            SetPremiumItem(NavAlertSounds, premium);
            SetPremiumItem(NavHistory, premium);
            SetPremiumItem(NavSchedule, premium);
            SetPremiumItem(NavZones, premium);
            SetPremiumItem(NavClips, premium);
            SetPremiumItem(NavDiscord, premium);
            SetPremiumItem(NavMultiCam, premium);
            SetPremiumItem(NavNight, premium);
            SetPremiumItem(NavQuiet, premium);
            SetPremiumItem(NavPin, premium);
            SetPremiumItem(NavHealth, premium);
            SetPremiumItem(NavNightRecipient, premium);
        }

        private async void NavigationPane_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            string item = args.InvokedItem as string;
            if (string.IsNullOrEmpty(item) && args.InvokedItem is NavigationViewItem nvi)
                item = nvi.Content as string;
            if (string.IsNullOrEmpty(item))
                return;
            item = item.Trim();

            if (item.StartsWith("Remove Ads") || item == "Ads Removed")
            {
                if (!StoreServices.RemoveAds && !StoreServices.IsPremium)
                {
                    await StoreServices.OpenStoreRemoveAds();
                    UpdatePremiumNavState();
                }
                return;
            }

            if (item.StartsWith("Unlock Premium") || item == "Premium Unlocked")
            {
                if (!StoreServices.IsPremium)
                {
                    await StoreServices.OpenStorePurchasePremium();
                    UpdatePremiumNavState();
                }
                return;
            }

            switch (item)
            {
                case "Dashboard":
                    SetNavigationHidden(false);
                    if (MainDisplayFrame.CurrentSourcePageType != typeof(DashboardPage))
                        MainDisplayFrame.Navigate(typeof(DashboardPage));
                    break;

                case "Hidden Mode":
                    if (StoreServices.IsPremium)
                    {
                        SetNavigationHidden(true);
                        MainDisplayFrame.Navigate(typeof(HiddenModeDashboardPage));
                    }
                    break;

                case "Alert Sounds":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(AlertSoundsPage));
                    break;

                case "Alert History":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(AlertHistoryPage));
                    break;

                case "Active Hours Schedule":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(PremiumFeaturePage), "schedule");
                    break;
                case "Named Zones & Smart Filter":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(PremiumFeaturePage), "zones");
                    break;
                case "Alert Clips & Buffer":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(PremiumFeaturePage), "clips");
                    break;
                case "Discord / IFTTT Alerts":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(PremiumFeaturePage), "integrations");
                    break;
                case "Multi-Camera":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(PremiumFeaturePage), "multicam");
                    break;
                case "Night Vision Preset":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(PremiumFeaturePage), "night");
                    break;
                case "Quiet Sound Hours":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(PremiumFeaturePage), "quiet");
                    break;
                case "Settings PIN Lock":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(PremiumFeaturePage), "pin");
                    break;
                case "Health Check":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(PremiumFeaturePage), "health");
                    break;
                case "Night Recipient Rules":
                    if (StoreServices.IsPremium)
                        MainDisplayFrame.Navigate(typeof(PremiumFeaturePage), "nightrecipient");
                    break;

                case "Documentation":
                    MainDisplayFrame.Navigate(typeof(DocumentationPage));
                    break;

                default:
                    if (args.InvokedItem is NavigationViewItem navItem &&
                        (navItem.Content as string) == "Settings")
                    {
                        MainDisplayFrame.Navigate(typeof(SettingsPage));
                    }
                    break;
            }
        }

        public void OnPropertyChanged(String prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
