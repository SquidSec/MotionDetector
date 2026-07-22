using BaseCodeLibrary.Utilities;

namespace MotionDetector.Models
{
    public class AppConfigModel : ModelBase
    {
        private bool _sendEmails;
        private int _captureDelay;
        private int _alertDelay;
        private int _alertThreshold;
        private double _pixelDelta;
        private int _imageDelta;
        private double _darkShiftThreshold;
        private int _configVersion;
        private string _selectedCameraId;

        public int ConfigVersion
        {
            get { return _configVersion; }
            set { _configVersion = value; }
        }

        public double DarkShiftThreshold
        {
            get { return _darkShiftThreshold; }
            set { _darkShiftThreshold = value; OnPropertyChanged(nameof(DarkShiftThreshold)); }
        }

        public bool SendEmails
        {
            get { return _sendEmails; }
            set { _sendEmails = value; OnPropertyChanged(nameof(SendEmails)); }
        }

        public int CaptureDelay
        {
            get { return _captureDelay; }
            set { _captureDelay = value; OnPropertyChanged(nameof(CaptureDelay)); }
        }

        public int AlertDelay
        {
            get { return _alertDelay; }
            set { _alertDelay = value; OnPropertyChanged(nameof(AlertDelay)); }
        }

        public int AlertThreshold
        {
            get { return _alertThreshold; }
            set { _alertThreshold = value; OnPropertyChanged(nameof(AlertThreshold)); }
        }

        public double PixelDelta
        {
            get { return _pixelDelta; }
            set { _pixelDelta = value; OnPropertyChanged(nameof(PixelDelta)); }
        }

        public int ImageDelta
        {
            get { return _imageDelta; }
            set { _imageDelta = value; OnPropertyChanged(nameof(ImageDelta)); }
        }

        public string SelectedCameraId
        {
            get { return _selectedCameraId; }
            set { _selectedCameraId = value; OnPropertyChanged(nameof(SelectedCameraId)); }
        }

        private int _minBaselineImages = 5;
        public int MinBaselineImages
        {
            get { return _minBaselineImages; }
            set { _minBaselineImages = value; OnPropertyChanged(nameof(MinBaselineImages)); }
        }

        private int _startupCountdownSeconds = 30;
        public int StartupCountdownSeconds
        {
            get { return _startupCountdownSeconds; }
            set { _startupCountdownSeconds = value; OnPropertyChanged(nameof(StartupCountdownSeconds)); }
        }

        // Free tier additions
        private bool _enableLocalLogging = true;
        public bool EnableLocalLogging
        {
            get { return _enableLocalLogging; }
            set { _enableLocalLogging = value; OnPropertyChanged(nameof(EnableLocalLogging)); }
        }

        private double _ignoreTopPercent = 0;
        public double IgnoreTopPercent
        {
            get { return _ignoreTopPercent; }
            set { _ignoreTopPercent = value; OnPropertyChanged(nameof(IgnoreTopPercent)); }
        }

        private double _ignoreBottomPercent = 0;
        public double IgnoreBottomPercent
        {
            get { return _ignoreBottomPercent; }
            set { _ignoreBottomPercent = value; OnPropertyChanged(nameof(IgnoreBottomPercent)); }
        }

        private double _ignoreLeftPercent = 0;
        public double IgnoreLeftPercent
        {
            get { return _ignoreLeftPercent; }
            set { _ignoreLeftPercent = value; OnPropertyChanged(nameof(IgnoreLeftPercent)); }
        }

        private double _ignoreRightPercent = 0;
        public double IgnoreRightPercent
        {
            get { return _ignoreRightPercent; }
            set { _ignoreRightPercent = value; OnPropertyChanged(nameof(IgnoreRightPercent)); }
        }

        private bool _webhookEnabled = false;
        public bool WebhookEnabled
        {
            get { return _webhookEnabled; }
            set { _webhookEnabled = value; OnPropertyChanged(nameof(WebhookEnabled)); }
        }

        private string _webhookUrl = "";
        public string WebhookUrl
        {
            get { return _webhookUrl; }
            set { _webhookUrl = value; OnPropertyChanged(nameof(WebhookUrl)); }
        }

        private bool _useDarkTheme = false;
        public bool UseDarkTheme
        {
            get { return _useDarkTheme; }
            set { _useDarkTheme = value; OnPropertyChanged(nameof(UseDarkTheme)); }
        }

        private double _dayImageDelta = 7;
        public double DayImageDelta
        {
            get { return _dayImageDelta; }
            set { _dayImageDelta = value; OnPropertyChanged(nameof(DayImageDelta)); }
        }

        private double _nightImageDelta = 7;
        public double NightImageDelta
        {
            get { return _nightImageDelta; }
            set { _nightImageDelta = value; OnPropertyChanged(nameof(NightImageDelta)); }
        }

        // ----- Premium features -----
        private bool _scheduleEnabled;
        public bool ScheduleEnabled
        {
            get { return _scheduleEnabled; }
            set { _scheduleEnabled = value; OnPropertyChanged(nameof(ScheduleEnabled)); }
        }

        /// <summary>Bitmask Sun=1<<0 ... Sat=1<<6. Default all days.</summary>
        private int _scheduleDaysMask = 0x7F;
        public int ScheduleDaysMask
        {
            get { return _scheduleDaysMask; }
            set { _scheduleDaysMask = value; OnPropertyChanged(nameof(ScheduleDaysMask)); }
        }

        private int _scheduleStartMinutes = 0; // 00:00
        public int ScheduleStartMinutes
        {
            get { return _scheduleStartMinutes; }
            set { _scheduleStartMinutes = value; OnPropertyChanged(nameof(ScheduleStartMinutes)); }
        }

        private int _scheduleEndMinutes = 1439; // 23:59
        public int ScheduleEndMinutes
        {
            get { return _scheduleEndMinutes; }
            set { _scheduleEndMinutes = value; OnPropertyChanged(nameof(ScheduleEndMinutes)); }
        }

        private bool _discordEnabled;
        public bool DiscordEnabled
        {
            get { return _discordEnabled; }
            set { _discordEnabled = value; OnPropertyChanged(nameof(DiscordEnabled)); }
        }

        private string _discordWebhookUrl = "";
        public string DiscordWebhookUrl
        {
            get { return _discordWebhookUrl; }
            set { _discordWebhookUrl = value; OnPropertyChanged(nameof(DiscordWebhookUrl)); }
        }

        private bool _iftttEnabled;
        public bool IftttEnabled
        {
            get { return _iftttEnabled; }
            set { _iftttEnabled = value; OnPropertyChanged(nameof(IftttEnabled)); }
        }

        private string _iftttKey = "";
        public string IftttKey
        {
            get { return _iftttKey; }
            set { _iftttKey = value; OnPropertyChanged(nameof(IftttKey)); }
        }

        private string _iftttEventName = "motion_detected";
        public string IftttEventName
        {
            get { return _iftttEventName; }
            set { _iftttEventName = value; OnPropertyChanged(nameof(IftttEventName)); }
        }

        /// <summary>Minimum % of considered pixels that must change (smart size filter).</summary>
        private double _minMotionAreaPercent = 0.5;
        public double MinMotionAreaPercent
        {
            get { return _minMotionAreaPercent; }
            set { _minMotionAreaPercent = value; OnPropertyChanged(nameof(MinMotionAreaPercent)); }
        }

        private bool _saveAlertClip;
        public bool SaveAlertClip
        {
            get { return _saveAlertClip; }
            set { _saveAlertClip = value; OnPropertyChanged(nameof(SaveAlertClip)); }
        }

        private int _alertClipFrames = 5;
        public int AlertClipFrames
        {
            get { return _alertClipFrames; }
            set { _alertClipFrames = value; OnPropertyChanged(nameof(AlertClipFrames)); }
        }

        private bool _enableAlertHistory = true;
        public bool EnableAlertHistory
        {
            get { return _enableAlertHistory; }
            set { _enableAlertHistory = value; OnPropertyChanged(nameof(EnableAlertHistory)); }
        }

        private bool _healthCheckEnabled;
        public bool HealthCheckEnabled
        {
            get { return _healthCheckEnabled; }
            set { _healthCheckEnabled = value; OnPropertyChanged(nameof(HealthCheckEnabled)); }
        }

        private int _healthCheckMinutes = 60;
        public int HealthCheckMinutes
        {
            get { return _healthCheckMinutes; }
            set { _healthCheckMinutes = value; OnPropertyChanged(nameof(HealthCheckMinutes)); }
        }

        private string _settingsPin = "";
        public string SettingsPin
        {
            get { return _settingsPin; }
            set { _settingsPin = value; OnPropertyChanged(nameof(SettingsPin)); }
        }

        private bool _lockSettingsWhenArmed;
        public bool LockSettingsWhenArmed
        {
            get { return _lockSettingsWhenArmed; }
            set { _lockSettingsWhenArmed = value; OnPropertyChanged(nameof(LockSettingsWhenArmed)); }
        }

        private string _zoneName = "Main area";
        public string ZoneName
        {
            get { return _zoneName; }
            set { _zoneName = value; OnPropertyChanged(nameof(ZoneName)); }
        }

        private string _secondaryCameraId = "";
        public string SecondaryCameraId
        {
            get { return _secondaryCameraId; }
            set { _secondaryCameraId = value; OnPropertyChanged(nameof(SecondaryCameraId)); }
        }

        private bool _useNightVisionPreset;
        public bool UseNightVisionPreset
        {
            get { return _useNightVisionPreset; }
            set { _useNightVisionPreset = value; OnPropertyChanged(nameof(UseNightVisionPreset)); }
        }

        private int _daySoundQuietHourStart = -1;
        public int DaySoundQuietHourStart
        {
            get { return _daySoundQuietHourStart; }
            set { _daySoundQuietHourStart = value; OnPropertyChanged(nameof(DaySoundQuietHourStart)); }
        }

        private int _daySoundQuietHourEnd = -1;
        public int DaySoundQuietHourEnd
        {
            get { return _daySoundQuietHourEnd; }
            set { _daySoundQuietHourEnd = value; OnPropertyChanged(nameof(DaySoundQuietHourEnd)); }
        }

        private bool _continuousBufferEnabled;
        public bool ContinuousBufferEnabled
        {
            get { return _continuousBufferEnabled; }
            set { _continuousBufferEnabled = value; OnPropertyChanged(nameof(ContinuousBufferEnabled)); }
        }

        private int _bufferSeconds = 10;
        public int BufferSeconds
        {
            get { return _bufferSeconds; }
            set { _bufferSeconds = value; OnPropertyChanged(nameof(BufferSeconds)); }
        }

        private string _nightRecipient = "";
        public string NightRecipient
        {
            get { return _nightRecipient; }
            set { _nightRecipient = value; OnPropertyChanged(nameof(NightRecipient)); }
        }
    }
}
