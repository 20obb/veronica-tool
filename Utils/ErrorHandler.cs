using System;
using System.Text;
using System.Windows.Forms;
using BypassTool.Models;

namespace BypassTool.Utils
{
    /// <summary>
    /// Centralized error handling and user-friendly error messages
    /// </summary>
    public static class ErrorHandler
    {
        #region Fields

        private static readonly Logger _logger = Logger.Instance;

        #endregion

        #region Error Handling Methods

        /// <summary>
        /// Handles an exception and returns a user-friendly message
        /// </summary>
        public static string HandleException(Exception ex, string context = null)
        {
            if (ex == null)
                return "An unknown error occurred";

            // Log the full exception
            string fullContext = string.IsNullOrEmpty(context) ? "General" : context;
            _logger.Error($"[{fullContext}] {ex.Message}");
            _logger.Debug($"Exception type: {ex.GetType().FullName}");
            _logger.Debug($"Stack trace:\n{ex.StackTrace}");

            if (ex.InnerException != null)
            {
                _logger.Debug($"Inner exception: {ex.InnerException.Message}");
            }

            // Return user-friendly message
            return GetUserFriendlyMessage(ex);
        }

        /// <summary>
        /// Handles an exception and shows a message box
        /// </summary>
        public static void HandleExceptionWithDialog(Exception ex, string title = "Error", string context = null)
        {
            string message = HandleException(ex, context);
            
            MessageBox.Show(
                message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        /// <summary>
        /// Creates a BypassResult from an exception
        /// </summary>
        public static BypassResult CreateFailureResult(Exception ex, BypassStep step, string context = null)
        {
            string message = HandleException(ex, context);
            
            return BypassResult.FailureResult(message, step, ex);
        }

        #endregion

        #region User-Friendly Messages

        /// <summary>
        /// Converts an exception to a user-friendly message
        /// </summary>
        private static string GetUserFriendlyMessage(Exception ex)
        {
            // Handle specific exception types
            switch (ex)
            {
                case UnauthorizedAccessException _:
                    return "Access denied. Please run the application as administrator.";

                case TimeoutException _:
                    return "The operation timed out. Please check device connection and try again.";

                case System.Net.Sockets.SocketException sockEx:
                    return GetSocketErrorMessage(sockEx);

                case System.IO.FileNotFoundException fnfEx:
                    return $"Required file not found: {fnfEx.FileName}";

                case System.IO.IOException ioEx:
                    return $"I/O error: {ioEx.Message}. Please check file access permissions.";

                case InvalidOperationException invEx:
                    return $"Invalid operation: {invEx.Message}";

                case ArgumentException argEx:
                    return $"Invalid argument: {argEx.Message}";

                case NotSupportedException nsEx:
                    return $"This operation is not supported: {nsEx.Message}";

                case AggregateException aggEx:
                    return HandleAggregateException(aggEx);

                default:
                    // Check for known error patterns in message
                    return GetMessageFromPattern(ex.Message) ?? ex.Message;
            }
        }

        /// <summary>
        /// Gets a user-friendly message for socket errors
        /// </summary>
        private static string GetSocketErrorMessage(System.Net.Sockets.SocketException ex)
        {
            return ex.SocketErrorCode switch
            {
                System.Net.Sockets.SocketError.ConnectionRefused =>
                    "Connection refused. Make sure the device is connected and SSH is enabled.",
                    
                System.Net.Sockets.SocketError.HostNotFound =>
                    "Device not found. Please check the IP address and network connection.",
                    
                System.Net.Sockets.SocketError.TimedOut =>
                    "Connection timed out. Please check the device is reachable.",
                    
                System.Net.Sockets.SocketError.HostUnreachable =>
                    "Device is unreachable. Make sure it's on the same network.",
                    
                System.Net.Sockets.SocketError.NetworkUnreachable =>
                    "Network is unreachable. Please check your network connection.",
                    
                _ => $"Network error: {ex.Message}"
            };
        }

        /// <summary>
        /// Handles aggregate exception
        /// </summary>
        private static string HandleAggregateException(AggregateException ex)
        {
            if (ex.InnerExceptions.Count == 1)
            {
                return GetUserFriendlyMessage(ex.InnerExceptions[0]);
            }

            var sb = new StringBuilder("Multiple errors occurred:\n");
            int count = 0;
            foreach (var innerEx in ex.InnerExceptions)
            {
                if (count++ >= 3)
                {
                    sb.AppendLine($"  ...and {ex.InnerExceptions.Count - 3} more errors");
                    break;
                }
                sb.AppendLine($"  - {GetUserFriendlyMessage(innerEx)}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets a user-friendly message based on error patterns
        /// </summary>
        private static string GetMessageFromPattern(string message)
        {
            if (string.IsNullOrEmpty(message))
                return null;

            string msgLower = message.ToLowerInvariant();

            if (msgLower.Contains("no device") || msgLower.Contains("device not found"))
                return "No iOS device detected. Please connect a device via USB and try again.";

            if (msgLower.Contains("lockdown") && msgLower.Contains("error"))
                return "Failed to communicate with device. Please unlock the device and trust this computer.";

            if (msgLower.Contains("ssh") && msgLower.Contains("authentication"))
                return "SSH authentication failed. Check the username and password (default: root/alpine).";

            if (msgLower.Contains("permission denied"))
                return "Permission denied. The device may not be jailbroken or SSH may not be running.";

            if (msgLower.Contains("connection reset"))
                return "Connection was reset by the device. Please reconnect and try again.";

            if (msgLower.Contains("activation"))
                return "Activation service error. The device may already be activated or locked.";

            if (msgLower.Contains("plist"))
                return "Failed to process device configuration file. The file may be corrupted.";

            return null;
        }

        #endregion

        #region Validation Helpers

        /// <summary>
        /// Validates that a parameter is not null
        /// </summary>
        public static void ValidateNotNull(object value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName, $"{paramName} cannot be null");
            }
        }

        /// <summary>
        /// Validates that a string is not null or empty
        /// </summary>
        public static void ValidateNotEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
            }
        }

        /// <summary>
        /// Validates that a device is connected
        /// </summary>
        public static void ValidateDeviceConnected(DeviceInfo device, string operation)
        {
            if (device == null)
            {
                throw new InvalidOperationException($"Cannot {operation}: No device connected");
            }

            if (!device.IsConnected)
            {
                throw new InvalidOperationException($"Cannot {operation}: Device is not connected");
            }
        }

        /// <summary>
        /// Validates iOS version support
        /// </summary>
        public static void ValidateIOSVersion(DeviceInfo device, int minMajor, int maxMajor)
        {
            if (device == null)
                return;

            int major = device.IOSMajorVersion;
            
            if (major < minMajor || major > maxMajor)
            {
                throw new NotSupportedException(
                    $"iOS {device.ProductVersion} is not supported. Supported versions: iOS {minMajor} - {maxMajor}");
            }
        }

        #endregion

        #region Result Helpers

        /// <summary>
        /// Wraps an action in error handling and returns a BypassResult
        /// </summary>
        public static BypassResult TryExecute(Action action, BypassStep step, string operationName)
        {
            try
            {
                _logger.Info($"Starting: {operationName}");
                action();
                _logger.Info($"Completed: {operationName}");
                return BypassResult.InProgress(step, 0, operationName);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, step, operationName);
            }
        }

        /// <summary>
        /// Wraps a function in error handling and returns the result or default
        /// </summary>
        public static T TryExecute<T>(Func<T> func, T defaultValue, string operationName)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return defaultValue;
            }
        }

        #endregion
    }
}
