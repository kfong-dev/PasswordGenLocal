using System;
using System.IO;
using System.Text;

namespace PasswordGenLocal
{
    internal static class AppLog
    {
        // Diagnostic log written alongside the executable.
        // Filename retained from the predecessor project for continuity with existing deployments.
        // Columns: timestamp (ISO 8601 local), type, message, detail
        private static readonly string LogPath =
            Path.Combine(AppContext.BaseDirectory, "rorg_log.csv");

        private static readonly object _lock = new object();

        static AppLog()
        {
            if (!File.Exists(LogPath))
            {
                try
                {
                    File.WriteAllText(LogPath,
                        "\"timestamp\",\"type\",\"message\",\"detail\"\r\n",
                        new UTF8Encoding(false));
                }
                catch { }
            }
        }

        public static void Write(string eventType, string message, string detail = "")
        {
            try
            {
                string line = string.Format(
                    "\"{0}\",\"{1}\",\"{2}\",\"{3}\"\r\n",
                    DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Escape(eventType),
                    Escape(message),
                    Escape(detail));

                lock (_lock)
                    File.AppendAllText(LogPath, line, new UTF8Encoding(false));
            }
            catch { }
        }

        private static string Escape(string? s) =>
            (s ?? string.Empty).Replace("\"", "\"\"");
    }
}
