using BasecodeLibrary.Utilities;
using Models.MotionDetector;
using MotionDetector.Models;
using MotionDetector.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Devices;
// StreamingCaptureMode in Windows.Media.Capture
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace MotionDetector.ViewModels
{
    public class MotionDetectorViewModel : ViewModelBase
    {
        #region Bound Properties

        #region private backers
        private bool _isAlert;
        private bool _isAlertSoundRunning = false;
        private List<byte[]> _baselineImages;
        private AlertDisplayImageModel _selectedAlertImage;
        private ObservableCollection<WriteableBitmap> _displayImages;
        private ObservableCollection<AlertDisplayImageModel> _alertDisplayImages;

        private bool _isCountdownActive = true;
        private int _countdownSecondsRemaining;
        private bool _isReadyToAlert = false;
        private DispatcherTimer _countdownTimer;

        // For FPS
        private int _frameCount;
        private DateTime _lastFpsUpdate = DateTime.Now;
        #endregion

        public bool IsAlert
        {
            get { return _isAlert; }
            set { _isAlert = value; OnPropertyChanged(nameof(IsAlert)); }
        }

        public bool IsCountdownActive
        {
            get { return _isCountdownActive; }
            set { _isCountdownActive = value; OnPropertyChanged(nameof(IsCountdownActive)); }
        }

        public int CountdownSecondsRemaining
        {
            get { return _countdownSecondsRemaining; }
            set { _countdownSecondsRemaining = value; OnPropertyChanged(nameof(CountdownSecondsRemaining)); }
        }

        public bool IsReadyToAlert
        {
            get { return _isReadyToAlert; }
            set 
            { 
                _isReadyToAlert = value; 
                OnPropertyChanged(nameof(IsReadyToAlert));
                OnPropertyChanged(nameof(IsInPreparationMode));
            }
        }

        public bool IsInPreparationMode => !IsReadyToAlert;

        private bool _showStartCountdownButton = true;
        public bool ShowStartCountdownButton
        {
            get { return _showStartCountdownButton; }
            set { _showStartCountdownButton = value; OnPropertyChanged(nameof(ShowStartCountdownButton)); }
        }

        private string _selectedCameraName = "No camera";
        public string SelectedCameraName
        {
            get { return _selectedCameraName; }
            set { _selectedCameraName = value; OnPropertyChanged(nameof(SelectedCameraName)); }
        }

        // Improved Dashboard stats (free)
        private double _liveFps;
        public double LiveFps
        {
            get { return _liveFps; }
            set { _liveFps = value; OnPropertyChanged(nameof(LiveFps)); }
        }

        private DateTime? _lastMotionTime;
        public DateTime? LastMotionTime
        {
            get { return _lastMotionTime; }
            set { _lastMotionTime = value; OnPropertyChanged(nameof(LastMotionTime)); }
        }

        private int _totalAlertsToday;
        public int TotalAlertsToday
        {
            get { return _totalAlertsToday; }
            set { _totalAlertsToday = value; OnPropertyChanged(nameof(TotalAlertsToday)); }
        }

        public ConfigModel ConfigurationSettings { get; set; }

        public MediaCapture MediaCaptureElement { get; set; }

        public AlertDisplayImageModel SelectedAlertImage { get => _selectedAlertImage; set { _selectedAlertImage = value; OnPropertyChanged(nameof(SelectedAlertImage)); } }

        /// <summary>
        /// The baseline images are what each newly captured image will be compared to.
        /// </summary>
        public ObservableCollection<WriteableBitmap> DisplayImages
        {
            get { return _displayImages; }
            set { _displayImages = value; OnPropertyChanged(nameof(DisplayImages)); }
        }

        public ObservableCollection<AlertDisplayImageModel> AlertDisplayImages
        {
            get { return _alertDisplayImages; }
            set { _alertDisplayImages = value; OnPropertyChanged(nameof(AlertDisplayImages)); }
        }

        #region Command Bindings
        public ICommand SaveImageCommand { get; set; }
        public ICommand StopAlertSoundCommand { get; set; }
        public ICommand InitializeCaptureSinkCommand { get; set; }
        public ICommand StartCountdownCommand { get; set; }
        #endregion

        #endregion

        #region Unbound properties

        /// <summary>
        /// The timer that controls the image test frequency 
        /// </summary>
        private DispatcherTimer captureTimer { get; set; }

        /// <summary>
        /// The timer that controls the baseline image capture frequency 
        /// </summary>
        private DispatcherTimer baselineTimer { get; set; }

        /// <summary>
        /// This is the list of images that will be emailed to the recipient once the threshold has been met.
        /// </summary>
        private List<IRandomAccessStream> streamList { get; set; }

        #endregion

        #region Variables

        //This is the secret sauce
        public CaptureElement caputureSink = new CaptureElement();
        public CaptureElement secondaryCaptureSink = new CaptureElement();
        public MediaCapture SecondaryMediaCapture { get; set; }

        private bool _showSecondaryCamera;
        public bool ShowSecondaryCamera
        {
            get { return _showSecondaryCamera; }
            set { _showSecondaryCamera = value; OnPropertyChanged(nameof(ShowSecondaryCamera)); }
        }

        private string _secondaryCameraName = "";
        public string SecondaryCameraName
        {
            get { return _secondaryCameraName; }
            set { _secondaryCameraName = value; OnPropertyChanged(nameof(SecondaryCameraName)); }
        }

        #endregion

        #region Constructors

        public MotionDetectorViewModel()
        {
            _baselineImages = new List<byte[]>();
            captureTimer = new DispatcherTimer();
            baselineTimer = new DispatcherTimer();
            streamList = new List<IRandomAccessStream>();
            DisplayImages = new ObservableCollection<WriteableBitmap>();
            AlertDisplayImages = new ObservableCollection<AlertDisplayImageModel>();

            SaveImageCommand = new CommandHandler(SaveImageExecuted);
            StopAlertSoundCommand = new CommandHandler(StopAlertSoundExecuted);
            InitializeCaptureSinkCommand = new CommandHandler(InitializeCameraAndSink);
            StartCountdownCommand = new CommandHandler(StartCountdownExecuted);
        }

        ~MotionDetectorViewModel()
        {
            Destroyer();
        }

        public void Destroyer()
        {
            try
            {
                SaveImageCommand = null;
                StopAlertSoundCommand = null;
                InitializeCaptureSinkCommand = null;
                MediaCaptureElement?.StopPreviewAsync();
                MediaCaptureElement?.Dispose();
                MediaCaptureElement = null;
                try
                {
                    SecondaryMediaCapture?.StopPreviewAsync();
                    SecondaryMediaCapture?.Dispose();
                    SecondaryMediaCapture = null;
                    secondaryCaptureSink.Source = null;
                }
                catch { }
                CameraServices.ResetLowLagCapture();
                _baselineImages = null;
                captureTimer?.Stop();

                if (baselineTimer != null && captureTimer != null)
                {
                    //for some reason I can't use the null check operator with events? Super weird 
                    baselineTimer.Tick -= OnBaselineTimerTick;
                    captureTimer.Tick -= OnCaptureTimerTick;
                }
                captureTimer = null;
                baselineTimer = null;
                streamList = null;
                DisplayImages = null;
                AlertDisplayImages = null;

                SaveImageCommand = null;
            }
            catch { }
        }

        #endregion

        #region Executed functions

        private void SaveImageExecuted()
        {
            if (SelectedAlertImage != null)
                CameraServices.SaveImage(SelectedAlertImage);
        }

        #endregion

        #region Public Functions

        public async void InitializeCameraAndSink()
        {
            await InitializeCameraAndSinkImplementation();
        }

        private async Task InitializeCameraAndSinkImplementation()
        {
            try
            {
                // If the element isn't null and it's just not streaming, that means we're still initializing it
                // don't reinitialize it or it'll explode. This is a side effect of the backgrounding shit
                // we need to do
                if (MediaCaptureElement != null
                    && MediaCaptureElement.CameraStreamState == CameraStreamState.NotStreaming)
                {
                    return;
                }

                MediaCaptureElement = new MediaCapture();

                DisplayRequest _displayRequest = new DisplayRequest();

                //make request to put in active state
                _displayRequest.RequestActive();

                if (ConfigurationSettings?.AppConfig.SelectedCameraId != null)
                {
                    await MediaCaptureElement.InitializeAsync(new MediaCaptureInitializationSettings() { VideoDeviceId = ConfigurationSettings?.AppConfig.SelectedCameraId });
                }
                else
                {
                    await MediaCaptureElement.InitializeAsync();
                }

                caputureSink.Source = MediaCaptureElement;

                await MediaCaptureElement.StartPreviewAsync();

                await InitializeSecondaryCameraAsync();
            }
            catch { }
        }

        private async Task InitializeSecondaryCameraAsync()
        {
            ShowSecondaryCamera = false;
            SecondaryCameraName = "";
            try
            {
                if (SecondaryMediaCapture != null)
                {
                    try { await SecondaryMediaCapture.StopPreviewAsync(); } catch { }
                    SecondaryMediaCapture.Dispose();
                    SecondaryMediaCapture = null;
                }
                secondaryCaptureSink.Source = null;

                string secId = ConfigurationSettings?.AppConfig?.SecondaryCameraId;
                if (!StoreServices.IsPremium || string.IsNullOrWhiteSpace(secId))
                    return;
                if (secId == ConfigurationSettings?.AppConfig?.SelectedCameraId)
                    return;

                SecondaryMediaCapture = new MediaCapture();
                await SecondaryMediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = secId,
                    StreamingCaptureMode = StreamingCaptureMode.Video
                });
                secondaryCaptureSink.Source = SecondaryMediaCapture;
                await SecondaryMediaCapture.StartPreviewAsync();

                var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                    Windows.Devices.Enumeration.DeviceClass.VideoCapture);
                var match = devices.FirstOrDefault(d => d.Id == secId);
                SecondaryCameraName = match != null ? match.Name : "Camera 2";
                ShowSecondaryCamera = true;
            }
            catch
            {
                ShowSecondaryCamera = false;
            }
        }

        /// <summary>
        /// Sets up the application and initializes the camera.
        /// </summary>
        public async Task Setup(int retryCount = 0)
        {
            try
            {
                ConfigurationSettings = await ConfigurationServices.GetConfig();

                ApplyTheme();
                TotalAlertsToday = 0;

                await InitializeCameraAndSinkImplementation();
                await RefreshSelectedCameraNameAsync();

                // Baseline starts only after countdown completes (not on Start press)
                baselineTimer.Interval = new TimeSpan(0, 0, 3);
                baselineTimer.Tick -= OnBaselineTimerTick;
                baselineTimer.Tick += OnBaselineTimerTick;
                if (baselineTimer.IsEnabled) baselineTimer.Stop();

                int countdown = ConfigurationSettings?.AppConfig?.StartupCountdownSeconds ?? 30;
                CountdownSecondsRemaining = countdown;
                IsCountdownActive = false;
                IsReadyToAlert = false;
                ShowStartCountdownButton = true;
                OnPropertyChanged(nameof(IsInPreparationMode));
            }
            catch
            {
                if (retryCount < 3)
                {
                    await Task.Delay(100);
                    await Setup(retryCount + 1);
                }
                // else give up silently
            }
        }

        private void StartCountdownExecuted()
        {
            StartStartupCountdown();
        }

        private void StartStartupCountdown()
        {
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer = null;
            }

            // Ensure baseline is not running during countdown
            if (baselineTimer != null && baselineTimer.IsEnabled)
                baselineTimer.Stop();

            int countdown = ConfigurationSettings?.AppConfig?.StartupCountdownSeconds ?? 30;
            if (countdown < 1) countdown = 1;
            CountdownSecondsRemaining = countdown;
            IsCountdownActive = true;
            IsReadyToAlert = false;
            ShowStartCountdownButton = false;
            OnPropertyChanged(nameof(IsInPreparationMode));

            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += (s, e) =>
            {
                CountdownSecondsRemaining--;
                if (CountdownSecondsRemaining <= 0)
                {
                    _countdownTimer.Stop();
                    IsCountdownActive = false;
                    // Baseline collection starts only after countdown completes
                    if (baselineTimer != null && !baselineTimer.IsEnabled)
                        baselineTimer.Start();
                    CheckIfReadyToAlert();
                    OnPropertyChanged(nameof(IsInPreparationMode));
                }
            };
            _countdownTimer.Start();
        }

        private async Task RefreshSelectedCameraNameAsync()
        {
            try
            {
                string id = ConfigurationSettings?.AppConfig?.SelectedCameraId;
                if (string.IsNullOrEmpty(id))
                {
                    SelectedCameraName = "Default camera";
                    return;
                }
                var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                    Windows.Devices.Enumeration.DeviceClass.VideoCapture);
                var match = devices.FirstOrDefault(d => d.Id == id);
                SelectedCameraName = match != null ? match.Name : "Selected camera";
            }
            catch
            {
                SelectedCameraName = "Camera";
            }
        }

        private void CheckIfReadyToAlert()
        {
            int minBaselines = ConfigurationSettings?.AppConfig?.MinBaselineImages ?? 5;
            if (!IsCountdownActive && _baselineImages != null && _baselineImages.Count >= minBaselines)
            {
                IsReadyToAlert = true;
                // Now start the actual capture timer for motion detection
                if (captureTimer != null)
                {
                    captureTimer.Interval = new TimeSpan(0, 0, 0, 0, ConfigurationSettings.AppConfig.CaptureDelay);
                    captureTimer.Tick += OnCaptureTimerTick;
                    captureTimer.Start();
                }
            }
        }

        #endregion

        #region Private Functions

        private void OnCaptureTimerTick(object sender, object e)
        {
            TakePhoto();
        }

        private void OnBaselineTimerTick(object sender, object e)
        {
            if (captureTimer == null)
            {
                return;
            }

            if (captureTimer.IsEnabled)
            {
                captureTimer.Stop();
            }

            CaptureBaseLineImage();
            captureTimer.Start();
        }

        /// <summary>
        /// Will capture a single image and store it in the baseline images list. Each image will be used to determine if we have an alert or not.
        /// </summary>
        private async void CaptureBaseLineImage()
        {
            try
            {
                if(_baselineImages == null)
                {
                    return;
                }

                SoftwareBitmap softwareBitmap = await CameraServices.CaptureImage(MediaCaptureElement);
                if (softwareBitmap == null) return;

                // FPS count from baseline too (free)
                _frameCount++;
                if ((DateTime.Now - _lastFpsUpdate).TotalSeconds >= 1)
                {
                    LiveFps = _frameCount;
                    _frameCount = 0;
                    _lastFpsUpdate = DateTime.Now;
                }

                WriteableBitmap writeableBitmap = new WriteableBitmap(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight);
                softwareBitmap.CopyToBuffer(writeableBitmap.PixelBuffer);

                byte[] imageBytes = new byte[4 * softwareBitmap.PixelWidth * softwareBitmap.PixelHeight];
                softwareBitmap.CopyToBuffer(imageBytes.AsBuffer());

                // Rolling baseline: append newest, drop oldest when over max
                int maxBaselines = 6;
                _baselineImages.Add(imageBytes);
                DisplayImages.Add(writeableBitmap);
                while (_baselineImages.Count > maxBaselines)
                {
                    _baselineImages.RemoveAt(0);
                }
                while (DisplayImages.Count > maxBaselines)
                {
                    DisplayImages.RemoveAt(0);
                }

                CheckIfReadyToAlert();

            }
            catch
            {
                // Sometimes when you serial capture we get an explosion because the hardware isn't ready...
                // Eat it and move on
            }
        }

        private void StopAlertSoundExecuted()
        {
            _isAlertSoundRunning = false;
        }

        private async void PlayAlertSound()
        {
            if (!StoreServices.IsPremium || !ConfigurationSettings.SoundConfig.PlaySounds || _isAlertSoundRunning)
                return;

            // Premium quiet hours for sounds
            int qStart = ConfigurationSettings.AppConfig.DaySoundQuietHourStart;
            int qEnd = ConfigurationSettings.AppConfig.DaySoundQuietHourEnd;
            if (qStart >= 0 && qEnd >= 0)
            {
                int h = DateTime.Now.Hour;
                bool quiet = qStart < qEnd ? (h >= qStart && h < qEnd) : (h >= qStart || h < qEnd);
                if (quiet) return;
            }

            if (!ConfigurationSettings.SoundConfig.PlayContinuous)
            {
                await SoundsServices.PlayAlertSound(ConfigurationSettings.SoundConfig.SoundName);
            }
            else
            {
                _isAlertSoundRunning = true;
                while (_isAlertSoundRunning)
                {
                    await SoundsServices.PlayAlertSound(ConfigurationSettings.SoundConfig.SoundName);
                    await Task.Delay(new TimeSpan(0, 0, ConfigurationSettings.SoundConfig.ContinuousSecondDelay));
                }
            }
        }

        /// <summary>
        /// Captures a photo and sends it off for analysis 
        /// </summary>
        private async void TakePhoto()
        {
            if (!IsReadyToAlert) return;
            // Premium schedule: skip motion when outside active hours
            if (!ScheduleServices.IsWithinActiveHours(ConfigurationSettings))
                return;

            try
            {
                SoftwareBitmap softwareBitmap = await CameraServices.CaptureImage(MediaCaptureElement);
                if (softwareBitmap == null) return;

                // Update FPS (free feature)
                _frameCount++;
                if ((DateTime.Now - _lastFpsUpdate).TotalSeconds >= 1)
                {
                    LiveFps = _frameCount;
                    _frameCount = 0;
                    _lastFpsUpdate = DateTime.Now;
                }

                int imgWidth = softwareBitmap.PixelWidth;
                int imgHeight = softwareBitmap.PixelHeight;
                byte[] imageBytes = new byte[4 * imgWidth * imgHeight];
                softwareBitmap.CopyToBuffer(imageBytes.AsBuffer());

                // Continuous buffer (premium): keep recent frames for clip export
                if (StoreServices.IsPremium && ConfigurationSettings.AppConfig.ContinuousBufferEnabled)
                    PushBufferFrame(softwareBitmap);

                // Day/Night threshold (free): temporarily override ImageDelta
                double originalDelta = ConfigurationSettings.AppConfig.ImageDelta;
                bool isDark = MotionServices.CheckIfImageIsDarkShifted(ConfigurationSettings, imageBytes);
                double nightDelta = ConfigurationSettings.AppConfig.NightImageDelta;
                if (StoreServices.IsPremium && ConfigurationSettings.AppConfig.UseNightVisionPreset && isDark)
                    nightDelta = Math.Max(nightDelta, 12); // less sensitive overnight preset
                ConfigurationSettings.AppConfig.ImageDelta = (int)(isDark ? nightDelta : ConfigurationSettings.AppConfig.DayImageDelta);

                bool isAlert = IsAlert = MotionServices.CheckForMotion(ConfigurationSettings, imageBytes, _baselineImages, imgWidth, imgHeight);

                ConfigurationSettings.AppConfig.ImageDelta = (int)originalDelta; // restore

                if (isAlert)
                {
                    LastMotionTime = DateTime.Now;
                    TotalAlertsToday++;
                    string zone = ConfigurationSettings.AppConfig.ZoneName ?? "Main area";

                    // Local logging (free)
                    if (ConfigurationSettings.AppConfig.EnableLocalLogging)
                    {
                        _ = LogEventAsync("Motion detected: " + zone, imageBytes);
                    }

                    // Premium alert history
                    if (StoreServices.IsPremium && ConfigurationSettings.AppConfig.EnableAlertHistory)
                    {
                        _ = AlertHistoryServices.AddAsync(new AlertHistoryEntry
                        {
                            Timestamp = DateTime.Now,
                            CameraName = SelectedCameraName,
                            ZoneName = zone,
                            Note = "Motion detected",
                            WasDark = isDark
                        });
                    }

                    PlayAlertSound();

                    WriteableBitmap writeableBitmap = new WriteableBitmap(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight);
                    softwareBitmap.CopyToBuffer(writeableBitmap.PixelBuffer);

                    AlertDisplayImages.Add(new AlertDisplayImageModel() { AlertDisplayImage = writeableBitmap, AlertDisplayCaption = DateTime.Now.ToString() + " - " + zone });

                    // Premium: save short image clip from buffer
                    if (StoreServices.IsPremium && ConfigurationSettings.AppConfig.SaveAlertClip)
                        _ = SaveAlertClipAsync();

                    captureTimer.Stop();
                    await Task.Delay(new TimeSpan(0, 0, ConfigurationSettings.AppConfig.AlertDelay));
                    captureTimer.Start();

                    if (ConfigurationSettings.AppConfig.SendEmails)
                    {
                        InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();

                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                        encoder.SetSoftwareBitmap(softwareBitmap);
                        await encoder.FlushAsync();
                        streamList.Add(stream);

                        if (streamList.Count > ConfigurationSettings.AppConfig.AlertThreshold)
                        {
                            await SMTPServices.SendAlertEmail(streamList, ConfigurationSettings);
                        }
                    }

                    // Webhook (free)
                    if (ConfigurationSettings.AppConfig.WebhookEnabled && !string.IsNullOrWhiteSpace(ConfigurationSettings.AppConfig.WebhookUrl))
                    {
                        _ = SendWebhookAsync("Motion detected (" + zone + ")", DateTime.Now);
                    }

                    // Premium Discord / IFTTT
                    if (StoreServices.IsPremium)
                    {
                        if (ConfigurationSettings.AppConfig.DiscordEnabled)
                            _ = SendDiscordAsync(zone, DateTime.Now);
                        if (ConfigurationSettings.AppConfig.IftttEnabled)
                            _ = SendIftttAsync(zone, DateTime.Now);
                    }
                }
            }
            catch
            {
                // Getting random COM errors. Just eat it and continue. There's nothing I can do about this. 
            }
        }

        private readonly System.Collections.Generic.Queue<WriteableBitmap> _frameBuffer = new System.Collections.Generic.Queue<WriteableBitmap>();

        private void PushBufferFrame(SoftwareBitmap softwareBitmap)
        {
            try
            {
                var wb = new WriteableBitmap(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight);
                softwareBitmap.CopyToBuffer(wb.PixelBuffer);
                _frameBuffer.Enqueue(wb);
                int max = Math.Max(3, ConfigurationSettings?.AppConfig?.AlertClipFrames ?? 5);
                while (_frameBuffer.Count > max) _frameBuffer.Dequeue();
            }
            catch { }
        }

        private async Task SaveAlertClipAsync()
        {
            try
            {
                var folder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("Clips", Windows.Storage.CreationCollisionOption.OpenIfExists);
                int i = 0;
                foreach (var frame in _frameBuffer.ToArray())
                {
                    var file = await folder.CreateFileAsync($"clip_{DateTime.Now:yyyyMMdd_HHmmss}_{i}.png", Windows.Storage.CreationCollisionOption.GenerateUniqueName);
                    using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                        using (var ps = frame.PixelBuffer.AsStream())
                        {
                            var pixels = new byte[ps.Length];
                            await ps.ReadAsync(pixels, 0, pixels.Length);
                            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                (uint)frame.PixelWidth, (uint)frame.PixelHeight, 96, 96, pixels);
                            await encoder.FlushAsync();
                        }
                    }
                    i++;
                }
            }
            catch { }
        }

        private async Task SendDiscordAsync(string zone, DateTime time)
        {
            try
            {
                var url = ConfigurationSettings.AppConfig.DiscordWebhookUrl;
                if (string.IsNullOrWhiteSpace(url)) return;
                var client = new Windows.Web.Http.HttpClient();
                var contentJson = $"{{\"content\":\"Motion in {zone} at {time:HH:mm:ss}\\nCamera: {SelectedCameraName}\"}}";
                var content = new Windows.Web.Http.HttpStringContent(contentJson, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                await client.PostAsync(new Uri(url), content);
            }
            catch { }
        }

        private async Task SendIftttAsync(string zone, DateTime time)
        {
            try
            {
                var key = ConfigurationSettings.AppConfig.IftttKey;
                var evt = ConfigurationSettings.AppConfig.IftttEventName;
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(evt)) return;
                var uri = new Uri($"https://maker.ifttt.com/trigger/{evt}/with/key/{key}");
                var client = new Windows.Web.Http.HttpClient();
                var json = $"{{\"value1\":\"{zone}\",\"value2\":\"{time:o}\",\"value3\":\"{SelectedCameraName}\"}}";
                var content = new Windows.Web.Http.HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                await client.PostAsync(uri, content);
            }
            catch { }
        }

        private async Task LogEventAsync(string message, byte[] imageBytes = null)
        {
            try
            {
                var folder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("Logs", Windows.Storage.CreationCollisionOption.OpenIfExists);
                string fileName = $"event_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                var logFile = await folder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                string logText = $"{DateTime.Now}: {message}\n";
                if (imageBytes != null)
                {
                    logText += $"Image size: {imageBytes.Length} bytes\n";
                    // Optionally save image too
                    var imgFile = await folder.CreateFileAsync($"image_{DateTime.Now:yyyyMMdd_HHmmss}.bin", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                    await Windows.Storage.FileIO.WriteBytesAsync(imgFile, imageBytes);
                }
                await Windows.Storage.FileIO.AppendTextAsync(logFile, logText);
            }
            catch { }
        }

        private async Task SendWebhookAsync(string message, DateTime time)
        {
            try
            {
                var client = new Windows.Web.Http.HttpClient();
                var json = $"{{\"event\":\"{message}\",\"time\":\"{time}\",\"camera\":\"{ConfigurationSettings.AppConfig.SelectedCameraId}\"}}";
                var content = new Windows.Web.Http.HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                await client.PostAsync(new Uri(ConfigurationSettings.AppConfig.WebhookUrl), content);
            }
            catch { }
        }

        private void ApplyTheme()
        {
            try
            {
                var app = Windows.UI.Xaml.Application.Current;
                if (ConfigurationSettings?.AppConfig?.UseDarkTheme == true)
                    app.RequestedTheme = Windows.UI.Xaml.ApplicationTheme.Dark;
                else
                    app.RequestedTheme = Windows.UI.Xaml.ApplicationTheme.Light;
            }
            catch { }
        }

        #endregion
    }
}
