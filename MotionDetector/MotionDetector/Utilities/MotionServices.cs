using Models.MotionDetector;
using System;
using System.Collections.Generic;

namespace MotionDetector.Utilities
{
    public static class MotionServices
    {
        /// <summary>
        /// Determins if the image being examined is a dark image. If it is, then true will be returned
        /// </summary>
        /// <param name="testImage">The image being examined</param>
        /// <returns>Checks if image is dark</returns>
        public static bool CheckIfImageIsDarkShifted(ConfigModel ConfigurationSettings, byte[] testImage)
        {
            if (testImage == null || testImage.Length == 0)
                return false;
            double total = 0;
            foreach (byte image in testImage)
            {
                total += Convert.ToDouble(image);
            }
            double average = total / testImage.Length;

            return average < ConfigurationSettings.AppConfig.DarkShiftThreshold;
        }

        /// <summary>
        /// Uses the parameters set in the config file to test the image passed in against all baseline images. If any of the baseline
        /// images match, then false is returned. Otherwise true is returned.
        /// </summary>
        /// <param name="testImage">The image you wish to test against all baseline images.</param>
        /// <returns>Returns true if none of the baseline images match the image passed in.</returns>
        public static bool CheckForMotion(ConfigModel ConfigurationSettings, byte[] testImage, IEnumerable<byte[]> baselineImages, int width = 0, int height = 0)
        {
            if (testImage == null || baselineImages == null)
                return false;
            double configPixelDelta = ConfigurationSettings.AppConfig.PixelDelta;
            List<bool> baseLineChecks = new List<bool>();

            int bytesPerPixel = 4;
            int totalPixels = testImage.Length / bytesPerPixel;
            if (width <= 0 || height <= 0)
            {
                // fallback square approx
                width = (int)Math.Sqrt(totalPixels);
                height = width;
            }

            int ignoreTopRows = (int)(height * ConfigurationSettings.AppConfig.IgnoreTopPercent / 100.0);
            int ignoreBottomRows = (int)(height * ConfigurationSettings.AppConfig.IgnoreBottomPercent / 100.0);
            int ignoreLeftCols = (int)(width * ConfigurationSettings.AppConfig.IgnoreLeftPercent / 100.0);
            int ignoreRightCols = (int)(width * ConfigurationSettings.AppConfig.IgnoreRightPercent / 100.0);

            foreach (byte[] baseline in baselineImages)
            {
                if (baseline == null || baseline.Length != testImage.Length)
                    continue;

                int changedPixels = 0;
                int consideredPixels = 0;

                for (int i = 0; i < totalPixels; i++)
                {
                    int row = i / width;
                    int col = i % width;

                    // skip ignored zones
                    if (row < ignoreTopRows || row >= (height - ignoreBottomRows)) continue;
                    if (col < ignoreLeftCols || col >= (width - ignoreRightCols)) continue;

                    consideredPixels++;

                    int byteIdx = i * bytesPerPixel;
                    // simple check on first byte for delta (approx)
                    byte t = testImage[byteIdx];
                    byte b = baseline[byteIdx];
                    if (t == 0)
                    {
                        if (b != 0) changedPixels++;
                    }
                    else
                    {
                        double pixelDelta = Convert.ToDouble(b) / Convert.ToDouble(t);
                        if (pixelDelta > (1.0 + configPixelDelta) || pixelDelta < (1.0 - configPixelDelta))
                        {
                            changedPixels++;
                        }
                    }
                }

                int denom = consideredPixels > 0 ? consideredPixels : (baseline.Length / bytesPerPixel);
                double changeRatio = Convert.ToDouble(changedPixels) / Convert.ToDouble(denom);
                double imageThreshold = Convert.ToDouble(ConfigurationSettings.AppConfig.ImageDelta) / 100d;
                // Premium smart filter: require minimum motion area
                double minArea = 0;
                if (StoreServices.IsPremium)
                    minArea = Math.Max(0, ConfigurationSettings.AppConfig.MinMotionAreaPercent) / 100d;
                baseLineChecks.Add(changeRatio > imageThreshold && changeRatio >= minArea);
            }

            if (baseLineChecks.Count > 0 && !baseLineChecks.Contains(false)
                    && !CheckIfImageIsDarkShifted(ConfigurationSettings, testImage))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
