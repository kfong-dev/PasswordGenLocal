using System;
using System.IO;
using System.Text;

namespace PasswordGenLocal.Core
{
    public class AppSettings
    {
        // ── Defaults ────────────────────────────────────────────────────────────

        public int Theme { get; set; }                          = 0;   // 0=System 1=Light 2=Dark
        public int DefaultLength { get; set; }                  = 16;
        public int DefaultMode { get; set; }                    = 0;   // CharsetMode
        public int DefaultCount { get; set; }                   = 1;
        public bool TrackAndClearGlobal { get; set; }           = false;
        public bool TrackAndClearEverSet { get; set; }          = false;
        public int ClipboardTimeoutSeconds { get; set; }        = 0;   // 0=disabled
        public bool AskOnGenerate { get; set; }                 = true;
        public bool CsvExportWarningAccepted { get; set; }      = false;

        // ── Session password caching ─────────────────────────────────────────
        // When true, the store password entered at load/first-save is kept in
        // process memory for the life of the store tab. Cleared on tab close.
        public bool RememberStorePassword { get; set; }         = false;

        // ── Store modification polling ────────────────────────────────────────
        public int StoreCheckIntervalSeconds { get; set; }      = 5;   // 1-60

        // ── Row deletion confirmation ─────────────────────────────────────────
        // When false the delete confirmation dialog is skipped globally.
        public bool DeleteConfirmGlobal { get; set; }           = true;

        private static string ConfigPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LPG", "config.ini");

        // ── Load ────────────────────────────────────────────────────────────────

        public string Load()
        {
            string path = ConfigPath;
            if (!File.Exists(path))
            {
                ResetToDefaults();
                return "Config file not found — defaults applied.";
            }

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (line.StartsWith('#') || !line.Contains('=')) continue;
                    int eq = line.IndexOf('=');
                    string key = line[..eq].Trim().ToLowerInvariant();
                    string val = line[(eq + 1)..].Trim();
                    // Strip inline comment
                    int hash = val.IndexOf('#');
                    if (hash >= 0) val = val[..hash].Trim();

                    switch (key)
                    {
                        case "theme":
                            if (int.TryParse(val, out int t)) Theme = Math.Clamp(t, 0, 2);
                            break;
                        case "defaultlength":
                            if (int.TryParse(val, out int dl)) DefaultLength = Math.Clamp(dl, 7, 128);
                            break;
                        case "defaultmode":
                            if (int.TryParse(val, out int dm)) DefaultMode = Math.Clamp(dm, 0, 2);
                            break;
                        case "defaultcount":
                            if (int.TryParse(val, out int dc)) DefaultCount = Math.Clamp(dc, 1, 128);
                            break;
                        case "trackandclearglobal":
                            TrackAndClearGlobal = ParseBool(val);
                            break;
                        case "trackandcleareverset":
                            TrackAndClearEverSet = ParseBool(val);
                            break;
                        case "clipboardtimeoutseconds":
                            if (int.TryParse(val, out int ct)) ClipboardTimeoutSeconds = Math.Max(0, ct);
                            break;
                        case "askongenerate":
                            AskOnGenerate = ParseBool(val);
                            break;
                        case "csvexportwarningaccepted":
                            CsvExportWarningAccepted = ParseBool(val);
                            break;
                        case "rememberstorepassword":
                            RememberStorePassword = ParseBool(val);
                            break;
                        case "storecheckintervalseconds":
                            if (int.TryParse(val, out int si)) StoreCheckIntervalSeconds = Math.Clamp(si, 1, 60);
                            break;
                        case "deleteconfirmglobal":
                            DeleteConfirmGlobal = ParseBool(val);
                            break;
                        // Legacy key — silently ignored; removed in this version
                        case "editablestorewarningaccepted":
                            break;
                    }
                }
                return "Config loaded.";
            }
            catch (Exception ex)
            {
                ResetToDefaults();
                return $"Config read error ({ex.Message}) — defaults applied.";
            }
        }

        // ── Save ────────────────────────────────────────────────────────────────

        public string Save()
        {
            try
            {
                string path = ConfigPath;
                string? dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var sb = new StringBuilder();
                sb.AppendLine("# PasswordGenLocal configuration");
                sb.AppendLine($"theme={Theme}");
                sb.AppendLine($"defaultLength={DefaultLength}");
                sb.AppendLine($"defaultMode={DefaultMode}");
                sb.AppendLine($"defaultCount={DefaultCount}");
                sb.AppendLine($"trackAndClearGlobal={TrackAndClearGlobal.ToString().ToLowerInvariant()}");
                sb.AppendLine($"trackAndClearEverSet={TrackAndClearEverSet.ToString().ToLowerInvariant()}");
                sb.AppendLine($"clipboardTimeoutSeconds={ClipboardTimeoutSeconds}");
                sb.AppendLine($"askOnGenerate={AskOnGenerate.ToString().ToLowerInvariant()}");
                sb.AppendLine($"csvExportWarningAccepted={CsvExportWarningAccepted.ToString().ToLowerInvariant()}");
                sb.AppendLine($"rememberStorePassword={RememberStorePassword.ToString().ToLowerInvariant()}");
                sb.AppendLine($"storeCheckIntervalSeconds={StoreCheckIntervalSeconds}");
                sb.AppendLine($"deleteConfirmGlobal={DeleteConfirmGlobal.ToString().ToLowerInvariant()}");

                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
                return "Config saved.";
            }
            catch (Exception ex)
            {
                return $"Config save error: {ex.Message}";
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        public void ResetToDefaults()
        {
            Theme                    = 0;
            DefaultLength            = 16;
            DefaultMode              = 0;
            DefaultCount             = 1;
            TrackAndClearGlobal      = false;
            TrackAndClearEverSet     = false;
            ClipboardTimeoutSeconds  = 0;
            AskOnGenerate            = true;
            CsvExportWarningAccepted = false;
            RememberStorePassword    = false;
            StoreCheckIntervalSeconds = 5;
            DeleteConfirmGlobal      = true;
        }

        private static bool ParseBool(string val)
        {
            return val.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   val.Equals("1",    StringComparison.Ordinal);
        }
    }
}
