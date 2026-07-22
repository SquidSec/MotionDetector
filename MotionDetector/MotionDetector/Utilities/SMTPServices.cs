using LightBuzz.SMTP;
using Models.MotionDetector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Email;
using Windows.Storage.Streams;

namespace MotionDetector.Utilities
{
    public static class SMTPServices
    {
        public static async Task<SmtpResult> SendAlertEmail(List<IRandomAccessStream> streams, ConfigModel ConfigurationSettings)
        {
            var settings = ConfigurationSettings.SmtpSettings;
            string recipientsRaw = settings.Recipient ?? string.Empty;

            using (SmtpClient client = new SmtpClient(settings.SmtpServer,
                                                      settings.SmtpPort,
                                                      settings.UseSSL,
                                                      settings.SmtpUserName,
                                                      settings.SmtpPassword))
            {
                EmailMessage emailMessage = new EmailMessage();

                emailMessage.Importance = EmailImportance.High;
                emailMessage.Sender.Address = string.IsNullOrWhiteSpace(settings.FromAddress) ? "alerts@motiondetector.local" : settings.FromAddress;
                emailMessage.Sender.Name = string.IsNullOrWhiteSpace(settings.FromName) ? "Universal Motion Detector" : settings.FromName;

                // Support multiple recipients (comma or semicolon separated) - next-gen
                foreach (var r in recipientsRaw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = r.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        emailMessage.To.Add(new EmailRecipient(trimmed));
                }

                emailMessage.Subject = "ALERT | MOTION DETECTED";

                int imageCount = streams?.Count ?? 0;
                emailMessage.Body = 
                    $"Motion was detected at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                    $"Images attached: {imageCount}\n" +
                    "This alert was sent by Universal Motion Detector Elite.";

                if (streams != null)
                {
                    int idx = 1;
                    foreach (IRandomAccessStream stream in streams)
                    {
                        string fileName = $"alert_{DateTime.Now:yyyyMMdd_HHmmss}_{idx}.png";
                        var reference = RandomAccessStreamReference.CreateFromStream(stream);
                        emailMessage.Attachments.Add(new EmailAttachment(fileName, reference));
                        idx++;
                    }
                }

                SmtpResult result = await client.SendMailAsync(emailMessage);

                // Cleanup
                if (streams != null)
                {
                    foreach (var stream in streams)
                        stream?.Dispose();
                    streams.Clear();
                }

                return result;
            }
        }

        public static async Task<(bool Success, string Message)> RunSMTPTestAsync(ConfigModel ConfigurationSettings)
        {
            var settings = ConfigurationSettings.SmtpSettings;
            if (string.IsNullOrWhiteSpace(settings.Recipient))
                return (false, "No recipient configured.");

            try
            {
                using (SmtpClient client = new SmtpClient(settings.SmtpServer,
                                          settings.SmtpPort,
                                          settings.UseSSL,
                                          settings.SmtpUserName,
                                          settings.SmtpPassword))
                {
                    EmailMessage emailMessage = new EmailMessage();
                    emailMessage.Subject = "TEST | Motion Detector Email Configuration";
                    emailMessage.Importance = EmailImportance.Normal;
                    emailMessage.Sender.Address = string.IsNullOrWhiteSpace(settings.FromAddress) ? "alerts@motiondetector.local" : settings.FromAddress;
                    emailMessage.Sender.Name = string.IsNullOrWhiteSpace(settings.FromName) ? "Universal Motion Detector" : settings.FromName;

                    foreach (var r in (settings.Recipient ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var t = r.Trim();
                        if (!string.IsNullOrEmpty(t)) emailMessage.To.Add(new EmailRecipient(t));
                    }

                    emailMessage.Body =
                        "This is a test email from your Universal Motion Detector.\n\n" +
                        "If you received this, your SMTP settings are working correctly.\n" +
                        $"Sent at: {DateTime.Now}\n\n" +
                        "Next-gen email system active.";

                    SmtpResult result = await client.SendMailAsync(emailMessage);

                    return (true, "Test email sent successfully!");
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
