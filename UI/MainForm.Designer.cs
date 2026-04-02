using System;
using System.Drawing;
using System.Windows.Forms;
using PasswordGenLocal.Core;

namespace PasswordGenLocal.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Controls ──────────────────────────────────────────────────────────────
        private MenuStrip menuStrip = null!;
        private ToolStripMenuItem menuFile = null!;
        private ToolStripMenuItem menuNewStore = null!;
        private ToolStripMenuItem menuSaveStore = null!;
        private ToolStripMenuItem menuLoadStore = null!;
        private ToolStripMenuItem menuClearOutput = null!;
        private ToolStripMenuItem menuCloseStore = null!;
        private ToolStripMenuItem menuExport = null!;
        private ToolStripMenuItem menuExit = null!;

        private ToolStripMenuItem menuEdit = null!;
        private ToolStripMenuItem menuCopy = null!;
        private ToolStripMenuItem menuPaste = null!;
        private ToolStripMenuItem menuSelectAll = null!;
        private ToolStripMenuItem menuClearClipboard = null!;
        private ToolStripMenuItem menuClearSettings = null!;
        private ToolStripMenuItem menuPreferences = null!;

        private ToolStripMenuItem menuHelp = null!;
        private ToolStripMenuItem menuHelpItem = null!;
        private ToolStripMenuItem menuAbout = null!;

        private Panel pSidebar = null!;
        private Panel pSepV = null!;
        private Panel pRight = null!;

        // Sidebar section labels & separators
        private Label lblLengthSection = null!;
        private Panel sepLength = null!;
        private NumericUpDown nudLength = null!;

        private Label lblCountSection = null!;
        private Panel sepCount = null!;
        private NumericUpDown nudCount = null!;

        private Label lblCharsetSection = null!;
        private Panel sepCharset = null!;
        private RadioButton radWeb = null!;
        private RadioButton radNix = null!;
        private RadioButton radAzure = null!;

        private Button btnGenerate = null!;
        private TextBox txtLog = null!;

        private TabControl tabMain = null!;
        private TabPage tabSession = null!;

        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel lblUTC = null!;
        private ToolStripStatusLabel lblTimeSep = null!;
        private ToolStripStatusLabel lblLocal = null!;
        private ToolStripStatusLabel lblSpring = null!;
        private ToolStripStatusLabel lblEncryption = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            SuspendLayout();

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode       = AutoScaleMode.Font;

            var baseFont    = new Font("Segoe UI", 9f);
            var sectionFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            var controlFont = new Font("Segoe UI", 9.5f);
            var genFont     = new Font("Segoe UI", 10f, FontStyle.Bold);
            var logFont     = new Font("Consolas", 8.25f);
            var clockFont   = new Font("Consolas", 9.5f);

            // ── Form ──────────────────────────────────────────────────────────────
            Text          = "Password Generator \u2014 Local";
            Font          = baseFont;
            ClientSize    = new Size(960, 540);
            MinimumSize   = new Size(960, 540);
            StartPosition = FormStartPosition.CenterScreen;

            // ── MenuStrip ─────────────────────────────────────────────────────────
            menuStrip = new MenuStrip { Dock = DockStyle.Top, Font = baseFont };

            menuFile      = new ToolStripMenuItem("File");
            menuNewStore  = new ToolStripMenuItem("New Store")            { ShortcutKeys = Keys.Control | Keys.N };
            menuSaveStore = new ToolStripMenuItem("Save/Update Store")    { ShortcutKeys = Keys.Control | Keys.S, Enabled = false };
            menuLoadStore = new ToolStripMenuItem("Load Store...")         { ShortcutKeys = Keys.Control | Keys.O };
            menuClearOutput = new ToolStripMenuItem("Clear Output");
            menuCloseStore  = new ToolStripMenuItem("Close Store")        { ShortcutKeys = Keys.Control | Keys.W, Enabled = false };
            menuExport      = new ToolStripMenuItem("Export Passwords (Unsafe)...");
            menuExit        = new ToolStripMenuItem("Exit");

            menuNewStore.Click    += MenuNewStore_Click;
            menuSaveStore.Click   += MenuSaveStore_Click;
            menuLoadStore.Click   += MenuLoadStore_Click;
            menuClearOutput.Click += MenuClearOutput_Click;
            menuCloseStore.Click  += MenuCloseStore_Click;
            menuExport.Click      += MenuExport_Click;
            menuExit.Click        += MenuExit_Click;

            menuFile.DropDownItems.Add(menuNewStore);
            menuFile.DropDownItems.Add(new ToolStripSeparator());
            menuFile.DropDownItems.Add(menuSaveStore);
            menuFile.DropDownItems.Add(menuLoadStore);
            menuFile.DropDownItems.Add(new ToolStripSeparator());
            menuFile.DropDownItems.Add(menuClearOutput);
            menuFile.DropDownItems.Add(menuCloseStore);
            menuFile.DropDownItems.Add(new ToolStripSeparator());
            menuFile.DropDownItems.Add(menuExport);
            menuFile.DropDownItems.Add(new ToolStripSeparator());
            menuFile.DropDownItems.Add(menuExit);

            menuEdit          = new ToolStripMenuItem("Edit");
            menuCopy          = new ToolStripMenuItem("Copy")       { ShortcutKeys = Keys.Control | Keys.C };
            menuPaste         = new ToolStripMenuItem("Paste")      { ShortcutKeys = Keys.Control | Keys.V };
            menuSelectAll     = new ToolStripMenuItem("Select All") { ShortcutKeys = Keys.Control | Keys.A };
            menuClearClipboard = new ToolStripMenuItem("Clear Clipboard");
            menuClearSettings = new ToolStripMenuItem("Clear Settings");
            menuPreferences   = new ToolStripMenuItem("Preferences...") { ShortcutKeys = Keys.Control | Keys.P };

            menuCopy.Click           += MenuCopy_Click;
            menuPaste.Click          += MenuPaste_Click;
            menuSelectAll.Click      += MenuSelectAll_Click;
            menuClearClipboard.Click += MenuClearClipboard_Click;
            menuClearSettings.Click  += MenuClearSettings_Click;
            menuPreferences.Click    += MenuPreferences_Click;

            menuEdit.DropDownItems.Add(menuCopy);
            menuEdit.DropDownItems.Add(menuPaste);
            menuEdit.DropDownItems.Add(menuSelectAll);
            menuEdit.DropDownItems.Add(new ToolStripSeparator());
            menuEdit.DropDownItems.Add(menuClearClipboard);
            menuEdit.DropDownItems.Add(new ToolStripSeparator());
            menuEdit.DropDownItems.Add(menuClearSettings);
            menuEdit.DropDownItems.Add(new ToolStripSeparator());
            menuEdit.DropDownItems.Add(menuPreferences);

            menuHelp     = new ToolStripMenuItem("Help");
            menuHelpItem = new ToolStripMenuItem("Help") { ShortcutKeys = Keys.F1 };
            menuAbout    = new ToolStripMenuItem("About...");

            menuHelpItem.Click += MenuHelp_Click;
            menuAbout.Click    += MenuAbout_Click;

            menuHelp.DropDownItems.Add(menuHelpItem);
            menuHelp.DropDownItems.Add(new ToolStripSeparator());
            menuHelp.DropDownItems.Add(menuAbout);

            menuStrip.Items.Add(menuFile);
            menuStrip.Items.Add(menuEdit);
            menuStrip.Items.Add(menuHelp);

            // ── StatusStrip ───────────────────────────────────────────────────────
            // Layout: [UTC] [|] [Local] [Spring] [Encryption]
            // UTC and Local are grouped left for visual consolidation; encryption sits at far right.
            statusStrip = new StatusStrip { Dock = DockStyle.Bottom, Font = baseFont, SizingGrip = false };

            lblUTC = new ToolStripStatusLabel("UTC: --:--:--.--")
            {
                Font      = clockFont,
                ForeColor = System.Drawing.Color.Empty   // set by ApplyTheme
            };
            lblTimeSep = new ToolStripStatusLabel("  |  ")
            {
                Font      = clockFont,
                ForeColor = System.Drawing.Color.Empty
            };
            lblLocal = new ToolStripStatusLabel("Local: --:--:--.--")
            {
                Font      = clockFont,
                ForeColor = System.Drawing.Color.Empty
            };
            lblSpring = new ToolStripStatusLabel { Spring = true };
            lblEncryption = new ToolStripStatusLabel("AES-256-GCM  |  --")
            {
                Font      = new Font("Segoe UI", 8.25f),
                ForeColor = System.Drawing.Color.Empty
            };

            statusStrip.Items.Add(lblUTC);
            statusStrip.Items.Add(lblTimeSep);
            statusStrip.Items.Add(lblLocal);
            statusStrip.Items.Add(lblSpring);
            statusStrip.Items.Add(lblEncryption);

            // ── Sidebar ───────────────────────────────────────────────────────────
            const int SidebarWidth = 278;
            pSidebar = new Panel
            {
                Width   = SidebarWidth,
                Dock    = DockStyle.Left,
                Padding = new Padding(8, 8, 8, 8)
            };

            int y = 8;

            // PASSWORD LENGTH section
            lblLengthSection = new Label
            {
                Text     = "PASSWORD LENGTH",
                Location = new Point(8, y),
                AutoSize = true,
                Font     = sectionFont
            };
            y += 20;
            sepLength = new Panel { Location = new Point(8, y), Size = new Size(SidebarWidth - 16, 1) };
            y += 6;
            nudLength = new NumericUpDown
            {
                Location      = new Point(8, y),
                Width         = SidebarWidth - 16,
                Font          = controlFont,
                Minimum       = 7,
                Maximum       = 128,
                Value         = 16,
                DecimalPlaces = 0
            };
            nudLength.ValueChanged += NudLength_ValueChanged;
            y += 32;

            // NUMBER OF PASSWORDS section (formerly "Batch Count") — now above Character Set
            lblCountSection = new Label
            {
                Text     = "NUMBER OF PASSWORDS",
                Location = new Point(8, y),
                AutoSize = true,
                Font     = sectionFont
            };
            y += 20;
            sepCount = new Panel { Location = new Point(8, y), Size = new Size(SidebarWidth - 16, 1) };
            y += 6;
            nudCount = new NumericUpDown
            {
                Location      = new Point(8, y),
                Width         = SidebarWidth - 16,
                Font          = controlFont,
                Minimum       = 1,
                Maximum       = 128,
                Value         = 1,
                DecimalPlaces = 0
            };
            y += 40;

            // CHARACTER SET section
            lblCharsetSection = new Label
            {
                Text     = "CHARACTER SET",
                Location = new Point(8, y),
                AutoSize = true,
                Font     = sectionFont
            };
            y += 20;
            sepCharset = new Panel { Location = new Point(8, y), Size = new Size(SidebarWidth - 16, 1) };
            y += 6;
            radWeb = new RadioButton
            {
                Text     = PasswordGenerator.CharsetLabel(CharsetMode.Web),
                Location = new Point(8, y),
                AutoSize = true,
                Font     = controlFont,
                Checked  = true
            };
            y += 24;
            radNix = new RadioButton
            {
                Text     = PasswordGenerator.CharsetLabel(CharsetMode.Nix),
                Location = new Point(8, y),
                AutoSize = true,
                Font     = controlFont
            };
            y += 24;
            radAzure = new RadioButton
            {
                Text     = PasswordGenerator.CharsetLabel(CharsetMode.Azure),
                Location = new Point(8, y),
                AutoSize = true,
                Font     = controlFont
            };
            y += 32;

            // Generate button
            btnGenerate = new Button
            {
                Text      = "Generate",
                Location  = new Point(8, y),
                Size      = new Size(SidebarWidth - 16, 40),
                Font      = genFont,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBtn,
                ForeColor = System.Drawing.Color.White,
                Cursor    = Cursors.Hand
            };
            btnGenerate.FlatAppearance.BorderSize = 0;
            btnGenerate.Click += BtnGenerate_Click;
            y += 48;

            // Log textbox (fills remaining sidebar space)
            txtLog = new TextBox
            {
                Location    = new Point(8, y),
                Size        = new Size(SidebarWidth - 16, 0),
                Multiline   = true,
                ReadOnly    = true,
                ScrollBars  = ScrollBars.Vertical,
                Font        = logFont,
                BorderStyle = BorderStyle.None
            };

            pSidebar.Controls.Add(lblLengthSection);
            pSidebar.Controls.Add(sepLength);
            pSidebar.Controls.Add(nudLength);
            pSidebar.Controls.Add(lblCountSection);
            pSidebar.Controls.Add(sepCount);
            pSidebar.Controls.Add(nudCount);
            pSidebar.Controls.Add(lblCharsetSection);
            pSidebar.Controls.Add(sepCharset);
            pSidebar.Controls.Add(radWeb);
            pSidebar.Controls.Add(radNix);
            pSidebar.Controls.Add(radAzure);
            pSidebar.Controls.Add(btnGenerate);
            pSidebar.Controls.Add(txtLog);

            pSidebar.Layout += (s, e) =>
            {
                int logTop      = btnGenerate.Bottom + 8;
                int availWidth  = pSidebar.ClientSize.Width - 16;
                int availHeight = Math.Max(40, pSidebar.ClientSize.Height - logTop - 8);
                txtLog.Location = new Point(8, logTop);
                txtLog.Size     = new Size(availWidth, availHeight);
            };

            // ── Vertical separator ────────────────────────────────────────────────
            pSepV = new Panel { Width = 1, Dock = DockStyle.Left };

            // ── Right panel with TabControl ───────────────────────────────────────
            pRight = new Panel { Dock = DockStyle.Fill };

            tabMain    = new TabControl { Dock = DockStyle.Fill, Font = baseFont };
            tabSession = new TabPage("Session") { BackColor = System.Drawing.Color.White };
            tabMain.TabPages.Add(tabSession);
            tabMain.SelectedIndexChanged += TabMain_SelectedIndexChanged;

            pRight.Controls.Add(tabMain);

            Controls.Add(pRight);
            Controls.Add(pSepV);
            Controls.Add(pSidebar);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);

            MainMenuStrip = menuStrip;
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
