using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using PasswordGenLocal;
using PasswordGenLocal.Core;
using PasswordGenLocal.UI.Dialogs;

namespace PasswordGenLocal.UI
{
    public partial class MainForm : Form
    {
        // ── Constants ────────────────────────────────────────────────────────────

        private const int InvalidLengthMin = 65;
        private const int InvalidLengthMax = 127;

        private const int LogMaxLines = 500;
        private const int LogTrimTo   = 400;

        // ── Settings & State ────────────────────────────────────────────────────
        private readonly AppSettings _settings = new();

        // _ran is seeded from CSPRNG every 60 s but is never used for password bytes.
        private Random _ran = new Random();

        // Suppresses the store prompt for the remainder of this session.
        private bool _suppressStorePromptSession = false;

        // Suppresses the delete-row confirmation for the remainder of this session.
        private bool _suppressDeleteConfirmSession = false;

        private decimal _lastValidLength = 16;

        // ── Timers ───────────────────────────────────────────────────────────────
        // Clock fires at 100 ms for centisecond display; reseed every 60 s.
        private readonly System.Windows.Forms.Timer _clockTimer  = new() { Interval = 100 };
        private readonly System.Windows.Forms.Timer _reseedTimer = new() { Interval = 60_000 };
        private System.Windows.Forms.Timer? _clipTimer;
        private System.Windows.Forms.Timer? _storeCheckTimer;

        // ── Controls (non-designer) ──────────────────────────────────────────────
        private SessionView sessionView = null!;
        private readonly Dictionary<TabPage, StoreView> _storeTabs = new();

        public MainForm()
        {
            InitializeComponent();

            string loadMsg = _settings.Load();
            AppLog.Write("SETTINGS", loadMsg);

            bool dark = _settings.Theme == 2 || (_settings.Theme == 0 && Theme.IsSystemDark());
            Theme.Apply(dark);

            sessionView = new SessionView();
            sessionView.ApplyTheme();
            sessionView.DeleteRequested += HandleDeleteRequested;
            tabSession.Controls.Add(sessionView);

            ApplyTheme(dark);

            ReseedRandom();

            _clockTimer.Tick  += ClockTimer_Tick;
            _reseedTimer.Tick += ReseedTimer_Tick;
            _clockTimer.Start();
            _reseedTimer.Start();

            if (_settings.ClipboardTimeoutSeconds > 0)
                StartClipboardTimer();

            StartStoreCheckTimer();

            int dl = _settings.DefaultLength;
            if (dl >= InvalidLengthMin && dl <= InvalidLengthMax) dl = 16;
            nudLength.Value = Math.Clamp(dl, 7, 128);
            _lastValidLength = nudLength.Value;
            switch (_settings.DefaultMode)
            {
                case 1: radNix.Checked   = true; break;
                case 2: radAzure.Checked = true; break;
                default: radWeb.Checked  = true; break;
            }
            nudCount.Value = Math.Clamp(_settings.DefaultCount, 1, 128);

            UpdateMenuState();
        }

        // ── Form Load ────────────────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Theme.SetTitleBarDark(Handle, Theme.IsDark);
            lblEncryption.Text = $"AES-256-GCM  |  {StoreManager.HashAlgorithmLabel}";
            ClockTimer_Tick(null, EventArgs.Empty);
            Log($"[{DateTime.Now:HH:mm:ss}] Password Generator \u2014 Local ready.");
        }

        // ── Generate ─────────────────────────────────────────────────────────────

        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            int length = (int)nudLength.Value;
            if (length >= InvalidLengthMin && length <= InvalidLengthMax)
            {
                nudLength.Value = _lastValidLength;
                Log($"[{DateTime.Now:HH:mm:ss}] Length {length} is in the invalid range {InvalidLengthMin}-{InvalidLengthMax}. Reverted.");
                return;
            }
            _lastValidLength = nudLength.Value;

            CharsetMode mode = radNix.Checked ? CharsetMode.Nix :
                               radAzure.Checked ? CharsetMode.Azure : CharsetMode.Web;
            int count = (int)nudCount.Value;

            bool isSessionTab = tabMain.SelectedTab == tabSession;
            StoreView? activeStore = null;
            if (!isSessionTab && tabMain.SelectedTab != null &&
                _storeTabs.TryGetValue(tabMain.SelectedTab, out var sv))
                activeStore = sv;

            if (isSessionTab && _settings.AskOnGenerate && !_suppressStorePromptSession)
            {
                var dr = PromptCreateStore();
                if (dr == DialogResult.Cancel)
                {
                    _suppressStorePromptSession = true;
                }
                else if (dr == DialogResult.Yes)
                {
                    isSessionTab = false;
                    if (tabMain.SelectedTab != null &&
                        _storeTabs.TryGetValue(tabMain.SelectedTab, out var nv))
                        activeStore = nv;
                }
            }

            for (int i = 0; i < count; i++)
            {
                string pwd = PasswordGenerator.Generate(mode, length);
                DateTime ts = DateTime.Now;

                if (activeStore != null)
                {
                    var entry = new StoreEntry
                    {
                        Id        = Guid.NewGuid(),
                        Label     = string.Empty,   // Username — user fills in
                        Note      = string.Empty,
                        Timestamp = ts,
                        Mode      = mode,
                        Length    = length,
                        Password  = pwd
                    };
                    activeStore.AddEntry(entry);
                    UpdateStoreTabTitle(activeStore);
                }
                else
                {
                    sessionView.AddEntry(ts, mode, length, pwd);
                }
            }

            Log($"[{DateTime.Now:HH:mm:ss}] Generated {count} \u00d7 {PasswordGenerator.ColumnLabel(mode, length)}");
        }

        private DialogResult PromptCreateStore()
        {
            using var dlg = new Form
            {
                Text            = "Save to Store?",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                ShowInTaskbar   = false,
                ClientSize      = new Size(380, 130),
                Font            = new Font("Segoe UI", 9f),
                BackColor       = Theme.FormBg,
                ForeColor       = Theme.PrimaryText
            };
            Theme.SetTitleBarDark(dlg.Handle, Theme.IsDark);

            var lbl = new Label
            {
                Text      = "Save generated passwords to a store?",
                Location  = new Point(12, 16),
                AutoSize  = true,
                BackColor = Theme.FormBg,
                ForeColor = Theme.PrimaryText
            };
            var btnCreate = new Button
            {
                Text      = "Create Store",
                Location  = new Point(12, 56),
                Width     = 100,
                BackColor = Theme.AccentBtn,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            var btnNotNow = new Button
            {
                Text      = "Not Now",
                Location  = new Point(120, 56),
                Width     = 80,
                BackColor = Theme.ToolbarBg,
                ForeColor = Theme.PrimaryText,
                FlatStyle = FlatStyle.Flat
            };
            var btnNever = new Button
            {
                Text      = "Never Ask This Session",
                Location  = new Point(208, 56),
                Width     = 162,
                BackColor = Theme.ToolbarBg,
                ForeColor = Theme.MutedText,
                FlatStyle = FlatStyle.Flat
            };

            DialogResult result = DialogResult.No;
            btnCreate.Click += (s, e) => { result = DialogResult.Yes;    dlg.Close(); };
            btnNotNow.Click += (s, e) => { result = DialogResult.No;     dlg.Close(); };
            btnNever.Click  += (s, e) => { result = DialogResult.Cancel; dlg.Close(); };

            dlg.Controls.Add(lbl);
            dlg.Controls.Add(btnCreate);
            dlg.Controls.Add(btnNotNow);
            dlg.Controls.Add(btnNever);

            dlg.ShowDialog(this);

            if (result == DialogResult.Yes)
                RunNewStoreFlow();

            return result;
        }

        // ── File Menu Handlers ────────────────────────────────────────────────────

        private void MenuNewStore_Click(object? sender, EventArgs e) => RunNewStoreFlow();

        private void RunNewStoreFlow()
        {
            using var dlg = new NewStoreDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            using var sfd = new SaveFileDialog
            {
                Title      = "Save New Store",
                Filter     = "LPGE Store (*.bin)|*.bin",
                DefaultExt = "bin"
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            var store = new LpgeStore
            {
                StoreName = dlg.StoreName,
                Created   = DateTime.Now,
                Modified  = DateTime.Now,
                Entries   = new System.Collections.Generic.List<StoreEntry>()
            };

            try
            {
                StoreManager.SaveStore(store, sfd.FileName, dlg.Password);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save store:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_settings.RememberStorePassword)
                store.CachedPassword = dlg.Password;

            store.FilePath = sfd.FileName;
            AddStoreTab(store);
            Log($"[{DateTime.Now:HH:mm:ss}] Created store: {Path.GetFileName(sfd.FileName)}");
        }

        private void MenuSaveStore_Click(object? sender, EventArgs e)
        {
            if (tabMain.SelectedTab == null ||
                !_storeTabs.TryGetValue(tabMain.SelectedTab, out var sv)) return;
            RunSaveStoreFlow(sv, tabMain.SelectedTab);
        }

        private void RunSaveStoreFlow(StoreView sv, TabPage tab)
        {
            if (sv.Store == null) return;
            LpgeStore store = sv.Store;

            string? filePath = store.FilePath;
            if (string.IsNullOrEmpty(filePath))
            {
                using var sfd = new SaveFileDialog
                {
                    Title      = "Save Store",
                    Filter     = "LPGE Store (*.bin)|*.bin",
                    DefaultExt = "bin"
                };
                if (sfd.ShowDialog(this) != DialogResult.OK) return;
                filePath = sfd.FileName;
            }

            // Use cached password if available and the feature is enabled
            string? password = null;
            if (_settings.RememberStorePassword && store.CachedPassword != null)
            {
                password = store.CachedPassword;
            }
            else
            {
                using var pp = new PassphraseDialog(PassphraseMode.Enter);
                if (pp.ShowDialog(this) != DialogResult.OK) return;
                password = pp.Passphrase;
                if (_settings.RememberStorePassword)
                    store.CachedPassword = password;
            }

            try
            {
                StoreManager.SaveStore(store, filePath, password);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save store:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            store.FilePath = filePath;
            store.IsDirty  = false;
            tab.Text = store.StoreName;
            sv.RefreshDirtyIndicator();
            Log($"[{DateTime.Now:HH:mm:ss}] Saved store: {Path.GetFileName(filePath)}");
        }

        private void MenuLoadStore_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title  = "Open Store",
                Filter = "LPGE Store (*.bin)|*.bin"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            using var pp = new PassphraseDialog(PassphraseMode.Enter);
            if (pp.ShowDialog(this) != DialogResult.OK) return;

            var (store, error) = StoreManager.LoadStore(ofd.FileName, pp.Passphrase);
            if (store == null)
            {
                MessageBox.Show($"Failed to load store:\n{error}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_settings.RememberStorePassword)
                store.CachedPassword = pp.Passphrase;

            AddStoreTab(store);
            Log($"[{DateTime.Now:HH:mm:ss}] Loaded store: {Path.GetFileName(ofd.FileName)} ({store.Entries.Count} entries)");
        }

        private void MenuClearOutput_Click(object? sender, EventArgs e)
        {
            if (tabMain.SelectedTab == tabSession)
            {
                sessionView.Clear();
                Log($"[{DateTime.Now:HH:mm:ss}] Session output cleared.");
            }
            else if (tabMain.SelectedTab != null &&
                     _storeTabs.TryGetValue(tabMain.SelectedTab, out var sv))
            {
                var dr = MessageBox.Show(
                    "This will remove displayed entries from this store.\n" +
                    "The store file is unchanged until you save.\n\nProceed?",
                    "Clear Store Output",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (dr == DialogResult.OK)
                {
                    sv.ClearDisplay();
                    UpdateStoreTabTitle(sv);
                    Log($"[{DateTime.Now:HH:mm:ss}] Store output cleared (unsaved).");
                }
            }
        }

        private void MenuCloseStore_Click(object? sender, EventArgs e)
        {
            if (tabMain.SelectedTab == null || tabMain.SelectedTab == tabSession) return;
            if (!_storeTabs.TryGetValue(tabMain.SelectedTab, out var sv)) return;
            CloseStoreTab(tabMain.SelectedTab, sv);
        }

        private void CloseStoreTab(TabPage tab, StoreView sv)
        {
            if (sv.IsDirty)
            {
                string name = sv.Store?.StoreName ?? "Untitled";
                var dr = MessageBox.Show(
                    $"Save changes to '{name}'?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (dr == DialogResult.Yes)
                {
                    RunSaveStoreFlow(sv, tab);
                    if (sv.IsDirty) return;
                }
                else if (dr == DialogResult.Cancel)
                    return;
            }

            // Clear cached password before removing the tab
            if (sv.Store != null)
                sv.Store.CachedPassword = null;

            _storeTabs.Remove(tab);
            tabMain.TabPages.Remove(tab);
            UpdateMenuState();
        }

        private void MenuExport_Click(object? sender, EventArgs e)
        {
            if (!_settings.CsvExportWarningAccepted)
            {
                using var warn = new ExportWarningDialog(_settings);
                if (warn.ShowDialog(this) != DialogResult.OK || !warn.Accepted) return;
            }

            using var sfd = new SaveFileDialog
            {
                Title      = "Export Passwords",
                Filter     = "CSV files (*.csv)|*.csv",
                DefaultExt = "csv"
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                int count = 0;

                if (tabMain.SelectedTab == tabSession)
                {
                    sb.AppendLine("\"Timestamp\",\"Settings\",\"Password\"");
                    foreach (var (ts, mode, length, pwd) in sessionView.GetEntries())
                    {
                        sb.AppendLine(
                            $"\"{ts:yyyy-MM-dd HH:mm:ss.fff}\"," +
                            $"\"{PasswordGenerator.ColumnLabel(mode, length)}\"," +
                            $"\"{EscapeCsv(pwd)}\"");
                        count++;
                    }
                }
                else if (tabMain.SelectedTab != null &&
                         _storeTabs.TryGetValue(tabMain.SelectedTab, out var sv))
                {
                    sb.AppendLine("\"Timestamp\",\"Settings\",\"Username\",\"Note\",\"Password\"");
                    foreach (var entry in sv.GetEntries())
                    {
                        sb.AppendLine(
                            $"\"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\"," +
                            $"\"{PasswordGenerator.ColumnLabel(entry.Mode, entry.Length)}\"," +
                            $"\"{EscapeCsv(entry.Label)}\"," +
                            $"\"{EscapeCsv(entry.Note)}\"," +
                            $"\"{EscapeCsv(entry.Password)}\"");
                        count++;
                    }
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(false));
                Log($"[{DateTime.Now:HH:mm:ss}] Exported {count} passwords to {Path.GetFileName(sfd.FileName)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string EscapeCsv(string? s) => (s ?? "").Replace("\"", "\"\"");

        private void MenuExit_Click(object? sender, EventArgs e) => Close();

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            foreach (var kv in _storeTabs)
            {
                if (kv.Value.IsDirty)
                {
                    string name = kv.Value.Store?.StoreName ?? "Untitled";
                    var dr = MessageBox.Show(
                        $"Store '{name}' has unsaved changes. Save before exit?",
                        "Unsaved Changes",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (dr == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                    if (dr == DialogResult.Yes)
                        RunSaveStoreFlow(kv.Value, kv.Key);
                }
            }

            if (!_settings.TrackAndClearEverSet)
            {
                var dr = MessageBox.Show(
                    "Clear session output and clipboard on exit?",
                    "Exit Behaviour",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    _settings.TrackAndClearGlobal  = true;
                    _settings.TrackAndClearEverSet = true;
                    _settings.Save();
                }
                else if (dr == DialogResult.No)
                {
                    _settings.TrackAndClearEverSet = true;
                    _settings.Save();
                }
            }

            if (_settings.TrackAndClearGlobal)
            {
                sessionView.Clear();
                ClipboardTracker.ClearIfOurs();
            }

            // Clear all cached passwords before exit
            foreach (var kv in _storeTabs)
                if (kv.Value.Store != null)
                    kv.Value.Store.CachedPassword = null;

            base.OnFormClosing(e);
        }

        // ── Edit Menu Handlers ────────────────────────────────────────────────────

        private void MenuCopy_Click(object? sender, EventArgs e)
        {
            string? text = GetSelectedPassword();
            if (text != null)
            {
                ClipboardTracker.SetText(text);
                Log($"[{DateTime.Now:HH:mm:ss}] Copied to clipboard.");
            }
        }

        private string? GetSelectedPassword()
        {
            if (tabMain.SelectedTab == tabSession)
                return sessionView.SelectedPassword;

            if (tabMain.SelectedTab != null &&
                _storeTabs.TryGetValue(tabMain.SelectedTab, out var sv))
                return sv.SelectedPassword;

            return null;
        }

        private void MenuPaste_Click(object? sender, EventArgs e)
        {
            Log($"[{DateTime.Now:HH:mm:ss}] Paste: no editable target in this view.");
        }

        private void MenuSelectAll_Click(object? sender, EventArgs e)
        {
            if (tabMain.SelectedTab == tabSession)
                sessionView.SelectAll();
            else if (tabMain.SelectedTab != null &&
                     _storeTabs.TryGetValue(tabMain.SelectedTab, out var sv))
                sv.SelectAll();
        }

        private void MenuClearClipboard_Click(object? sender, EventArgs e)
        {
            ClipboardTracker.ClearAll();
            Log($"[{DateTime.Now:HH:mm:ss}] Clipboard cleared.");
        }

        private void MenuClearSettings_Click(object? sender, EventArgs e)
        {
            var dr = MessageBox.Show(
                "Reset all settings to defaults? Store files are not affected.",
                "Reset Settings",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (dr != DialogResult.OK) return;

            _settings.ResetToDefaults();
            _settings.Save();

            nudLength.Value = _settings.DefaultLength;
            nudCount.Value  = _settings.DefaultCount;
            switch (_settings.DefaultMode)
            {
                case 1: radNix.Checked   = true; break;
                case 2: radAzure.Checked = true; break;
                default: radWeb.Checked  = true; break;
            }
            bool dark = _settings.Theme == 2 || (_settings.Theme == 0 && Theme.IsSystemDark());
            Theme.Apply(dark);
            ApplyTheme(dark);
            StartStoreCheckTimer();
            Log($"[{DateTime.Now:HH:mm:ss}] Settings reset to defaults.");
        }

        private void MenuPreferences_Click(object? sender, EventArgs e)
        {
            using var dlg = new PreferencesDialog(_settings);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                bool dark = _settings.Theme == 2 || (_settings.Theme == 0 && Theme.IsSystemDark());
                Theme.Apply(dark);
                ApplyTheme(dark);
                StartStoreCheckTimer();
                Log($"[{DateTime.Now:HH:mm:ss}] Preferences saved.");
            }
        }

        // ── Help Menu ─────────────────────────────────────────────────────────────

        private void MenuHelp_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Help documentation is being prepared.", "Help",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MenuAbout_Click(object? sender, EventArgs e)
        {
            using var dlg = new AboutDialog();
            dlg.ShowDialog(this);
        }

        // ── Tab Management ────────────────────────────────────────────────────────

        private void AddStoreTab(LpgeStore store)
        {
            var tab = new TabPage(store.StoreName);
            tab.BackColor = Theme.PanelBg;
            var sv = new StoreView(_settings);
            sv.LoadStore(store);
            sv.ApplyTheme();
            sv.DeleteRequested += HandleDeleteRequested;
            tab.Controls.Add(sv);
            _storeTabs[tab] = sv;
            tabMain.TabPages.Add(tab);
            tabMain.SelectedTab = tab;
            UpdateMenuState();
        }

        private void UpdateStoreTabTitle(StoreView sv)
        {
            foreach (var kv in _storeTabs)
            {
                if (kv.Value == sv)
                {
                    string name = sv.Store?.StoreName ?? "Untitled";
                    kv.Key.Text = sv.IsDirty ? name + " *" : name;
                    break;
                }
            }
        }

        private void TabMain_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateMenuState();
        }

        private void UpdateMenuState()
        {
            bool storeActive = tabMain.SelectedTab != null && tabMain.SelectedTab != tabSession;
            menuSaveStore.Enabled  = storeActive;
            menuCloseStore.Enabled = storeActive;
        }

        // ── Delete Row Handling ───────────────────────────────────────────────────

        private void HandleDeleteRequested(object? sender, EventArgs e)
        {
            int count = sender is SessionView sv1 ? sv1.SelectedRowCount
                      : sender is StoreView sv2   ? sv2.SelectedRowCount
                      : 0;
            if (count == 0) return;

            bool needConfirm = _settings.DeleteConfirmGlobal && !_suppressDeleteConfirmSession;
            if (needConfirm)
            {
                var result = ShowDeleteConfirmDialog(count, out bool neverSession, out bool neverGlobal);
                if (result != DialogResult.Yes) return;
                if (neverSession) _suppressDeleteConfirmSession = true;
                if (neverGlobal)
                {
                    _settings.DeleteConfirmGlobal = false;
                    _settings.Save();
                }
            }

            if (sender is SessionView sess)
            {
                sess.DeleteSelectedRows();
            }
            else if (sender is StoreView sv)
            {
                sv.DeleteSelectedRows();
                UpdateStoreTabTitle(sv);
            }

            Log($"[{DateTime.Now:HH:mm:ss}] Deleted {count} row{(count != 1 ? "s" : "")}.");
        }

        private DialogResult ShowDeleteConfirmDialog(int count, out bool neverSession, out bool neverGlobal)
        {
            neverSession = false;
            neverGlobal  = false;

            using var dlg = new Form
            {
                Text            = "Confirm Delete",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                ShowInTaskbar   = false,
                ClientSize      = new Size(400, 164),
                Font            = new Font("Segoe UI", 9f),
                BackColor       = Theme.FormBg,
                ForeColor       = Theme.PrimaryText
            };
            Theme.SetTitleBarDark(dlg.Handle, Theme.IsDark);

            var lbl = new Label
            {
                Text      = $"Delete {count} selected row{(count != 1 ? "s" : "")}?",
                Location  = new Point(12, 16),
                AutoSize  = true,
                BackColor = Theme.FormBg,
                ForeColor = Theme.PrimaryText
            };
            var chkSession = new CheckBox
            {
                Text      = "Never ask again this session",
                Location  = new Point(12, 52),
                AutoSize  = true,
                BackColor = Theme.FormBg,
                ForeColor = Theme.PrimaryText
            };
            var chkGlobal = new CheckBox
            {
                Text      = "Never ask again (global — saves to config)",
                Location  = new Point(12, 76),
                AutoSize  = true,
                BackColor = Theme.FormBg,
                ForeColor = Theme.MutedText
            };
            var btnDelete = new Button
            {
                Text      = "Delete",
                Location  = new Point(214, 124),
                Width     = 80,
                BackColor = Color.FromArgb(0xC4, 0x2B, 0x1C),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            var btnNo = new Button
            {
                Text         = "Cancel",
                DialogResult = DialogResult.No,
                Location     = new Point(302, 124),
                Width        = 80,
                BackColor    = Theme.ToolbarBg,
                ForeColor    = Theme.PrimaryText,
                FlatStyle    = FlatStyle.Flat
            };

            DialogResult result = DialogResult.No;
            btnDelete.Click += (s, ev) => { result = DialogResult.Yes; dlg.Close(); };
            btnNo.Click     += (s, ev) => { result = DialogResult.No;  dlg.Close(); };

            dlg.Controls.AddRange(new Control[] { lbl, chkSession, chkGlobal, btnDelete, btnNo });
            dlg.ShowDialog(this);

            neverSession = chkSession.Checked;
            neverGlobal  = chkGlobal.Checked;
            return result;
        }

        // ── Theme Application ─────────────────────────────────────────────────────

        private void ApplyTheme(bool dark)
        {
            Theme.Apply(dark);
            Theme.SetTitleBarDark(Handle, dark);

            BackColor = Theme.FormBg;
            ForeColor = Theme.PrimaryText;

            pSidebar.BackColor = Theme.PanelBg;

            foreach (Control c in pSidebar.Controls)
            {
                c.BackColor = Theme.PanelBg;
                c.ForeColor = Theme.PrimaryText;
                if (c is Label lbl && lbl.Font.Bold)
                    lbl.ForeColor = Theme.MutedText;
                if (c is Panel sep && sep.Height == 1)
                    sep.BackColor = Theme.SepColor;
                if (c is NumericUpDown n)
                {
                    n.BackColor = Theme.InputBg;
                    n.ForeColor = Theme.InputFg;
                }
                if (c is RadioButton rb)
                {
                    rb.BackColor = Theme.PanelBg;
                    rb.ForeColor = Theme.PrimaryText;
                }
            }

            btnGenerate.BackColor = Theme.AccentBtn;
            btnGenerate.ForeColor = Color.White;

            txtLog.BackColor = Theme.LogBg;
            txtLog.ForeColor = Theme.LogFg;

            pSepV.BackColor = Theme.SepColor;

            // Status strip — clocks use PrimaryText for visibility
            statusStrip.BackColor  = Theme.StatusBg;
            lblUTC.ForeColor       = Theme.PrimaryText;
            lblTimeSep.ForeColor   = Theme.StatusFg;
            lblLocal.ForeColor     = Theme.PrimaryText;
            lblEncryption.ForeColor = Theme.MutedText;

            Theme.StyleMenuStrip(menuStrip);

            tabMain.BackColor    = Theme.FormBg;
            tabSession.BackColor = Theme.PanelBg;
            foreach (var kv in _storeTabs)
            {
                kv.Key.BackColor = Theme.PanelBg;
                kv.Value.ApplyTheme();
            }

            sessionView?.ApplyTheme();
        }

        // ── Timers ────────────────────────────────────────────────────────────────

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            lblUTC.Text   = $"UTC {now.ToUniversalTime():HH:mm:ss.ff}";
            lblLocal.Text = $"Local {now:HH:mm:ss.ff}";
        }

        private void ReseedTimer_Tick(object? sender, EventArgs e) => ReseedRandom();

        private void ReseedRandom()
        {
            byte[] buf = new byte[4];
            RandomNumberGenerator.Fill(buf);
            _ran = new Random(BitConverter.ToInt32(buf));
        }

        private void StartClipboardTimer()
        {
            _clipTimer?.Stop();
            _clipTimer = new System.Windows.Forms.Timer
            {
                Interval = _settings.ClipboardTimeoutSeconds * 1000
            };
            _clipTimer.Tick += (s, e) =>
            {
                _clipTimer.Stop();
                ClipboardTracker.ClearIfOurs();
                Log($"[{DateTime.Now:HH:mm:ss}] Clipboard auto-cleared (timeout).");
            };
            _clipTimer.Start();
        }

        private void StartStoreCheckTimer()
        {
            _storeCheckTimer?.Stop();
            _storeCheckTimer = new System.Windows.Forms.Timer
            {
                Interval = Math.Max(1, _settings.StoreCheckIntervalSeconds) * 1000
            };
            _storeCheckTimer.Tick += (s, e) =>
            {
                foreach (var kv in _storeTabs)
                    kv.Value.RefreshDirtyIndicator();
            };
            _storeCheckTimer.Start();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void Log(string message)
        {
            txtLog.AppendText(message + Environment.NewLine);
            if (txtLog.Lines.Length > LogMaxLines)
                txtLog.Lines = txtLog.Lines[^LogTrimTo..];
        }

        // ── nudLength Validation ──────────────────────────────────────────────────

        private void NudLength_ValueChanged(object? sender, EventArgs e)
        {
            decimal val = nudLength.Value;
            if (val >= InvalidLengthMin && val <= InvalidLengthMax)
            {
                nudLength.ValueChanged -= NudLength_ValueChanged;
                nudLength.Value = _lastValidLength;
                nudLength.ValueChanged += NudLength_ValueChanged;
                return;
            }
            _lastValidLength = val;
        }
    }
}
