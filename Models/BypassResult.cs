using System;
using System.Collections.Generic;

namespace BypassTool.Models
{
    /// <summary>
    /// Represents the result of a bypass operation
    /// </summary>
    public class BypassResult
    {
        #region Properties

        /// <summary>
        /// Whether the bypass operation succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Detailed error information for debugging
        /// </summary>
        public string DetailedError { get; set; }

        /// <summary>
        /// Exception that caused the failure (if any)
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Current step in the bypass process
        /// </summary>
        public BypassStep CurrentStep { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// Status message for UI display
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Time when operation started
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Time when operation completed
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Duration of the operation
        /// </summary>
        public TimeSpan Duration => EndTime.HasValue 
            ? EndTime.Value - StartTime 
            : DateTime.Now - StartTime;

        /// <summary>
        /// List of files that were successfully injected
        /// </summary>
        public List<string> InjectedFiles { get; set; } = new List<string>();

        /// <summary>
        /// List of warnings encountered during operation
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Whether a backup was created
        /// </summary>
        public bool BackupCreated { get; set; }

        /// <summary>
        /// Path to the backup (if created)
        /// </summary>
        public string BackupPath { get; set; }

        /// <summary>
        /// Whether device needs reboot
        /// </summary>
        public bool RequiresReboot { get; set; }

        /// <summary>
        /// Whether device was rebooted
        /// </summary>
        public bool DeviceRebooted { get; set; }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static BypassResult SuccessResult(string message = "Bypass completed successfully")
        {
            return new BypassResult
            {
                Success = true,
                Progress = 100,
                StatusMessage = message,
                CurrentStep = BypassStep.Complete,
                EndTime = DateTime.Now
            };
        }

        /// <summary>
        /// Creates a failure result
        /// </summary>
        public static BypassResult FailureResult(string errorMessage, BypassStep failedStep, Exception ex = null)
        {
            return new BypassResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                DetailedError = ex?.ToString(),
                Exception = ex,
                CurrentStep = failedStep,
                EndTime = DateTime.Now
            };
        }

        /// <summary>
        /// Creates a progress result
        /// </summary>
        public static BypassResult InProgress(BypassStep step, int progress, string statusMessage)
        {
            return new BypassResult
            {
                Success = false,
                CurrentStep = step,
                Progress = progress,
                StatusMessage = statusMessage
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a warning to the result
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// Adds injected file to the list
        /// </summary>
        public void AddInjectedFile(string path)
        {
            InjectedFiles.Add(path);
        }

        /// <summary>
        /// Gets a summary of the result
        /// </summary>
        public string GetSummary()
        {
            if (Success)
            {
                return $"Bypass completed in {Duration.TotalSeconds:F1}s. " +
                       $"Files injected: {InjectedFiles.Count}. " +
                       (Warnings.Count > 0 ? $"Warnings: {Warnings.Count}" : "No warnings.");
            }
            else
            {
                return $"Bypass failed at step '{CurrentStep}': {ErrorMessage}";
            }
        }

        public override string ToString()
        {
            return Success 
                ? $"Success: {StatusMessage}" 
                : $"Failed at {CurrentStep}: {ErrorMessage}";
        }

        #endregion
    }

    /// <summary>
    /// Steps in the bypass process
    /// </summary>
    public enum BypassStep
    {
        /// <summary>Not started</summary>
        NotStarted = 0,
        
        /// <summary>Initializing components</summary>
        Initializing = 1,
        
        /// <summary>Detecting device</summary>
        DetectingDevice = 2,
        
        /// <summary>Connecting to device</summary>
        Connecting = 3,
        
        /// <summary>Reading device info</summary>
        ReadingDeviceInfo = 4,
        
        /// <summary>Creating backup</summary>
        CreatingBackup = 5,
        
        /// <summary>Generating activation record</summary>
        GeneratingActivation = 6,
        
        /// <summary>Generating setup bypass</summary>
        GeneratingSetupBypass = 7,
        
        /// <summary>Connecting via SSH</summary>
        ConnectingSSH = 8,
        
        /// <summary>Injecting files</summary>
        InjectingFiles = 9,
        
        /// <summary>Setting permissions</summary>
        SettingPermissions = 10,
        
        /// <summary>Restarting services</summary>
        RestartingServices = 11,
        
        /// <summary>Verifying bypass</summary>
        Verifying = 12,
        
        /// <summary>Rebooting device</summary>
        Rebooting = 13,
        
        /// <summary>Operation complete</summary>
        Complete = 14,
        
        /// <summary>Operation failed</summary>
        Failed = 99
    }
}
