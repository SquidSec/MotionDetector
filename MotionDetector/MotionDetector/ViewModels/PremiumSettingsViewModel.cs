using BasecodeLibrary.Utilities;
using Models.MotionDetector;
using MotionDetector.Models;
using MotionDetector.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Enumeration;

namespace MotionDetector.ViewModels
{
    public class PremiumSettingsViewModel : ViewModelBase
    {
        private ConfigModel _config;
        private string _status;
        private VideoDeviceModel _secondaryCamera;
        private TimeSpan _scheduleStart = TimeSpan.Zero;
        private TimeSpan _scheduleEnd = new TimeSpan(23, 59, 0);
        private TimeSpan _quietStart = TimeSpan.Zero;
        private TimeSpan _quietEnd = TimeSpan.Zero;
        private bool _quietEnabled;

        public ConfigModel ConfigurationSettings
        {
            get => _config;
            set { _config = value; OnPropertyChanged(nameof(ConfigurationSettings)); }
        }

        public ObservableCollection<VideoDeviceModel> AvailableCameras { get; } = new ObservableCollection<VideoDeviceModel>();

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public TimeSpan ScheduleStartTime
        {
            get => _scheduleStart;
            set
            {
                _scheduleStart = value;
                OnPropertyChanged(nameof(ScheduleStartTime));
                if (_config?.AppConfig != null)
                {
                    _config.AppConfig.ScheduleStartMinutes = (int)value.TotalMinutes;
                    Save();
                }
            }
        }

        public TimeSpan ScheduleEndTime
        {
            get => _scheduleEnd;
            set
            {
                _scheduleEnd = value;
                OnPropertyChanged(nameof(ScheduleEndTime));
                if (_config?.AppConfig != null)
                {
                    _config.AppConfig.ScheduleEndMinutes = (int)value.TotalMinutes;
                    Save();
                }
            }
        }

        public bool QuietEnabled
        {
            get => _quietEnabled;
            set
            {
                _quietEnabled = value;
                OnPropertyChanged(nameof(QuietEnabled));
                if (_config?.AppConfig == null) return;
                if (!value)
                {
                    _config.AppConfig.DaySoundQuietHourStart = -1;
                    _config.AppConfig.DaySoundQuietHourEnd = -1;
                }
                else
                {
                    _config.AppConfig.DaySoundQuietHourStart = QuietStartTime.Hours;
                    _config.AppConfig.DaySoundQuietHourEnd = QuietEndTime.Hours;
                }
                Save();
            }
        }

        public TimeSpan QuietStartTime
        {
            get => _quietStart;
            set
            {
                _quietStart = value;
                OnPropertyChanged(nameof(QuietStartTime));
                if (_config?.AppConfig != null && QuietEnabled)
                {
                    _config.AppConfig.DaySoundQuietHourStart = value.Hours;
                    Save();
                }
            }
        }

        public TimeSpan QuietEndTime
        {
            get => _quietEnd;
            set
            {
                _quietEnd = value;
                OnPropertyChanged(nameof(QuietEndTime));
                if (_config?.AppConfig != null && QuietEnabled)
                {
                    _config.AppConfig.DaySoundQuietHourEnd = value.Hours;
                    Save();
                }
            }
        }

        public VideoDeviceModel SecondaryCameraModel
        {
            get => _secondaryCamera;
            set
            {
                _secondaryCamera = value;
                OnPropertyChanged(nameof(SecondaryCameraModel));
                if (_config?.AppConfig != null)
                {
                    _config.AppConfig.SecondaryCameraId = value?.DeviceId ?? "";
                    Save();
                }
            }
        }

        public ICommand TestDiscordCommand { get; private set; }
        public ICommand TestIftttCommand { get; private set; }

        public PremiumSettingsViewModel()
        {
            TestDiscordCommand = new CommandHandler(() => { _ = TestDiscordAsync(); });
            TestIftttCommand = new CommandHandler(() => { _ = TestIftttAsync(); });
            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            ConfigurationSettings = await ConfigurationServices.GetConfig();
            if (ConfigurationSettings?.AppConfig != null)
            {
                ConfigurationSettings.AppConfig.PropertyChanged += (s, e) => Save();
                var a = ConfigurationSettings.AppConfig;
                _scheduleStart = TimeSpan.FromMinutes(Math.Max(0, Math.Min(1439, a.ScheduleStartMinutes)));
                _scheduleEnd = TimeSpan.FromMinutes(Math.Max(0, Math.Min(1439, a.ScheduleEndMinutes)));
                OnPropertyChanged(nameof(ScheduleStartTime));
                OnPropertyChanged(nameof(ScheduleEndTime));

                if (a.DaySoundQuietHourStart >= 0 && a.DaySoundQuietHourEnd >= 0)
                {
                    _quietEnabled = true;
                    _quietStart = TimeSpan.FromHours(a.DaySoundQuietHourStart);
                    _quietEnd = TimeSpan.FromHours(a.DaySoundQuietHourEnd);
                }
                OnPropertyChanged(nameof(QuietEnabled));
                OnPropertyChanged(nameof(QuietStartTime));
                OnPropertyChanged(nameof(QuietEndTime));
            }
            await LoadCamerasAsync();
        }

        private async Task LoadCamerasAsync()
        {
            AvailableCameras.Clear();
            AvailableCameras.Add(new VideoDeviceModel { DeviceId = "", DeviceName = "(None)" });
            foreach (var device in await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture))
                AvailableCameras.Add(new VideoDeviceModel { DeviceId = device.Id, DeviceName = device.Name });

            var id = ConfigurationSettings?.AppConfig?.SecondaryCameraId;
            SecondaryCameraModel = AvailableCameras.FirstOrDefault(c => c.DeviceId == id)
                ?? AvailableCameras.FirstOrDefault();
            OnPropertyChanged(nameof(AvailableCameras));
        }

        private void Save()
        {
            try
            {
                if (ConfigurationSettings != null)
                    ConfigurationServices.SaveConfig(ConfigurationSettings);
            }
            catch { }
        }

        private async Task TestDiscordAsync()
        {
            try
            {
                Status = "Sending Discord test...";
                var url = ConfigurationSettings?.AppConfig?.DiscordWebhookUrl;
                if (string.IsNullOrWhiteSpace(url))
                {
                    Status = "Enter a Discord webhook URL first.";
                    return;
                }
                var client = new Windows.Web.Http.HttpClient();
                var json = "{\"content\":\"Test from Universal Motion Detector Elite\"}";
                var content = new Windows.Web.Http.HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                var resp = await client.PostAsync(new Uri(url), content);
                Status = resp.IsSuccessStatusCode ? "Discord test OK" : "Discord failed: " + resp.StatusCode;
            }
            catch (Exception ex)
            {
                Status = "Discord error: " + ex.Message;
            }
        }

        private async Task TestIftttAsync()
        {
            try
            {
                Status = "Sending IFTTT test...";
                var key = ConfigurationSettings?.AppConfig?.IftttKey;
                var evt = ConfigurationSettings?.AppConfig?.IftttEventName ?? "motion_detected";
                if (string.IsNullOrWhiteSpace(key))
                {
                    Status = "Enter IFTTT key first.";
                    return;
                }
                var uri = new Uri($"https://maker.ifttt.com/trigger/{evt}/with/key/{key}");
                var client = new Windows.Web.Http.HttpClient();
                var json = "{\"value1\":\"test\",\"value2\":\"" + DateTime.Now.ToString("o") + "\",\"value3\":\"UMD Elite\"}";
                var content = new Windows.Web.Http.HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                var resp = await client.PostAsync(uri, content);
                Status = resp.IsSuccessStatusCode ? "IFTTT test OK" : "IFTTT failed: " + resp.StatusCode;
            }
            catch (Exception ex)
            {
                Status = "IFTTT error: " + ex.Message;
            }
        }
    }
}
