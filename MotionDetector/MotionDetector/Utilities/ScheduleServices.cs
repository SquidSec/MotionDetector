using Models.MotionDetector;
using System;

namespace MotionDetector.Utilities
{
    public static class ScheduleServices
    {
        /// <summary>
        /// Returns true if monitoring should be active right now based on premium schedule settings.
        /// Free tier / schedule disabled = always active.
        /// </summary>
        public static bool IsWithinActiveHours(ConfigModel config)
        {
            if (config?.AppConfig == null) return true;
            if (!StoreServices.IsPremium || !config.AppConfig.ScheduleEnabled) return true;

            var now = DateTime.Now;
            int dayBit = 1 << (int)now.DayOfWeek; // Sunday=0
            if ((config.AppConfig.ScheduleDaysMask & dayBit) == 0)
                return false;

            int start = Math.Max(0, Math.Min(1439, config.AppConfig.ScheduleStartMinutes));
            int end = Math.Max(0, Math.Min(1439, config.AppConfig.ScheduleEndMinutes));
            int mins = now.Hour * 60 + now.Minute;

            if (start == end) return true; // 24h
            if (start < end)
                return mins >= start && mins < end;
            // overnight window e.g. 22:00-06:00
            return mins >= start || mins < end;
        }
    }
}
