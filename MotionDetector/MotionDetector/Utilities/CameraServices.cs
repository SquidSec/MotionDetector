using MotionDetector.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace MotionDetector.Utilities
{
    public static class CameraServices
    {
        private static LowLagPhotoCapture lowLagCapture = null;               

        public static void ResetLowLagCapture()
        {
            lowLagCapture = null;
        }

        public static async Task<SoftwareBitmap> CaptureImage(MediaCapture MediaCaptureElement)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (lowLagCapture == null)
                {
                    try
                    {
                        lowLagCapture = await MediaCaptureElement.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
                    }
                    catch
                    {
                        lowLagCapture = null;
                        continue;
                    }
                }

                try
                {
                    CapturedPhoto capturedPhoto = await lowLagCapture.CaptureAsync();
                    SoftwareBitmap softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;
                    return softwareBitmap;
                }
                catch
                {
                    lowLagCapture = null;
                }
            }
            return null;
        }

        public static async void SaveImage(AlertDisplayImageModel SelectedAlertImage)
        {
            try
            {
                if (SelectedAlertImage?.AlertDisplayImage == null)
                    return;

                FileSavePicker picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.SuggestedFileName = "motion_alert_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                picker.FileTypeChoices.Add("PNG Image", new List<string> { ".png" });
                picker.FileTypeChoices.Add("JPEG Image", new List<string> { ".jpg" });

                StorageFile destFile = await picker.PickSaveFileAsync();
                if (destFile == null)
                    return;

                Guid encoderId = destFile.FileType.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                    ? BitmapEncoder.JpegEncoderId
                    : BitmapEncoder.PngEncoderId;

                using (IRandomAccessStream stream = await destFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    stream.Size = 0;
                    using (Stream pixelStream = SelectedAlertImage.AlertDisplayImage.PixelBuffer.AsStream())
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, stream);
                        byte[] pixels = new byte[pixelStream.Length];
                        await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                        encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                             BitmapAlphaMode.Ignore,
                                             (uint)SelectedAlertImage.AlertDisplayImage.PixelWidth,
                                             (uint)SelectedAlertImage.AlertDisplayImage.PixelHeight,
                                             96.0,
                                             96.0,
                                             pixels);

                        await encoder.FlushAsync();
                    }
                }
            }
            catch { }
        }
    }
}
