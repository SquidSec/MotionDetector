using MotionDetector.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MotionDetector.Views
{
    public sealed partial class PremiumFeaturePage : Page
    {
        public PremiumFeaturePage()
        {
            this.InitializeComponent();
            this.DataContext = new PremiumSettingsViewModel();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string key = e.Parameter as string ?? "";
            HideAll();
            switch (key)
            {
                case "schedule":
                    PageTitle.Text = "Active Hours Schedule";
                    PanelSchedule.Visibility = Visibility.Visible;
                    break;
                case "zones":
                    PageTitle.Text = "Named Zones & Smart Filter";
                    PanelZones.Visibility = Visibility.Visible;
                    break;
                case "clips":
                    PageTitle.Text = "Alert Clips & Buffer";
                    PanelClips.Visibility = Visibility.Visible;
                    break;
                case "integrations":
                    PageTitle.Text = "Discord / IFTTT Alerts";
                    PanelIntegrations.Visibility = Visibility.Visible;
                    break;
                case "multicam":
                    PageTitle.Text = "Multi-Camera";
                    PanelMultiCam.Visibility = Visibility.Visible;
                    break;
                case "night":
                    PageTitle.Text = "Night Vision Preset";
                    PanelNight.Visibility = Visibility.Visible;
                    break;
                case "quiet":
                    PageTitle.Text = "Quiet Sound Hours";
                    PanelQuiet.Visibility = Visibility.Visible;
                    break;
                case "pin":
                    PageTitle.Text = "Settings PIN Lock";
                    PanelPin.Visibility = Visibility.Visible;
                    break;
                case "health":
                    PageTitle.Text = "Health Check";
                    PanelHealth.Visibility = Visibility.Visible;
                    break;
                case "nightrecipient":
                    PageTitle.Text = "Night Recipient Rules";
                    PanelNightRecipient.Visibility = Visibility.Visible;
                    break;
                default:
                    PageTitle.Text = "Premium Feature";
                    break;
            }
        }

        private void HideAll()
        {
            PanelSchedule.Visibility = Visibility.Collapsed;
            PanelZones.Visibility = Visibility.Collapsed;
            PanelClips.Visibility = Visibility.Collapsed;
            PanelIntegrations.Visibility = Visibility.Collapsed;
            PanelMultiCam.Visibility = Visibility.Collapsed;
            PanelNight.Visibility = Visibility.Collapsed;
            PanelQuiet.Visibility = Visibility.Collapsed;
            PanelPin.Visibility = Visibility.Collapsed;
            PanelHealth.Visibility = Visibility.Collapsed;
            PanelNightRecipient.Visibility = Visibility.Collapsed;
        }
    }
}
