using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;
using Microsoft.Win32;
using PasswordGenLocal.Core;
using PasswordGenLocal.UI;

namespace PasswordGenLocal.UI.Dialogs
{
    public class PreferencesDialog : Form
    {
        private readonly TabControl tabControl;
        private readonly TabPage tabGeneral;
        private readonly TabPage tabSecurity;
        private readonly TabPage tabStore;
        private readonly TabPage tabSession;
        private readonly TabPage tabGlobal;

        // General tab
        private readonly ComboBox cmbTheme;
        private readonly NumericUpDown nudLength;
        private readonly ComboBox cmbCharset;
        private readonly NumericUpDown nudCount;

        // Security tab
        private readonly CheckBox chkTrackAndClear;
        private readonly NumericUpDown nudClipTimeout;
        private readonly CheckBox chkAskOnGenerate;
        private readonly CheckBox chkRememberPassword;

        // Store Settings tab
        private readonly NumericUpDown nudStoreCheckInterval;

        // Session Settings tab
        private readonly CheckBox chkDeleteConfirm;

        // Global tab
        private readonly Button btnResetWarnings;
        private readonly Label lblHashAlgo;
        private readonly Label lblPbkdf2;
        private readonly Label lblAesGcm;
        private readonly Label lblFips;
        private readonly Label lblSha3Support;
        private readonly Label lblStoreAlgo;

        // Bottom
        private readonly Button btnSave;
        private readonly Button btnDiscard;

        public AppSettings Settings { get; }

        public PreferencesDialog(AppSettings settings)
        {
            Settings = settings;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;

            Text            = "Preferences";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            ClientSize      = new Size(500, 440);
            Font            = new Font("Segoe UI", 9f);

            tabGeneral  = new TabPage("General");
            tabSecurity = new TabPage("Security");
            tabStore    = new TabPage("Store Settings");
            tabSession  = new TabPage("Session Settings");
            tabGlobal   = new TabPage("Global (System)");

            // ── General Tab ──────────────────────────────────────────────────────

            int y = 16;
            AddLabel(tabGeneral, "Theme:", 12, y);
            cmbTheme = new ComboBox { Location = new Point(160, y - 2), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTheme.Items.AddRange(new object[] { "Follow System", "Light", "Dark" });
            cmbTheme.SelectedIndex = Math.Clamp(settings.Theme, 0, 2);
            tabGeneral.Controls.Add(cmbTheme);
            y += 36;

            AddLabel(tabGeneral, "Default Length:", 12, y);
            nudLength = new NumericUpDown { Location = new Point(160, y - 2), Width = 80, Minimum = 7, Maximum = 128, Value = Math.Clamp(settings.DefaultLength, 7, 128) };
            tabGeneral.Controls.Add(nudLength);
            y += 36;

            AddLabel(tabGeneral, "Default Charset:", 12, y);
            cmbCharset = new ComboBox { Location = new Point(160, y - 2), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCharset.Items.AddRange(new object[] { "Web / OAuth", "NIX / POSIX", "Azure AD / Kerberos" });
            cmbCharset.SelectedIndex = Math.Clamp(settings.DefaultMode, 0, 2);
            tabGeneral.Controls.Add(cmbCharset);
            y += 36;

            AddLabel(tabGeneral, "Default Count:", 12, y);
            nudCount = new NumericUpDown { Location = new Point(160, y - 2), Width = 80, Minimum = 1, Maximum = 128, Value = Math.Clamp(settings.DefaultCount, 1, 128) };
            tabGeneral.Controls.Add(nudCount);

            // ── Security Tab ─────────────────────────────────────────────────────

            y = 16;
            chkTrackAndClear = new CheckBox
            {
                Text     = "Clear clipboard and results on exit (global)",
                Location = new Point(12, y),
                AutoSize = true,
                Checked  = settings.TrackAndClearGlobal
            };
            tabSecurity.Controls.Add(chkTrackAndClear);
            y += 40;

            AddLabel(tabSecurity, "Clipboard auto-clear timeout:", 12, y);
            nudClipTimeout = new NumericUpDown { Location = new Point(210, y - 2), Width = 80, Minimum = 0, Maximum = 300, Value = settings.ClipboardTimeoutSeconds };
            AddLabel(tabSecurity, "seconds (0 = disabled)", 298, y);
            tabSecurity.Controls.Add(nudClipTimeout);
            y += 40;

            chkAskOnGenerate = new CheckBox
            {
                Text     = "Ask to create store on first generate",
                Location = new Point(12, y),
                AutoSize = true,
                Checked  = settings.AskOnGenerate
            };
            tabSecurity.Controls.Add(chkAskOnGenerate);
            y += 40;

            chkRememberPassword = new CheckBox
            {
                Text     = "Remember store password in session memory (reduces re-entry prompts)",
                Location = new Point(12, y),
                AutoSize = true,
                Checked  = settings.RememberStorePassword
            };
            tabSecurity.Controls.Add(chkRememberPassword);
            y += 28;

            AddLabel(tabSecurity,
                "\u26a0  Password is held in process memory until the store is closed or the\n" +
                "    application exits. May be visible to memory-inspection tools or\n" +
                "    persist in the page file on systems with hibernation enabled.",
                28, y, muted: true, wrap: true, width: 440);

            // ── Store Settings Tab ────────────────────────────────────────────────

            y = 16;
            AddLabel(tabStore, "Modification check interval:", 12, y);
            nudStoreCheckInterval = new NumericUpDown
            {
                Location = new Point(210, y - 2),
                Width    = 60,
                Minimum  = 1,
                Maximum  = 60,
                Value    = Math.Clamp(settings.StoreCheckIntervalSeconds, 1, 60)
            };
            AddLabel(tabStore, "seconds (1 – 60)", 278, y);
            tabStore.Controls.Add(nudStoreCheckInterval);
            y += 36;

            AddLabel(tabStore,
                "Controls how often the store header is refreshed to show the\n" +
                "unsaved-changes indicator below the Created / Modified timestamps.",
                12, y, muted: true, wrap: true, width: 450);

            // ── Session Settings Tab ──────────────────────────────────────────────

            y = 16;
            chkDeleteConfirm = new CheckBox
            {
                Text     = "Confirm before deleting rows (globally)",
                Location = new Point(12, y),
                AutoSize = true,
                Checked  = settings.DeleteConfirmGlobal
            };
            tabSession.Controls.Add(chkDeleteConfirm);
            y += 36;

            AddLabel(tabSession,
                "When unchecked, rows are deleted immediately without a prompt.\n" +
                "The per-session \"Never ask this session\" option in the delete dialog\n" +
                "also suppresses the prompt for the remainder of the current session.",
                12, y, muted: true, wrap: true, width: 450);

            // ── Global Tab ────────────────────────────────────────────────────────

            y = 16;
            btnResetWarnings = new Button
            {
                Text     = "Reset All Suppressed Warnings",
                Location = new Point(12, y),
                Width    = 220
            };
            btnResetWarnings.Click += (s, e) =>
            {
                settings.CsvExportWarningAccepted = false;
                settings.Save();
                MessageBox.Show("All suppressed warnings have been reset.", "Done",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            tabGlobal.Controls.Add(btnResetWarnings);
            y += 44;

            bool sha3Supported = StoreManager.UseSha3_384;
            bool fipsActive    = DetectFips();
            string algoInUse   = sha3Supported ? "SHA3-384 (48 bytes + 16-byte zero pad)" : "SHA-512 (64 bytes)";

            lblHashAlgo    = AddLabel(tabGlobal, $"Integrity hash algorithm: {algoInUse}", 12, y);       y += 24;
            lblPbkdf2      = AddLabel(tabGlobal, "PBKDF2 iterations: 210,000 (SHA-512)", 12, y);          y += 24;
            lblAesGcm      = AddLabel(tabGlobal, "Encryption: AES-256-GCM  |  FIPS compliant: Yes", 12, y); y += 24;
            lblSha3Support = AddLabel(tabGlobal, $"SHA3-384 hardware support: {(sha3Supported ? "Yes" : "No — using SHA-512 fallback")}", 12, y); y += 24;
            lblFips        = AddLabel(tabGlobal, $"System FIPS mode: {(fipsActive ? "Active" : "Inactive")}", 12, y); y += 24;
            lblStoreAlgo   = AddLabel(tabGlobal, $"Store integrity algorithm in use: {(sha3Supported ? "SHA3-384" : "SHA-512")}", 12, y);

            // ── TabControl ────────────────────────────────────────────────────────

            tabControl = new TabControl
            {
                Location = new Point(0, 0),
                Size     = new Size(500, 390),
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right
            };
            tabControl.TabPages.Add(tabGeneral);
            tabControl.TabPages.Add(tabSecurity);
            tabControl.TabPages.Add(tabStore);
            tabControl.TabPages.Add(tabSession);
            tabControl.TabPages.Add(tabGlobal);

            btnSave = new Button
            {
                Text     = "Save",
                Location = new Point(330, 400),
                Width    = 75,
                Anchor   = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnSave.Click += BtnSave_Click;

            btnDiscard = new Button
            {
                Text         = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location     = new Point(413, 400),
                Width        = 75,
                Anchor       = AnchorStyles.Bottom | AnchorStyles.Right
            };

            AcceptButton = btnSave;
            CancelButton = btnDiscard;

            Controls.Add(tabControl);
            Controls.Add(btnSave);
            Controls.Add(btnDiscard);

            Load += (s, e) => ApplyTheme();
        }

        private static Label AddLabel(Control parent, string text, int x, int y,
            bool muted = false, bool wrap = false, int width = 0)
        {
            var lbl = new Label
            {
                Text     = text,
                Location = new Point(x, y),
                AutoSize = !wrap
            };
            if (wrap)
            {
                lbl.AutoSize = false;
                lbl.Size     = new Size(width > 0 ? width : 400, 60);
            }
            parent.Controls.Add(lbl);
            return lbl;
        }

        private static bool DetectFips()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Lsa\FipsAlgorithmPolicy", writable: false);
                if (key == null) return false;
                object? val = key.GetValue("Enabled");
                return val is int i && i != 0;
            }
            catch { return false; }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            Settings.Theme                    = cmbTheme.SelectedIndex;
            Settings.DefaultLength            = (int)nudLength.Value;
            Settings.DefaultMode              = cmbCharset.SelectedIndex;
            Settings.DefaultCount             = (int)nudCount.Value;
            Settings.TrackAndClearGlobal      = chkTrackAndClear.Checked;
            Settings.ClipboardTimeoutSeconds  = (int)nudClipTimeout.Value;
            Settings.AskOnGenerate            = chkAskOnGenerate.Checked;
            Settings.RememberStorePassword    = chkRememberPassword.Checked;
            Settings.StoreCheckIntervalSeconds = (int)nudStoreCheckInterval.Value;
            Settings.DeleteConfirmGlobal      = chkDeleteConfirm.Checked;
            Settings.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplyTheme()
        {
            BackColor = Theme.FormBg;
            ForeColor = Theme.PrimaryText;
            Theme.SetTitleBarDark(Handle, Theme.IsDark);

            tabControl.BackColor = Theme.FormBg;
            foreach (TabPage tp in tabControl.TabPages)
            {
                tp.BackColor = Theme.FormBg;
                tp.ForeColor = Theme.PrimaryText;
                ApplyThemeToChildren(tp);
            }

            btnSave.BackColor    = Theme.AccentBtn;
            btnSave.ForeColor    = Color.White;
            btnSave.FlatStyle    = FlatStyle.Flat;
            btnDiscard.BackColor = Theme.ToolbarBg;
            btnDiscard.ForeColor = Theme.PrimaryText;
            btnDiscard.FlatStyle = FlatStyle.Flat;
        }

        private static void ApplyThemeToChildren(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                c.BackColor = Theme.FormBg;
                c.ForeColor = Theme.PrimaryText;
                if (c is TextBox t)           { t.BackColor  = Theme.InputBg; t.ForeColor  = Theme.InputFg; }
                if (c is ComboBox cb)         { cb.BackColor = Theme.InputBg; cb.ForeColor = Theme.InputFg; }
                if (c is NumericUpDown n)     { n.BackColor  = Theme.InputBg; n.ForeColor  = Theme.InputFg; }
                if (c is Button b)
                {
                    b.BackColor = Theme.ToolbarBg;
                    b.ForeColor = Theme.PrimaryText;
                    b.FlatStyle = FlatStyle.Flat;
                }
                ApplyThemeToChildren(c);
            }
        }
    }
}
