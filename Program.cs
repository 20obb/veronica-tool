using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BypassTool
{
    /// <summary>
    /// Application entry point
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Enable visual styles for modern UI
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Set up global exception handling
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                // Initialize logger
                Utils.Logger.Instance.Info("BypassTool starting...");
                Utils.Logger.Instance.Info($"Version: {Application.ProductVersion}");
                Utils.Logger.Instance.Info($"OS: {Environment.OSVersion}");
                Utils.Logger.Instance.Info($".NET Runtime: {Environment.Version}");

                // Run main form
                Application.Run(new UI.MainForm());

                Utils.Logger.Instance.Info("BypassTool shutting down normally.");
            }
            catch (Exception ex)
            {
                Utils.Logger.Instance.Error($"Fatal error during startup: {ex.Message}");
                Utils.Logger.Instance.Error(ex.StackTrace);
                
                MessageBox.Show(
                    $"A fatal error occurred during startup:\n\n{ex.Message}\n\nPlease check the logs for more details.",
                    "BypassTool - Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Handles unhandled exceptions on the UI thread
        /// </summary>
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Utils.Logger.Instance.Error($"Unhandled UI thread exception: {e.Exception.Message}");
            Utils.Logger.Instance.Error(e.Exception.StackTrace);

            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe application will attempt to continue.",
                "BypassTool - Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        /// <summary>
        /// Handles unhandled exceptions on non-UI threads
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Utils.Logger.Instance.Error($"Unhandled domain exception: {exception?.Message ?? "Unknown error"}");
            
            if (exception != null)
            {
                Utils.Logger.Instance.Error(exception.StackTrace);
            }

            if (e.IsTerminating)
            {
                MessageBox.Show(
                    $"A fatal error occurred:\n\n{exception?.Message ?? "Unknown error"}\n\nThe application will now close.",
                    "BypassTool - Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
