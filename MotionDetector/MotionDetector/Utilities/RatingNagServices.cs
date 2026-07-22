using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;

namespace MotionDetector.Utilities
{
    public static class RatingNagServices
    {
        private const string KeyDontShow = "RatingNag_DontShowAgain";
        private const string KeyReviewed = "RatingNag_HasReviewed";
        private static readonly Random _rng = new Random();

        public static bool ShouldShowNag()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                if (settings.ContainsKey(KeyDontShow) && settings[KeyDontShow] is bool d && d)
                    return false;
                if (settings.ContainsKey(KeyReviewed) && settings[KeyReviewed] is bool r && r)
                    return false;
                // ~40% chance each launch
                return _rng.NextDouble() < 0.40;
            }
            catch { return false; }
        }

        public static async Task ShowNagIfNeededAsync()
        {
            if (!ShouldShowNag()) return;

            var dialog = new MessageDialog(
                "Enjoying Universal Motion Detector Elite?\n\nA quick rating on the Microsoft Store helps other people find the app and keeps free updates coming.",
                "Rate this app?");

            var review = new UICommand("Rate on Store", async _ =>
            {
                try
                {
                    ApplicationData.Current.LocalSettings.Values[KeyReviewed] = true;
                    // Generic Store product page protocol; works when published
                    // Uses installed package family name when available
                    string pfn = Windows.ApplicationModel.Package.Current.Id.FamilyName;
                    await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?PFN=" + pfn));
                }
                catch
                {
                    try
                    {
                        await Launcher.LaunchUriAsync(new Uri("ms-windows-store://home"));
                    }
                    catch { }
                }
            });
            var later = new UICommand("Maybe later");
            var never = new UICommand("Don't show again", _ =>
            {
                ApplicationData.Current.LocalSettings.Values[KeyDontShow] = true;
            });

            dialog.Commands.Add(review);
            dialog.Commands.Add(later);
            dialog.Commands.Add(never);
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;

            await dialog.ShowAsync();
        }
    }
}
