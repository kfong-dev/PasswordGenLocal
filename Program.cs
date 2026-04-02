using System;
using System.Windows.Forms;
using PasswordGenLocal.UI;

namespace PasswordGenLocal
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Last-resort handler for non-UI-thread crashes (e.g. finalizer exceptions).
            // The app cannot continue after this fires, but we log before the process exits.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                AppLog.Write("CRASH",
                    ex?.Message ?? "Unknown exception",
                    ex?.ToString() ?? string.Empty);
            };

            // Handler for unhandled exceptions thrown on the UI thread or thread pool.
            // Logs the full stack trace and shows a brief message to the user.
            Application.ThreadException += (s, e) =>
            {
                AppLog.Write("THREAD_EXCEPTION",
                    e.Exception.Message,
                    e.Exception.ToString());
                MessageBox.Show(
                    $"An unexpected error occurred:\n{e.Exception.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            // Route WinForms thread exceptions through the handler above instead of the
            // default "send to debugger or terminate silently" behaviour.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
