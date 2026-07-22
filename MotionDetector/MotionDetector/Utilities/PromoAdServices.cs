using System;
using System.Collections.Generic;

namespace MotionDetector.Utilities
{
    public class PromoAd
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Cta { get; set; }
        public string Url { get; set; }
        public string ImagePath { get; set; }
        public string AccentHex { get; set; }
    }

    public static class PromoAdServices
    {
        private static readonly List<PromoAd> Ads = new List<PromoAd>
        {
            new PromoAd
            {
                Title = "SquidScan by SquidSec",
                Subtitle = "Automated reconnaissance with scheduled scans, change detection, dashboards, and executive reports.",
                Cta = "Start Free",
                Url = "https://squidscanner.com",
                ImagePath = "ms-appx:///Assets/Ads/squid_logo.png",
                AccentHex = "#00ff9f"
            },
            new PromoAd
            {
                Title = "SquidScan Recon Intelligence",
                Subtitle = "KPIs, charts, PDF export, and scope sync for security assessments. Built for professionals.",
                Cta = "Open SquidScan",
                Url = "https://app.squidscanner.com/register",
                ImagePath = "ms-appx:///Assets/Ads/ss2.png",
                AccentHex = "#00ff9f"
            },
            new PromoAd
            {
                Title = "SquidOffense - Security Assessments",
                Subtitle = "Manual penetration testing for web, APIs, desktop apps, cloud, and mobile. Veteran-owned.",
                Cta = "Get a Quote",
                Url = "https://squidoffense.com",
                ImagePath = "ms-appx:///Assets/Ads/squid_logo.png",
                AccentHex = "#ff6b35"
            },
            new PromoAd
            {
                Title = "SquidSec Research Blog",
                Subtitle = "Security research, cheat sheets, supply-chain analysis, and practical hardening guidance.",
                Cta = "Read Blog",
                Url = "https://squidhacker.com",
                ImagePath = "ms-appx:///Assets/Ads/ss1.png",
                AccentHex = "#7c4dff"
            },
            new PromoAd
            {
                Title = "BloodBash - Security Toolkit",
                Subtitle = "Tools and reference material for security researchers and assessment professionals.",
                Cta = "Learn More",
                Url = "https://squidhacker.com/cheat-sheets/",
                ImagePath = "ms-appx:///Assets/Ads/ss3.png",
                AccentHex = "#ff1744"
            }
        };

        private static int _index;
        private static readonly Random _rng = new Random();

        public static PromoAd Current { get; private set; } = Ads[0];

        public static PromoAd Next()
        {
            _index = (_index + 1) % Ads.Count;
            Current = Ads[_index];
            return Current;
        }

        public static PromoAd Random()
        {
            _index = _rng.Next(Ads.Count);
            Current = Ads[_index];
            return Current;
        }
    }
}
