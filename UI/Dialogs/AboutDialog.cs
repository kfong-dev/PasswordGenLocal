using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using PasswordGenLocal.Core;
using PasswordGenLocal.UI;

namespace PasswordGenLocal.UI.Dialogs
{
    public class AboutDialog : Form
    {
        private const string MIT_TEXT =
            "MIT License\r\n\r\n" +
            "Copyright (c) 2025 Password Generator \u2014 Local Contributors\r\n\r\n" +
            "Permission is hereby granted, free of charge, to any person obtaining a copy " +
            "of this software and associated documentation files (the \"Software\"), to deal " +
            "in the Software without restriction, including without limitation the rights " +
            "to use, copy, modify, merge, publish, distribute, sublicense, and/or sell " +
            "copies of the Software, and to permit persons to whom the Software is " +
            "furnished to do so, subject to the following conditions:\r\n\r\n" +
            "The above copyright notice and this permission notice shall be included in all " +
            "copies or substantial portions of the Software.\r\n\r\n" +
            "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR " +
            "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
            "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE " +
            "AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER " +
            "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, " +
            "OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE " +
            "SOFTWARE.\r\n\r\n" +
            "---\r\n" +
            "Concept inspired by Jan Pisa\u2019s RORG Password Generator.";

        private readonly Panel pLogo;
        private readonly RichTextBox rtfLicense;
        private readonly Button btnOk;

        public AboutDialog()
        {
            Text            = "About";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            ClientSize      = new Size(440, 390);
            Font            = new Font("Segoe UI", 9f);

            int y = 20;

            // ── Logo placeholder + App name ───────────────────────────────────────
            pLogo = new Panel
            {
                Location  = new Point(20, y),
                Size      = new Size(48, 48),
                BackColor = Theme.AccentBtn
            };
            var lblInitials = new Label
            {
                Text      = "PG",
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            pLogo.Controls.Add(lblInitials);

            var lblName = new Label
            {
                Text      = "Password Generator \u2014 Local",
                Location  = new Point(78, y + 4),
                Size      = new Size(342, 36),
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            y += 64;

            // ── Version ───────────────────────────────────────────────────────────
            string version = "1.0.0.0";
            try
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                if (v != null) version = v.ToString();
            }
            catch { }

            var lblVersion = new Label
            {
                Text     = $"Version {version}",
                Location = new Point(20, y),
                AutoSize = true
            };
            y += 26;

            // ── License text box ─────────────────────────────────────────────────
            rtfLicense = new RichTextBox
            {
                Location   = new Point(20, y),
                Size       = new Size(400, 140),
                Text       = MIT_TEXT,
                ReadOnly   = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Font       = new Font("Segoe UI", 8.25f),
                BorderStyle = BorderStyle.FixedSingle,
                WordWrap   = true,
                // Suppress any link detection — no clickable URLs
                DetectUrls = false
            };
            y += 150;

            // ── Separator ─────────────────────────────────────────────────────────
            var sep = new Panel
            {
                Location = new Point(20, y),
                Size     = new Size(400, 1)
            };
            y += 10;

            // ── Workstation status ────────────────────────────────────────────────
            string joinDesc;
            try   { joinDesc = EnterpriseDetector.Describe(EnterpriseDetector.Detect()); }
            catch { joinDesc = "Detection unavailable"; }

            var lblWorkstation = new Label
            {
                Text     = $"Workstation:  {joinDesc}",
                Location = new Point(20, y),
                Size     = new Size(400, 36),
                AutoSize = false
            };
            y += 44;

            // ── OK button ─────────────────────────────────────────────────────────
            btnOk = new Button
            {
                Text         = "OK",
                DialogResult = DialogResult.OK,
                Location     = new Point(336, y),
                Width        = 84,
                Height       = 28
            };

            AcceptButton = btnOk;

            Controls.Add(pLogo);
            Controls.Add(lblName);
            Controls.Add(lblVersion);
            Controls.Add(rtfLicense);
            Controls.Add(sep);
            Controls.Add(lblWorkstation);
            Controls.Add(btnOk);

            Load += (s, e) => ApplyTheme();
        }

        private void ApplyTheme()
        {
            BackColor = Theme.FormBg;
            ForeColor = Theme.PrimaryText;
            Theme.SetTitleBarDark(Handle, Theme.IsDark);

            foreach (Control c in Controls)
            {
                if (c is Panel p && p.Height == 1)
                {
                    p.BackColor = Theme.SepColor;
                    continue;
                }
                c.BackColor = Theme.FormBg;
                c.ForeColor = Theme.PrimaryText;
            }

            pLogo.BackColor      = Theme.AccentBtn;
            rtfLicense.BackColor = Theme.InputBg;
            rtfLicense.ForeColor = Theme.InputFg;

            btnOk.BackColor = Theme.AccentBtn;
            btnOk.ForeColor = Color.White;
            btnOk.FlatStyle = FlatStyle.Flat;
        }
    }
}
