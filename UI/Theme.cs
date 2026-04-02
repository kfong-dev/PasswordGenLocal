using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PasswordGenLocal.UI
{
    public static class Theme
    {
        public static bool IsDark { get; private set; } = false;

        public static void Apply(bool dark)
        {
            IsDark = dark;
        }

        // ── Colors ───────────────────────────────────────────────────────────────
        // Each property returns the correct value for the currently active theme.
        // Call Theme.Apply(bool dark) once per theme change; all properties update immediately.

        // -- Backgrounds & surfaces --
        public static Color FormBg        => IsDark ? Color.FromArgb(0x20, 0x20, 0x20) : Color.FromArgb(0xF3, 0xF3, 0xF3);
        public static Color ToolbarBg     => IsDark ? Color.FromArgb(0x26, 0x26, 0x26) : Color.FromArgb(0xE0, 0xE0, 0xE0);
        public static Color SepColor      => IsDark ? Color.FromArgb(0x3C, 0x3C, 0x3C) : Color.FromArgb(0xC8, 0xC8, 0xC8);
        public static Color PanelBg       => FormBg;

        // -- Text --
        public static Color PrimaryText   => IsDark ? Color.White                       : Color.Black;
        public static Color MutedText     => IsDark ? Color.FromArgb(0x96, 0x96, 0x96) : Color.FromArgb(0x64, 0x64, 0x64);

        // -- Input controls (NumericUpDown, TextBox) --
        public static Color InputBg       => IsDark ? Color.FromArgb(0x1E, 0x1E, 0x1E) : Color.White;
        public static Color InputFg       => IsDark ? Color.FromArgb(0xD4, 0xD4, 0xD4) : Color.Black;

        // -- In-UI log panel --
        public static Color LogBg         => IsDark ? Color.FromArgb(0x16, 0x16, 0x16) : Color.FromArgb(0xF9, 0xF9, 0xF9);
        public static Color LogFg         => IsDark ? Color.FromArgb(0x96, 0x96, 0x96) : Color.FromArgb(0x50, 0x50, 0x50);

        // -- Accent / action button (Windows blue palette) --
        public static Color AccentBtn      => IsDark ? Color.FromArgb(0x00, 0x78, 0xD4) : Color.FromArgb(0x00, 0x67, 0xC0);
        public static Color AccentBtnHover => IsDark ? Color.FromArgb(0x14, 0x84, 0xD9) : Color.FromArgb(0x00, 0x53, 0x99);

        // -- DataGridView --
        public static Color GridBg        => IsDark ? Color.FromArgb(0x1E, 0x1E, 0x1E) : Color.White;
        public static Color GridFg        => IsDark ? Color.FromArgb(0xD4, 0xD4, 0xD4) : Color.Black;
        public static Color GridHeaderBg  => IsDark ? Color.FromArgb(0x2D, 0x2D, 0x2D) : Color.FromArgb(0xF0, 0xF0, 0xF0);
        public static Color GridHeaderFg  => IsDark ? Color.White                       : Color.Black;
        public static Color GridSel       => IsDark ? Color.FromArgb(0x26, 0x4F, 0x78) : Color.FromArgb(0xCC, 0xE4, 0xF7);
        public static Color GridSelFg     => IsDark ? Color.White                       : Color.Black;
        public static Color GridAltRow    => IsDark ? Color.FromArgb(0x25, 0x25, 0x25) : Color.FromArgb(0xFA, 0xFA, 0xFA);
        public static Color GridLine      => IsDark ? Color.FromArgb(0x3C, 0x3C, 0x3C) : Color.FromArgb(0xE0, 0xE0, 0xE0);

        // -- Status strip --
        public static Color StatusBg      => IsDark ? Color.FromArgb(0x14, 0x14, 0x14) : Color.FromArgb(0xE6, 0xE6, 0xE6);
        public static Color StatusFg      => IsDark ? Color.FromArgb(0x8C, 0x8C, 0x8C) : Color.FromArgb(0x3C, 0x3C, 0x3C);

        // ── StyleGrid ────────────────────────────────────────────────────────────

        public static void StyleGrid(DataGridView g)
        {
            g.BackgroundColor             = GridBg;
            g.GridColor                   = GridLine;
            g.DefaultCellStyle.BackColor  = GridBg;
            g.DefaultCellStyle.ForeColor  = GridFg;
            g.DefaultCellStyle.SelectionBackColor = GridSel;
            g.DefaultCellStyle.SelectionForeColor = GridSelFg;
            g.AlternatingRowsDefaultCellStyle.BackColor = GridAltRow;
            g.AlternatingRowsDefaultCellStyle.ForeColor = GridFg;
            g.AlternatingRowsDefaultCellStyle.SelectionBackColor = GridSel;
            g.AlternatingRowsDefaultCellStyle.SelectionForeColor = GridSelFg;
            g.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBg;
            g.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderFg;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderBg;
            g.EnableHeadersVisualStyles   = false;
        }

        // ── StyleMenuStrip ────────────────────────────────────────────────────────

        public static void StyleMenuStrip(MenuStrip m)
        {
            m.BackColor = ToolbarBg;
            m.ForeColor = PrimaryText;
            foreach (ToolStripItem item in m.Items)
            {
                item.BackColor = ToolbarBg;
                item.ForeColor = PrimaryText;
            }
        }

        // ── IsSystemDark ─────────────────────────────────────────────────────────

        public static bool IsSystemDark()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    writable: false);
                if (key == null) return false;
                object? val = key.GetValue("AppsUseLightTheme");
                if (val is int i) return i == 0;
            }
            catch { }
            return false;
        }

        // ── SetTitleBarDark ───────────────────────────────────────────────────────

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void SetTitleBarDark(IntPtr hwnd, bool dark)
        {
            int value = dark ? 1 : 0;
            // Attribute 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (Win11 / 22000+)
            if (DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int)) != 0)
            {
                // Fallback: attribute 19 (pre-release Win10)
                DwmSetWindowAttribute(hwnd, 19, ref value, sizeof(int));
            }
        }
    }
}
