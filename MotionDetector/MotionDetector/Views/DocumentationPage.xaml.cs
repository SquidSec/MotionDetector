using MotionDetector.Utilities;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

namespace MotionDetector.Views
{
    public sealed partial class DocumentationPage : Page
    {
        public bool ShowPremiumUpsell { get; private set; } = true;
        public bool ShowPremiumOwned { get; private set; }

        public DocumentationPage()
        {
            RefreshPremiumState();
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            RefreshPremiumState();
            Bindings.Update();
        }

        private void RefreshPremiumState()
        {
            ShowPremiumOwned = StoreServices.IsPremium;
            ShowPremiumUpsell = !StoreServices.IsPremium;
        }

        private void ToggleSection_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is ToggleButton tb) || tb.Tag == null) return;
            var panel = this.FindName(tb.Tag.ToString()) as UIElement;
            if (panel == null) return;
            panel.Visibility = tb.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void BuyPremium_Click(object sender, RoutedEventArgs e)
        {
            await StoreServices.OpenStorePurchasePremium();
            RefreshPremiumState();
            Bindings.Update();
        }

        private async void RemoveAds_Click(object sender, RoutedEventArgs e)
        {
            await StoreServices.OpenStoreRemoveAds();
            RefreshPremiumState();
            Bindings.Update();
        }
    }
}
