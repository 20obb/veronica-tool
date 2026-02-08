using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;

namespace BypassTool.Utils
{
    /// <summary>
    /// Thread-safe logging system with file and console output
    /// </summary>
    public sealed class Logger : IDisposable
    {
        #region Singleton

        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        
        /// <summary>
        /// Gets the singleton logger instance
        /// </summary>
        public static Logger Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly string _logDirectory;
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();
        private readonly StreamWriter _writer;
        private readonly LogLevel _minLevel;
        private bool _disposed;

        /// <summary>
        /// Event raised when a log message is written
        /// </summary>
        public event EventHandler<LogEventArgs> LogWritten;

        #endregion

        #region Constructor

        private Logger()
        {
            // Get log directory from config or use default
            string configLogDir = ConfigurationManager.AppSettings["LogDirectory"];
            _logDirectory = string.IsNullOrEmpty(configLogDir)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "logs")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configLogDir);

            // Create log directory
            Directory.CreateDirectory(_logDirectory);

            // Create log file with timestamp
            string fileName = $"bypass_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            _logFilePath = Path.Combine(_logDirectory, fileName);

            // Open file stream
            _writer = new StreamWriter(_logFilePath, true, Encoding.UTF8)
            {
                AutoFlush = true
            };

            // Get log level from config
            string levelConfig = ConfigurationManager.AppSettings["LogLevel"];
            _minLevel = Enum.TryParse(levelConfig, true, out LogLevel level) ? level : LogLevel.Debug;

            // Write header
            WriteHeader();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Logs a debug message
        /// </summary>
        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// Logs a debug message with format
        /// </summary>
        public void Debug(string format, params object[] args)
        {
            Log(LogLevel.Debug, string.Format(format, args));
        }

        /// <summary>
        /// Logs an info message
        /// </summary>
        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// Logs an info message with format
        /// </summary>
        public void Info(string format, params object[] args)
        {
            Log(LogLevel.Info, string.Format(format, args));
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// Logs a warning message with format
        /// </summary>
        public void Warning(string format, params object[] args)
        {
            Log(LogLevel.Warning, string.Format(format, args));
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        /// <summary>
        /// Logs an error message with format
        /// </summary>
        public void Error(string format, params object[] args)
        {
            Log(LogLevel.Error, string.Format(format, args));
        }

        /// <summary>
        /// Logs an exception
        /// </summary>
        public void Error(string message, Exception ex)
        {
            Log(LogLevel.Error, $"{message}: {ex.Message}");
            Log(LogLevel.Debug, ex.StackTrace ?? "No stack trace available");
            
            if (ex.InnerException != null)
            {
                Log(LogLevel.Debug, $"Inner exception: {ex.InnerException.Message}");
            }
        }

        /// <summary>
        /// Logs a fatal error message
        /// </summary>
        public void Fatal(string message)
        {
            Log(LogLevel.Fatal, message);
        }

        /// <summary>
        /// Logs a fatal exception
        /// </summary>
        public void Fatal(string message, Exception ex)
        {
            Log(LogLevel.Fatal, $"{message}: {ex.Message}");
            Log(LogLevel.Fatal, ex.StackTrace ?? "No stack trace available");
        }

        /// <summary>
        /// Gets the current log file path
        /// </summary>
        public string GetLogFilePath() => _logFilePath;

        /// <summary>
        /// Gets the log directory path
        /// </summary>
        public string GetLogDirectory() => _logDirectory;

        #endregion

        #region Private Methods

        private void Log(LogLevel level, string message)
        {
            if (_disposed || level < _minLevel)
                return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string threadId = Thread.CurrentThread.ManagedThreadId.ToString().PadLeft(3);
            string levelStr = level.ToString().ToUpper().PadRight(5);
            string logLine = $"[{timestamp}] [{threadId}] [{levelStr}] {message}";

            lock (_lockObject)
            {
                try
                {
                    // Write to file
                    _writer.WriteLine(logLine);

                    // Write to console with color
                    WriteToConsole(level, logLine);

                    // Raise event for UI
                    LogWritten?.Invoke(this, new LogEventArgs(level, message, timestamp));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Logging error: {ex.Message}");
                }
            }
        }

        private void WriteToConsole(LogLevel level, string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = level switch
                {
                    LogLevel.Debug => ConsoleColor.Gray,
                    LogLevel.Info => ConsoleColor.White,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Fatal => ConsoleColor.DarkRed,
                    _ => ConsoleColor.White
                };

                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        private void WriteHeader()
        {
            string separator = new string('=', 80);
            _writer.WriteLine(separator);
            _writer.WriteLine($"BypassTool Log - Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer.WriteLine($"Machine: {Environment.MachineName}");
            _writer.WriteLine($"User: {Environment.UserName}");
            _writer.WriteLine($"OS: {Environment.OSVersion}");
            _writer.WriteLine($".NET Version: {Environment.Version}");
            _writer.WriteLine($"Log Level: {_minLevel}");
            _writer.WriteLine(separator);
            _writer.WriteLine();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            
            lock (_lockObject)
            {
                if (_disposed) return;
                _disposed = true;

                try
                {
                    _writer?.WriteLine();
                    _writer?.WriteLine($"Log ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    _writer?.Flush();
                    _writer?.Dispose();
                }
                catch { }
            }
        }

        #endregion
    }

    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }

    /// <summary>
    /// Event args for log events
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        public LogLevel Level { get; }
        public string Message { get; }
        public string Timestamp { get; }

        public LogEventArgs(LogLevel level, string message, string timestamp)
        {
            Level = level;
            Message = message;
            Timestamp = timestamp;
        }
    }
}
