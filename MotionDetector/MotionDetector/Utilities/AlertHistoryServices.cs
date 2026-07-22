using MotionDetector.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace MotionDetector.Utilities
{
    public static class AlertHistoryServices
    {
        private const string FileName = "alert_history.json";
        private static readonly object _lock = new object();

        public static async Task AddAsync(AlertHistoryEntry entry)
        {
            try
            {
                var list = await LoadAsync();
                list.Insert(0, entry);
                if (list.Count > 500) list.RemoveRange(500, list.Count - 500);
                var json = JsonConvert.SerializeObject(list, Formatting.Indented);
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
            catch { }
        }

        public static async Task<List<AlertHistoryEntry>> LoadAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(FileName);
                var json = await FileIO.ReadTextAsync(file);
                return JsonConvert.DeserializeObject<List<AlertHistoryEntry>>(json) ?? new List<AlertHistoryEntry>();
            }
            catch
            {
                return new List<AlertHistoryEntry>();
            }
        }

        public static async Task<string> ExportCsvAsync()
        {
            var list = await LoadAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Timestamp,Camera,Zone,Note,WasDark");
            foreach (var e in list)
            {
                sb.AppendLine($"\"{e.Timestamp:o}\",\"{Escape(e.CameraName)}\",\"{Escape(e.ZoneName)}\",\"{Escape(e.Note)}\",{e.WasDark}");
            }
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                $"alert_history_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                CreationCollisionOption.GenerateUniqueName);
            await FileIO.WriteTextAsync(file, sb.ToString());
            return file.Path;
        }

        private static string Escape(string s) => (s ?? "").Replace("\"", "\"\"");
    }
}
