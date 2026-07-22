using BasecodeLibrary.Utilities;
using Models.MotionDetector;
using MotionDetector.Models;
using MotionDetector.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Enumeration;

namespace MotionDetector.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private bool _isNotRunningTest = true;
        private VideoDeviceModel selectedCameraModel;
        private string _testStatus;

        public ICommand RunTestsCommand { get; set; }
        public ICommand UpdateSettingsCommand { get; set; }
        public ICommand RefreshCamerasCommand { get; set; }
        public ICommand ExportConfigCommand { get; set; }
        public ICommand ImportConfigCommand { get; set; }
        public ICommand PreviewSoundCommand { get; set; }
        public List<string> ServerOptions { get { return new List<string>() { "Gmail", "Outlook / Microsoft 365", "Yahoo", "iCloud", "Custom (Advanced)" }; } }
        public ObservableCollection<VideoDeviceModel> AvailableCameras { get; set; }
        public ConfigModel ConfigurationSettings { get; set; }

        public VideoDeviceModel SelectedCameraModel
        {
            get => selectedCameraModel;
            set
            {
                selectedCameraModel = value;
                OnPropertyChanged(nameof(SelectedCameraModel));
                if (value != null && ConfigurationSettings?.AppConfig != null)
                {
                    ConfigurationSettings.AppConfig.SelectedCameraId = value.DeviceId;
                    try { ConfigurationServices.SaveConfig(ConfigurationSettings); } catch { }
                }
            }
        }

        public bool IsNotRunningTest
        {
            get { return _isNotRunningTest; }
            set { _isNotRunningTest = value; OnPropertyChanged(nameof(IsNotRunningTest)); }
        }

        public string TestStatus
        {
            get { return _testStatus; }
            set { _testStatus = value; OnPropertyChanged(nameof(TestStatus)); }
        }

        public SettingsViewModel()
        {
            Setup();
        }

        ~SettingsViewModel()
        {
            try
            {
                if (this.ConfigurationSettings?.AppConfig != null)
                    this.ConfigurationSettings.AppConfig.PropertyChanged -= SettingsViewModel_PropertyChanged;
                if (this.ConfigurationSettings?.SmtpSettings != null)
                    this.ConfigurationSettings.SmtpSettings.PropertyChanged -= SettingsViewModel_PropertyChanged;
            }
            catch { }
        }

        private async void Setup()
        {
            AvailableCameras = new ObservableCollection<VideoDeviceModel>();
            RunTestsCommand = new CommandHandler(RunSMTPTestExecuted);
            UpdateSettingsCommand = new CommandHandler(UpdateSettingsExecuted);
            RefreshCamerasCommand = new CommandHandler(RefreshCamerasExecuted);
            ExportConfigCommand = new CommandHandler(ExportConfigExecuted);
            ImportConfigCommand = new CommandHandler(ImportConfigExecuted);
            PreviewSoundCommand = new CommandHandler(PreviewSoundExecuted);

            ConfigurationSettings = await ConfigurationServices.GetConfig();
            OnPropertyChanged(nameof(ConfigurationSettings));
            this.ConfigurationSettings.AppConfig.PropertyChanged += SettingsViewModel_PropertyChanged;
            this.ConfigurationSettings.SmtpSettings.PropertyChanged += SettingsViewModel_PropertyChanged;
            await LoadCameras();
        }

        private async void RunSMTPTestExecuted()
        {
            IsNotRunningTest = false;
            TestStatus = "Sending test...";
            var (success, message) = await SMTPServices.RunSMTPTestAsync(ConfigurationSettings);
            TestStatus = message;
            IsNotRunningTest = true;
        }

        private async Task LoadCameras()
        {
            foreach (var device in await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture))
            {
                AvailableCameras.Add(new VideoDeviceModel() { DeviceId = device.Id, DeviceName = device.Name });
            }

            VideoDeviceModel toSelect = null;
            if (!string.IsNullOrEmpty(ConfigurationSettings.AppConfig.SelectedCameraId))
            {
                toSelect = AvailableCameras.FirstOrDefault(c => c.DeviceId == ConfigurationSettings.AppConfig.SelectedCameraId);
            }
            if (toSelect == null && AvailableCameras.Count > 0)
            {
                toSelect = AvailableCameras.FirstOrDefault();
            }
            if (toSelect != null)
            {
                SelectedCameraModel = toSelect;
            }
        }

        private void SettingsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PreferredSmtpServer")
            {
                switch (ConfigurationSettings.SmtpSettings.PreferredSmtpServer)
                {
                    case "Gmail":
                        ConfigurationSettings.SmtpSettings.SmtpServer = "smtp.gmail.com";
                        ConfigurationSettings.SmtpSettings.SmtpPort = 465;
                        ConfigurationSettings.SmtpSettings.UseSSL = true;
                        break;
                    case "Outlook / Microsoft 365":
                        ConfigurationSettings.SmtpSettings.SmtpServer = "smtp.office365.com";
                        ConfigurationSettings.SmtpSettings.SmtpPort = 587;
                        ConfigurationSettings.SmtpSettings.UseSSL = true;
                        break;
                    case "Yahoo":
                        ConfigurationSettings.SmtpSettings.SmtpServer = "smtp.mail.yahoo.com";
                        ConfigurationSettings.SmtpSettings.SmtpPort = 465;
                        ConfigurationSettings.SmtpSettings.UseSSL = true;
                        break;
                    case "iCloud":
                        ConfigurationSettings.SmtpSettings.SmtpServer = "smtp.mail.me.com";
                        ConfigurationSettings.SmtpSettings.SmtpPort = 587;
                        ConfigurationSettings.SmtpSettings.UseSSL = true;
                        break;
                    case "Custom (Advanced)":
                        break;
                }
            }

            if (e.PropertyName == nameof(ConfigurationSettings.AppConfig.UseDarkTheme) ||
                e.PropertyName == "UseDarkTheme")
            {
                try
                {
                    var app = Windows.UI.Xaml.Application.Current;
                    app.RequestedTheme = ConfigurationSettings.AppConfig.UseDarkTheme ? Windows.UI.Xaml.ApplicationTheme.Dark : Windows.UI.Xaml.ApplicationTheme.Light;
                }
                catch { }
            }

            // Auto-save on any setting change (no Save button required)
            try
            {
                ConfigurationServices.SaveConfig(ConfigurationSettings);
            }
            catch { }
        }

        private void UpdateSettingsExecuted()
        {
            ConfigurationServices.SaveConfig(ConfigurationSettings);
        }

        private async void RefreshCamerasExecuted()
        {
            if (AvailableCameras != null)
            {
                AvailableCameras.Clear();
            }
            await LoadCameras();
        }

        private async void ExportConfigExecuted()
        {
            try
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("JSON Config", new List<string>() { ".json" });
                savePicker.SuggestedFileName = "MotionDetectorConfig";
                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(ConfigurationSettings, Newtonsoft.Json.Formatting.Indented);
                    await Windows.Storage.FileIO.WriteTextAsync(file, json);
                }
            }
            catch { }
        }

        private async void ImportConfigExecuted()
        {
            try
            {
                var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
                openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".json");
                var file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    string json = await Windows.Storage.FileIO.ReadTextAsync(file);
                    var imported = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigModel>(json);
                    if (imported != null)
                    {
                        ConfigurationSettings = imported;
                        OnPropertyChanged(nameof(ConfigurationSettings));
                        ConfigurationServices.SaveConfig(ConfigurationSettings);
                    }
                }
            }
            catch { }
        }

        private async void PreviewSoundExecuted()
        {
            try
            {
                await SoundsServices.PlayAlertSound(ConfigurationSettings.SoundConfig.SoundName);
            }
            catch { }
        }
    }
}
