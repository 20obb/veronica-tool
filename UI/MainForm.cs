using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using BypassTool.Core;
using BypassTool.Models;
using BypassTool.Utils;

namespace BypassTool.UI
{
    /// <summary>
    /// Main application form for iOS Activation Bypass Tool
    /// Modern UI with rootless jailbreak support
    /// </summary>
    public partial class MainForm : Form
    {
        #region Fields

        private readonly Logger _logger;
        private DeviceManager _deviceManager;
        private ActivationEngine _activationEngine;
        private BackupManager _backupManager;
        private FileInjector _fileInjector;
        private DeviceInfo _currentDevice;
        private bool _isOperationRunning;
        private string _outputPath;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainForm
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            
            _logger = Logger.Instance;
            _logger.Info("MainForm initializing...");
            
            // Set output path
            _outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            Directory.CreateDirectory(_outputPath);
            
            // Initialize managers
            InitializeManagers();
            
            // Setup event handlers
            SetupEventHandlers();
            
            // Update UI state
            UpdateUIState();
            
            _logger.Info("MainForm initialized successfully.");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes all manager classes
        /// </summary>
        private void InitializeManagers()
        {
            try
            {
                _deviceManager = new DeviceManager();
                _activationEngine = new ActivationEngine();
                _backupManager = new BackupManager();
                _fileInjector = new FileInjector();
                
                // Subscribe to injection progress
                _fileInjector.ProgressChanged += OnInjectionProgress;
                
                _logger.Debug("Managers initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to initialize managers: {ex.Message}");
                LogMessage($"Failed to initialize managers: {ex.Message}", LogLevel.Error);
                MessageBox.Show(
                    $"Failed to initialize: {ex.Message}\n\nPlease ensure all dependencies are installed.",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Sets up event handlers for controls
        /// </summary>
        private void SetupEventHandlers()
        {
            // Form events
            this.FormClosing += MainForm_FormClosing;
            this.Load += MainForm_Load;
            
            // Device detection
            btnDetectDevice.Click += BtnDetectDevice_Click;
            
            // SSH connection events
            btnConnectSSH.Click += BtnConnectSSH_Click;
            btnDisconnectSSH.Click += BtnDisconnectSSH_Click;
            
            // Show password toggle
            chkShowPassword.CheckedChanged += (s, e) => {
                txtSSHPassword.PasswordChar = chkShowPassword.Checked ? '\0' : '●';
            };
            
            // Action button events
            btnBypass.Click += BtnBypass_Click;
            btnRemoveActivation.Click += BtnRemoveActivation_Click;
            btnBackup.Click += BtnBackup_Click;
            btnRestore.Click += BtnRestore_Click;
            
            // Logger event for real-time log display
            Logger.Instance.LogWritten += OnLogWritten;
        }

        /// <summary>
        /// Updates the UI state based on current conditions
        /// </summary>
        private void UpdateUIState()
        {
            bool hasDevice = _currentDevice != null;
            bool hasSSH = _fileInjector != null && _fileInjector.IsConnected;
            bool canOperate = hasDevice && hasSSH && !_isOperationRunning;
            
            // Update button states
            btnBypass.Enabled = canOperate;
            btnRemoveActivation.Enabled = canOperate;
            btnBackup.Enabled = canOperate;
            btnRestore.Enabled = canOperate;
            btnConnectSSH.Enabled = hasDevice && !hasSSH && !_isOperationRunning;
            btnDisconnectSSH.Enabled = hasSSH && !_isOperationRunning;
            btnDetectDevice.Enabled = !_isOperationRunning;
            
            // Update status
            UpdateStatusBar();
        }

        #endregion

        #region Form Events

        private void MainForm_Load(object sender, EventArgs e)
        {
            LogMessage("BypassTool started. Connect an iOS device to begin.", LogLevel.Info);
            LogMessage("Supports rootful and rootless jailbreaks (Dopamine, palera1n, etc.)", LogLevel.Info);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isOperationRunning)
            {
                var result = MessageBox.Show(
                    "An operation is currently running. Are you sure you want to exit?",
                    "Confirm Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            
            CleanupResources();
        }

        #endregion

        #region Device Detection

        private async void BtnDetectDevice_Click(object sender, EventArgs e)
        {
            await DetectDeviceAsync();
        }

        /// <summary>
        /// Detects connected iOS devices
        /// </summary>
        private async Task DetectDeviceAsync()
        {
            try
            {
                SetOperationRunning(true, "Detecting devices...");
                LogMessage("Scanning for connected iOS devices...", LogLevel.Info);
                
                var devices = await Task.Run(() => _deviceManager.DetectDevices());
                
                if (devices == null || devices.Count == 0)
                {
                    LogMessage("No iOS devices found. Please connect a device.", LogLevel.Warning);
                    ClearDeviceInfo();
                    _currentDevice = null;
                    
                    MessageBox.Show(
                        "No iOS devices detected.\n\n" +
                        "Make sure:\n" +
                        "1. Device is connected via USB\n" +
                        "2. Device is unlocked\n" +
                        "3. You tapped 'Trust' on device\n" +
                        "4. iTunes or Apple Devices app is installed",
                        "No Device Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                
                if (devices.Count == 1)
                {
                    _currentDevice = devices[0];
                }
                else
                {
                    // Multiple devices - show selector
                    _currentDevice = ShowDeviceSelector(devices);
                    if (_currentDevice == null)
                    {
                        LogMessage("Device selection cancelled.", LogLevel.Info);
                        return;
                    }
                }
                
                LogMessage($"Found device: {_currentDevice.DeviceName} ({_currentDevice.ProductType})", LogLevel.Info);
                LogMessage("Establishing persistent connection...", LogLevel.Info);
                
                // Connect to the selected device (with retry logic)
                bool connected = await Task.Run(() => _deviceManager.ConnectToDevice(_currentDevice.UDID));
                
                if (!connected)
                {
                    LogMessage("❌ Failed to establish persistent connection to device.", LogLevel.Error);
                    
                    MessageBox.Show(
                        "Device detected but connection failed.\n\n" +
                        "This can happen if:\n" +
                        "• Device was disconnected during detection\n" +
                        "• Device needs to be trusted (tap 'Trust')\n" +
                        "• Another app is using the device\n\n" +
                        "Try:\n" +
                        "• Disconnect and reconnect USB cable\n" +
                        "• Restart the device\n" +
                        "• Run this app as Administrator",
                        "Connection Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    
                    ClearDeviceInfo();
                    _currentDevice = null;
                    return;
                }
                
                LogMessage("✓ Device connected successfully!", LogLevel.Info);
                
                // Get updated device info from persistent connection
                try
                {
                    _currentDevice = _deviceManager.GetDeviceInfo();
                }
                catch (Exception ex)
                {
                    LogMessage($"Warning: Could not refresh device info: {ex.Message}", LogLevel.Warning);
                }
                
                // Check jailbreak status
                bool isJailbroken = false;
                try
                {
                    isJailbroken = _deviceManager.IsDeviceJailbroken();
                }
                catch { }
                
                // Display device info
                DisplayDeviceInfo(isJailbroken);
                LogMessage($"Device connected: {_currentDevice.DeviceName} ({_currentDevice.ProductType})", LogLevel.Info);
                LogMessage($"  iOS Version: {_currentDevice.ProductVersion}", LogLevel.Info);
                LogMessage($"  Activation State: {_currentDevice.ActivationState}", LogLevel.Info);
                
                if (!isJailbroken)
                {
                    LogMessage("⚠️ Jailbreak not detected via USB. Connect via SSH for full detection.", LogLevel.Warning);
                }
                else
                {
                    LogMessage("✓ Jailbreak detected!", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error detecting device: {ex.Message}", LogLevel.Error);
                _currentDevice = null;
                ClearDeviceInfo();
            }
            finally
            {
                SetOperationRunning(false);
                UpdateUIState();
            }
        }

        /// <summary>
        /// Shows device selector dialog for multiple devices
        /// </summary>
        private DeviceInfo ShowDeviceSelector(List<DeviceInfo> devices)
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Select Device";
                dialog.Size = new Size(400, 200);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                
                var label = new Label
                {
                    Text = "Multiple devices detected. Select one:",
                    Location = new Point(10, 15),
                    AutoSize = true
                };
                
                var combo = new ComboBox
                {
                    Location = new Point(10, 40),
                    Size = new Size(360, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                
                foreach (var device in devices)
                {
                    combo.Items.Add($"{device.DeviceName} ({device.UDID.Substring(0, 8)}...)");
                }
                combo.SelectedIndex = 0;
                
                var okButton = new Button
                {
                    Text = "Select",
                    DialogResult = DialogResult.OK,
                    Location = new Point(210, 120),
                    Size = new Size(80, 30)
                };
                
                var cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(300, 120),
                    Size = new Size(80, 30)
                };
                
                dialog.Controls.Add(label);
                dialog.Controls.Add(combo);
                dialog.Controls.Add(okButton);
                dialog.Controls.Add(cancelButton);
                dialog.AcceptButton = okButton;
                dialog.CancelButton = cancelButton;
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return devices[combo.SelectedIndex];
                }
            }
            
            return null;
        }

        /// <summary>
        /// Displays device information in the UI
        /// </summary>
        private void DisplayDeviceInfo(bool isJailbroken)
        {
            if (_currentDevice == null) return;
            
            lblDeviceStatusValue.Text = "Connected";
            lblDeviceStatusValue.ForeColor = Color.FromArgb(16, 124, 16); // Green
            
            lblModelValue.Text = _currentDevice.ProductType ?? _currentDevice.DeviceName ?? "-";
            lbliOSValue.Text = _currentDevice.ProductVersion ?? "-";
            lblUDIDValue.Text = _currentDevice.UDID ?? "-";
            
            if (isJailbroken)
            {
                lblJailbreakValue.Text = "✓ Jailbroken (detected)";
                lblJailbreakValue.ForeColor = Color.FromArgb(16, 124, 16);
            }
            else
            {
                lblJailbreakValue.Text = "⚠️ Not detected (connect SSH for full check)";
                lblJailbreakValue.ForeColor = Color.FromArgb(255, 140, 0);
            }
        }

        /// <summary>
        /// Clears device information from UI
        /// </summary>
        private void ClearDeviceInfo()
        {
            lblDeviceStatusValue.Text = "No device detected";
            lblDeviceStatusValue.ForeColor = Color.FromArgb(96, 96, 96);
            lblModelValue.Text = "-";
            lbliOSValue.Text = "-";
            lblUDIDValue.Text = "-";
            lblJailbreakValue.Text = "-";
            lblJailbreakValue.ForeColor = Color.FromArgb(32, 32, 32);
        }

        #endregion

        #region SSH Connection

        private async void BtnConnectSSH_Click(object sender, EventArgs e)
        {
            await ConnectSSHAsync();
        }

        private void BtnDisconnectSSH_Click(object sender, EventArgs e)
        {
            DisconnectSSH();
        }

        /// <summary>
        /// Connects to device via SSH
        /// </summary>
        private async Task ConnectSSHAsync()
        {
            try
            {
                string ipAddress = txtDeviceIP.Text.Trim();
                string sshPassword = txtSSHPassword.Text;
                
                if (!int.TryParse(txtSSHPort.Text.Trim(), out int port))
                {
                    port = 22;
                }
                
                if (string.IsNullOrEmpty(ipAddress))
                {
                    MessageBox.Show("Please enter the device IP address.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtDeviceIP.Focus();
                    return;
                }
                
                if (string.IsNullOrEmpty(sshPassword))
                {
                    MessageBox.Show("Please enter SSH password.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtSSHPassword.Focus();
                    return;
                }
                
                SetOperationRunning(true, "Connecting via SSH...");
                LogMessage($"Connecting to {ipAddress}:{port} via SSH...", LogLevel.Info);
                
                bool connected = await Task.Run(() => 
                    _fileInjector.ConnectSSH(ipAddress, "root", sshPassword, port));
                
                if (connected)
                {
                    LogMessage("✓ SSH connection established!", LogLevel.Info);
                    
                    // Check jailbreak via SSH (more reliable)
                    LogMessage("Checking jailbreak status via SSH...", LogLevel.Info);
                    bool isJailbroken = _deviceManager.IsDeviceJailbrokenViaSSH(_fileInjector.GetSshClient());
                    
                    if (isJailbroken)
                    {
                        LogMessage("✓ Jailbreak detected via SSH!", LogLevel.Info);
                        lblJailbreakValue.Text = "✓ Jailbroken (confirmed via SSH)";
                        lblJailbreakValue.ForeColor = Color.FromArgb(16, 124, 16);
                    }
                    else
                    {
                        LogMessage("⚠️ Jailbreak not detected via SSH. Bypass may not work.", LogLevel.Warning);
                        lblJailbreakValue.Text = "⚠️ Not detected via SSH";
                        lblJailbreakValue.ForeColor = Color.FromArgb(196, 43, 28);
                    }
                    
                    // Update button states
                    btnConnectSSH.Enabled = false;
                    btnDisconnectSSH.Enabled = true;
                }
                else
                {
                    LogMessage("❌ Failed to connect via SSH. Check IP and credentials.", LogLevel.Error);
                    MessageBox.Show(
                        "Failed to connect via SSH.\n\n" +
                        "Please check:\n" +
                        "• Device IP address is correct\n" +
                        "• SSH is enabled on device\n" +
                        "• Password is correct (default: alpine)\n" +
                        "• Device is jailbroken\n\n" +
                        "Check the Operation Log for details.",
                        "Connection Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"SSH connection error: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                SetOperationRunning(false);
                UpdateUIState();
            }
        }

        /// <summary>
        /// Disconnects SSH connection
        /// </summary>
        private void DisconnectSSH()
        {
            try
            {
                _fileInjector?.DisconnectSSH();
                LogMessage("SSH disconnected.", LogLevel.Info);
                
                btnConnectSSH.Enabled = true;
                btnDisconnectSSH.Enabled = false;
            }
            catch (Exception ex)
            {
                LogMessage($"Error disconnecting: {ex.Message}", LogLevel.Warning);
            }
            
            UpdateUIState();
        }

        #endregion

        #region Main Operations

        private async void BtnBypass_Click(object sender, EventArgs e)
        {
            await StartBypassAsync();
        }

        /// <summary>
        /// Starts the activation bypass process
        /// </summary>
        private async Task StartBypassAsync()
        {
            if (_currentDevice == null || !_fileInjector.IsConnected)
            {
                MessageBox.Show("Please detect a device and connect via SSH first.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Confirmation
            var confirm = MessageBox.Show(
                "This will apply activation bypass to your device.\n\n" +
                "A backup will be created automatically.\n\n" +
                "Continue?",
                "Confirm Bypass",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            
            if (confirm != DialogResult.Yes)
                return;
            
            try
            {
                SetOperationRunning(true, "Applying bypass...");
                progressBar.Style = ProgressBarStyle.Marquee;
                
                // Step 1: Create backup
                LogMessage("Step 1/5: Creating backup...", LogLevel.Info);
                string backupPath = Path.Combine(_outputPath, "backups", 
                    $"backup_{_currentDevice.UDID}_{DateTime.Now:yyyyMMdd_HHmmss}");
                
                await Task.Run(() => _fileInjector.BackupActivationFiles(backupPath));
                LogMessage($"Backup saved to: {backupPath}", LogLevel.Info);
                
                // Step 2: Generate activation record
                LogMessage("Step 2/5: Generating activation record...", LogLevel.Info);
                var activationRecord = await Task.Run(() => 
                    _activationEngine.GenerateActivationRecord(_currentDevice));
                
                if (activationRecord == null)
                {
                    throw new Exception("Failed to generate activation record");
                }
                LogMessage("Activation record generated successfully.", LogLevel.Info);
                
                // Step 3: Generate all bypass files
                LogMessage("Step 3/5: Generating bypass files...", LogLevel.Info);
                var allFiles = await Task.Run(() =>
                    _activationEngine.GenerateAllActivationFiles(_currentDevice));
                
                // Create activation files dict with activation record
                var activationFiles = new Dictionary<string, byte[]>
                {
                    { "activation_record.plist", activationRecord }
                };
                
                // Step 4: Inject files
                LogMessage("Step 4/5: Injecting files to device...", LogLevel.Info);
                var result = await Task.Run(() => 
                    _fileInjector.InjectBypassFiles(_currentDevice, activationFiles, allFiles));
                
                if (!result.Success)
                {
                    throw new Exception("Failed to inject bypass files");
                }
                
                // Step 5: Restart services
                LogMessage("Step 5/5: Restarting device services...", LogLevel.Info);
                await Task.Run(() =>
                {
                    _fileInjector.RestartActivationDaemon();
                    System.Threading.Thread.Sleep(1000);
                    _fileInjector.RespringDevice();
                });
                
                LogMessage("✓ Bypass applied successfully!", LogLevel.Info);
                LogMessage("Device will respring. Please wait.", LogLevel.Info);
                
                MessageBox.Show(
                    "✅ Bypass applied successfully!\n\n" +
                    "Your device will respring.\n" +
                    "After respring, the activation lock should be bypassed.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Bypass failed: {ex.Message}", LogLevel.Error);
                MessageBox.Show(
                    $"Bypass failed: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
                SetOperationRunning(false);
            }
        }

        private async void BtnRemoveActivation_Click(object sender, EventArgs e)
        {
            await RemoveActivationAsync();
        }

        /// <summary>
        /// Removes activation files from device (for testing)
        /// </summary>
        private async Task RemoveActivationAsync()
        {
            // Confirmation dialog
            var result = MessageBox.Show(
                "⚠️ WARNING!\n\n" +
                "This will REMOVE activation files from your device.\n" +
                "Your device will show as 'Unactivated' and require setup.\n\n" +
                "This is for TESTING purposes only on devices you own.\n\n" +
                "A backup will be created automatically.\n\n" +
                "Continue?",
                "Confirm Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            
            if (result != DialogResult.Yes)
                return;
            
            if (_currentDevice == null)
            {
                MessageBox.Show("Please detect a device first.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            if (!_fileInjector.IsConnected)
            {
                MessageBox.Show("Please connect via SSH first.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            try
            {
                SetOperationRunning(true, "Removing activation files...");
                progressBar.Style = ProgressBarStyle.Marquee;
                
                await Task.Run(() =>
                {
                    // Create backup first
                    string backupPath = Path.Combine(_outputPath, "backups",
                        $"pre_removal_backup_{_currentDevice.UDID}_{DateTime.Now:yyyyMMdd_HHmmss}");
                    
                    LogMessage("Creating backup before removal...", LogLevel.Info);
                    _fileInjector.BackupActivationFiles(backupPath);
                    LogMessage($"Backup saved: {backupPath}", LogLevel.Info);
                    
                    // Remove activation files
                    LogMessage("Removing activation files...", LogLevel.Info);
                    bool success = _fileInjector.RemoveActivationFiles();
                    
                    if (success)
                    {
                        LogMessage("✓ Activation files removed successfully!", LogLevel.Info);
                        LogMessage("Device will show 'Unactivated' state.", LogLevel.Info);
                        LogMessage("Please reboot device to see changes.", LogLevel.Warning);
                    }
                    else
                    {
                        LogMessage("❌ Failed to remove activation files.", LogLevel.Error);
                    }
                });
                
                MessageBox.Show(
                    "✅ Activation files removed!\n\n" +
                    "A backup was saved automatically.\n\n" +
                    "Please reboot your device.\n" +
                    "Device will show 'Unactivated' state.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Error: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Error: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
                SetOperationRunning(false);
            }
        }

        private async void BtnBackup_Click(object sender, EventArgs e)
        {
            await CreateBackupAsync();
        }

        /// <summary>
        /// Creates a backup of current device state
        /// </summary>
        private async Task CreateBackupAsync()
        {
            if (!_fileInjector.IsConnected)
            {
                MessageBox.Show("Please connect via SSH first.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            try
            {
                SetOperationRunning(true, "Creating backup...");
                progressBar.Style = ProgressBarStyle.Marquee;
                
                string backupPath = Path.Combine(_outputPath, "backups",
                    $"manual_backup_{_currentDevice?.UDID ?? "unknown"}_{DateTime.Now:yyyyMMdd_HHmmss}");
                
                LogMessage($"Creating backup...", LogLevel.Info);
                
                bool success = await Task.Run(() => _fileInjector.BackupActivationFiles(backupPath));
                
                if (success)
                {
                    LogMessage($"✓ Backup created: {backupPath}", LogLevel.Info);
                    MessageBox.Show(
                        $"Backup created successfully!\n\nLocation:\n{backupPath}",
                        "Backup Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage("❌ Backup failed or no files found.", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Backup error: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
                SetOperationRunning(false);
            }
        }

        private async void BtnRestore_Click(object sender, EventArgs e)
        {
            await RestoreBackupAsync();
        }

        /// <summary>
        /// Restores a previous backup to the device
        /// </summary>
        private async Task RestoreBackupAsync()
        {
            if (!_fileInjector.IsConnected)
            {
                MessageBox.Show("Please connect via SSH first.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Show folder browser to select backup
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select backup folder to restore";
                dialog.SelectedPath = Path.Combine(_outputPath, "backups");
                
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;
                
                string backupPath = dialog.SelectedPath;
                
                if (!Directory.Exists(backupPath))
                {
                    MessageBox.Show("Invalid backup folder.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                var confirm = MessageBox.Show(
                    $"Restore backup from:\n{backupPath}\n\nContinue?",
                    "Confirm Restore",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (confirm != DialogResult.Yes)
                    return;
                
                try
                {
                    SetOperationRunning(true, "Restoring backup...");
                    progressBar.Style = ProgressBarStyle.Marquee;
                    
                    LogMessage($"Restoring from: {backupPath}", LogLevel.Info);
                    
                    bool success = await Task.Run(() => _fileInjector.RestoreActivationFiles(backupPath));
                    
                    if (success)
                    {
                        LogMessage("✓ Backup restored successfully!", LogLevel.Info);
                        MessageBox.Show(
                            "Backup restored successfully!\n\nDevice may need to respring.",
                            "Restore Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        LogMessage("❌ Restore failed.", LogLevel.Error);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Restore error: {ex.Message}", LogLevel.Error);
                }
                finally
                {
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressBar.Value = 0;
                    SetOperationRunning(false);
                }
            }
        }

        #endregion

        #region Progress and Events

        private void OnInjectionProgress(object sender, InjectionProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnInjectionProgress(sender, e)));
                return;
            }
            
            if (progressBar.Style != ProgressBarStyle.Marquee)
            {
                progressBar.Value = Math.Min(e.Progress, 100);
            }
            LogMessage(e.Status, LogLevel.Info);
        }

        #endregion

        #region Logging

        /// <summary>
        /// Handler for log events from Logger
        /// </summary>
        private void OnLogWritten(object sender, LogEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnLogWritten(sender, e)));
                return;
            }
            
            AppendLogMessage(e.Timestamp, e.Level, e.Message);
        }

        /// <summary>
        /// Logs a message to the UI
        /// </summary>
        private void LogMessage(string message, LogLevel level)
        {
            // Log to file via Logger
            switch (level)
            {
                case LogLevel.Debug:
                    _logger.Debug(message);
                    break;
                case LogLevel.Info:
                    _logger.Info(message);
                    break;
                case LogLevel.Warning:
                    _logger.Warning(message);
                    break;
                case LogLevel.Error:
                    _logger.Error(message);
                    break;
            }
        }

        /// <summary>
        /// Appends a formatted log message to the log textbox
        /// </summary>
        private void AppendLogMessage(string timestamp, LogLevel level, string message)
        {
            if (txtLog == null) return;
            
            // Color based on level
            Color color;
            switch (level)
            {
                case LogLevel.Error:
                    color = Color.FromArgb(255, 100, 100);
                    break;
                case LogLevel.Warning:
                    color = Color.FromArgb(255, 200, 100);
                    break;
                case LogLevel.Info:
                    color = Color.FromArgb(100, 200, 100);
                    break;
                default:
                    color = Color.FromArgb(180, 180, 180);
                    break;
            }
            
            string formattedMessage = $"[{timestamp}] [{level}] {message}\n";
            
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText(formattedMessage);
            txtLog.SelectionColor = txtLog.ForeColor;
            txtLog.ScrollToCaret();
        }

        #endregion

        #region UI Helpers

        /// <summary>
        /// Sets the operation running state
        /// </summary>
        private void SetOperationRunning(bool running, string status = null)
        {
            _isOperationRunning = running;
            
            if (running)
            {
                lblStatus.Text = status ?? "Working...";
            }
            else
            {
                lblStatus.Text = "Ready";
            }
            
            UpdateUIState();
        }

        /// <summary>
        /// Updates the status bar
        /// </summary>
        private void UpdateStatusBar()
        {
            if (_isOperationRunning) return;
            
            if (_currentDevice == null)
            {
                lblStatus.Text = "No device connected";
            }
            else if (_fileInjector != null && _fileInjector.IsConnected)
            {
                lblStatus.Text = "Ready - SSH Connected";
            }
            else
            {
                lblStatus.Text = "Ready - Connect SSH";
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleans up resources before form closes
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                _logger.Info("Cleaning up resources...");
                
                // Disconnect SSH
                _fileInjector?.DisconnectSSH();
                _fileInjector?.Dispose();
                
                // Disconnect device manager
                _deviceManager?.Disconnect();
                _deviceManager?.Dispose();
                
                // Unsubscribe from logger
                Logger.Instance.LogWritten -= OnLogWritten;
                
                _logger.Info("Resources cleaned up successfully.");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error during cleanup: {ex.Message}");
            }
        }

        #endregion
    }
}
