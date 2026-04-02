using System.Windows.Forms;

namespace PasswordGenLocal.Core
{
    /// <summary>
    /// Tracks whether this session wrote to the clipboard so we can conditionally clear.
    /// All Clipboard calls must be made from the UI thread (STAThread).
    /// </summary>
    public static class ClipboardTracker
    {
        public static bool WroteToClipboard { get; private set; } = false;

        /// <summary>Sets text to clipboard and marks that we wrote to it.</summary>
        public static void SetText(string text)
        {
            Clipboard.SetText(text);
            WroteToClipboard = true;
        }

        /// <summary>Clears clipboard only if we were the last to write to it this session.</summary>
        public static void ClearIfOurs()
        {
            if (WroteToClipboard)
            {
                try { Clipboard.Clear(); }
                catch { /* best effort */ }
                WroteToClipboard = false;
            }
        }

        /// <summary>Always clears the clipboard and resets the flag.</summary>
        public static void ClearAll()
        {
            try { Clipboard.Clear(); }
            catch { /* best effort */ }
            WroteToClipboard = false;
        }
    }
}
