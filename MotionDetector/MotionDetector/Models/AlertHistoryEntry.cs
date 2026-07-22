using System;

namespace MotionDetector.Models
{
    public class AlertHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string CameraName { get; set; }
        public string ZoneName { get; set; }
        public string Note { get; set; }
        public bool WasDark { get; set; }
    }
}
