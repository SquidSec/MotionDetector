using MotionDetector.Utilities;
using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MotionDetector.Views
{
    public sealed partial class AlertHistoryPage : Page
    {
        public AlertHistoryPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!StoreServices.IsPremium)
            {
                StatusText.Text = "Premium required - unlock to use history.";
                await StoreServices.OpenStorePurchasePremium();
            }
            await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            var list = await AlertHistoryServices.LoadAsync();
            HistoryList.ItemsSource = list;
            StatusText.Text = list.Count + " events";
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadAsync();
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (!StoreServices.IsPremium)
            {
                await StoreServices.OpenStorePurchasePremium();
                return;
            }
            try
            {
                var path = await AlertHistoryServices.ExportCsvAsync();
                StatusText.Text = "Exported: " + path;
                await new MessageDialog("CSV saved to:\n" + path, "Export complete").ShowAsync();
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.Message;
            }
        }
    }
}
