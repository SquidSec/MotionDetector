using MotionDetector.Models;
using MotionDetector.Utilities;
using MotionDetector.ViewModels;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MotionDetector.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private TutorialModel _tutorialModels = null;
        public SettingsPage()
        {
            this.InitializeComponent();
            this.DataContext = new SettingsViewModel();
            this.Loaded += SettingsPage_Loaded;
            this.LostFocus += SettingsPage_LostFocus;
        }

        private void SettingsPage_LostFocus(object sender, RoutedEventArgs e)
        {
            // Persist when any field loses focus (covers PasswordBox and default TextBox binding)
            try
            {
                if (this.DataContext is SettingsViewModel vm && vm.ConfigurationSettings != null)
                    ConfigurationServices.SaveConfig(vm.ConfigurationSettings);
            }
            catch { }
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _tutorialModels = ConfigurationServices.GetTutorialLinks();
        }

        private async void BrowseToYoutubeTutorial(object sender, RoutedEventArgs e)
        {
            Uri youtubeTutorial;
            if (_tutorialModels != null)
            {
                youtubeTutorial = new Uri(_tutorialModels.TutorialLinkTwo);
            }
            else
            {
                youtubeTutorial = new Uri(@"https://youtu.be/EpaH1thk4IA");
            }
            await Windows.System.Launcher.LaunchUriAsync(youtubeTutorial);
        }

        private async void BrowseToGmailSecurity(object sender, RoutedEventArgs e)
        {
            // Modern Gmail requires 2FA + App Passwords (the old "less secure apps" is gone)
            Uri gmailSecurityUri = new Uri(@"https://myaccount.google.com/apppasswords");            
            await Windows.System.Launcher.LaunchUriAsync(gmailSecurityUri);
        }

        private void OpenDocumentation_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Frame?.Navigate(typeof(DocumentationPage));
        }
    }
}
