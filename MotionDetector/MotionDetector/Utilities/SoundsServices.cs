using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace MotionDetector.Utilities
{
    public static class SoundsServices
    {
        private static MediaElement _mediaElement = new MediaElement();
        private const string CustomSoundsFolderName = "CustomSounds";

        private static async Task<StorageFolder> GetCustomSoundsFolderAsync()
        {
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync(CustomSoundsFolderName, CreationCollisionOption.OpenIfExists);
        }

        public static async Task<bool> ImportCustomSound()
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".wav");

            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null && file.IsAvailable)
            {
                StorageFolder folder = await GetCustomSoundsFolderAsync();
                await file.CopyAsync(folder, file.Name, NameCollisionOption.ReplaceExisting);
            }
            return true;
        }

        public static async Task<bool> PlayAlertSound(string sound)
        {
            if (string.IsNullOrEmpty(sound))
            {
                return false;
            }
            else
            {
                StorageFile file = null;
                try
                {
                    StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("Assets\\Sounds");
                    file = await folder.GetFileAsync(sound);
                }
                catch
                {
                    try
                    {
                        StorageFolder custom = await GetCustomSoundsFolderAsync();
                        file = await custom.GetFileAsync(sound);
                    }
                    catch { }
                }
                if (file != null)
                {
                    IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
                    _mediaElement.SetSource(stream, "");
                    _mediaElement.Play();
                }
            }
            return true;
        }

        public static async Task<IEnumerable<string>> GetAvailableSounds()
        {
            List<string> returnValues = new List<string>();
            try
            {
                StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("Assets\\Sounds");
                IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    returnValues.Add(file.Name);
                }
            }
            catch
            {
                MessageDialog dialog = new MessageDialog("There was an exception while gathering the included sound files. Please make sure the app has the proper permissions");
                await dialog.ShowAsync();
            }

            try
            {
                StorageFolder custom = await GetCustomSoundsFolderAsync();
                IReadOnlyList<StorageFile> customFiles = await custom.GetFilesAsync();
                foreach (StorageFile file in customFiles)
                {
                    if (!returnValues.Contains(file.Name))
                    {
                        returnValues.Add(file.Name);
                    }
                }
            }
            catch { }

            return returnValues;
        }
    }
}
