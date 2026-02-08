namespace BypassTool.UI
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            
            // === MODERN COLOR SCHEME ===
            System.Drawing.Color primaryColor = System.Drawing.Color.FromArgb(0, 120, 215);      // Windows Blue
            System.Drawing.Color accentColor = System.Drawing.Color.FromArgb(16, 110, 190);      // Dark Blue
            System.Drawing.Color successColor = System.Drawing.Color.FromArgb(16, 124, 16);      // Green
            System.Drawing.Color dangerColor = System.Drawing.Color.FromArgb(196, 43, 28);       // Red
            System.Drawing.Color warningColor = System.Drawing.Color.FromArgb(255, 140, 0);      // Orange
            System.Drawing.Color backgroundColor = System.Drawing.Color.FromArgb(243, 243, 243); // Light Gray
            System.Drawing.Color cardColor = System.Drawing.Color.White;
            System.Drawing.Color textColor = System.Drawing.Color.FromArgb(32, 32, 32);
            System.Drawing.Color subtextColor = System.Drawing.Color.FromArgb(96, 96, 96);
            System.Drawing.Color borderColor = System.Drawing.Color.FromArgb(218, 220, 224);

            // === INITIALIZE ALL CONTROLS ===
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.panelMain = new System.Windows.Forms.Panel();
            this.panelDeviceInfo = new System.Windows.Forms.Panel();
            this.panelConnection = new System.Windows.Forms.Panel();
            this.panelActions = new System.Windows.Forms.Panel();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            
            // Device Info Panel Controls
            this.lblDeviceInfoTitle = new System.Windows.Forms.Label();
            this.lblDeviceStatusLabel = new System.Windows.Forms.Label();
            this.lblDeviceStatusValue = new System.Windows.Forms.Label();
            this.lblModelLabel = new System.Windows.Forms.Label();
            this.lblModelValue = new System.Windows.Forms.Label();
            this.lbliOSLabel = new System.Windows.Forms.Label();
            this.lbliOSValue = new System.Windows.Forms.Label();
            this.lblUDIDLabel = new System.Windows.Forms.Label();
            this.lblUDIDValue = new System.Windows.Forms.Label();
            this.lblJailbreakLabel = new System.Windows.Forms.Label();
            this.lblJailbreakValue = new System.Windows.Forms.Label();
            this.btnDetectDevice = new System.Windows.Forms.Button();
            
            // Connection Panel Controls
            this.lblConnectionTitle = new System.Windows.Forms.Label();
            this.lblIPLabel = new System.Windows.Forms.Label();
            this.txtDeviceIP = new System.Windows.Forms.TextBox();
            this.lblPortLabel = new System.Windows.Forms.Label();
            this.txtSSHPort = new System.Windows.Forms.TextBox();
            this.lblPasswordLabel = new System.Windows.Forms.Label();
            this.txtSSHPassword = new System.Windows.Forms.TextBox();
            this.chkShowPassword = new System.Windows.Forms.CheckBox();
            this.btnConnectSSH = new System.Windows.Forms.Button();
            this.btnDisconnectSSH = new System.Windows.Forms.Button();
            
            // Actions Panel Controls
            this.lblActionsTitle = new System.Windows.Forms.Label();
            this.btnBypass = new System.Windows.Forms.Button();
            this.btnRemoveActivation = new System.Windows.Forms.Button();
            this.btnBackup = new System.Windows.Forms.Button();
            this.btnRestore = new System.Windows.Forms.Button();
            
            // Log Panel
            this.panelLog = new System.Windows.Forms.Panel();
            this.lblLogTitle = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.RichTextBox();

            this.SuspendLayout();
            
            // === MAIN FORM ===
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(950, 720);
            this.BackColor = backgroundColor;
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BypassTool - iOS Activation Manager";
            
            // === HEADER PANEL ===
            this.panelHeader.BackColor = cardColor;
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 75;
            this.panelHeader.Padding = new System.Windows.Forms.Padding(25, 18, 25, 18);
            
            this.lblTitle.Text = "iOS Activation Manager";
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = textColor;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(25, 18);
            
            this.lblSubtitle.Text = "Manage iOS device activation state • Supports rootless jailbreaks";
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = subtextColor;
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Location = new System.Drawing.Point(25, 48);
            
            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.lblSubtitle);
            
            // === MAIN CONTAINER ===
            this.panelMain.Location = new System.Drawing.Point(20, 90);
            this.panelMain.Size = new System.Drawing.Size(910, 600);
            this.panelMain.BackColor = System.Drawing.Color.Transparent;
            
            // === DEVICE INFO CARD ===
            this.panelDeviceInfo.Location = new System.Drawing.Point(0, 0);
            this.panelDeviceInfo.Size = new System.Drawing.Size(440, 240);
            this.panelDeviceInfo.BackColor = cardColor;
            this.panelDeviceInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            
            this.lblDeviceInfoTitle.Text = "Device Information";
            this.lblDeviceInfoTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 11F);
            this.lblDeviceInfoTitle.ForeColor = textColor;
            this.lblDeviceInfoTitle.Location = new System.Drawing.Point(18, 12);
            this.lblDeviceInfoTitle.AutoSize = true;
            
            this.lblDeviceStatusLabel.Text = "Status:";
            this.lblDeviceStatusLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblDeviceStatusLabel.ForeColor = subtextColor;
            this.lblDeviceStatusLabel.Location = new System.Drawing.Point(18, 45);
            this.lblDeviceStatusLabel.AutoSize = true;
            
            this.lblDeviceStatusValue.Text = "No device detected";
            this.lblDeviceStatusValue.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblDeviceStatusValue.ForeColor = textColor;
            this.lblDeviceStatusValue.Location = new System.Drawing.Point(18, 63);
            this.lblDeviceStatusValue.Size = new System.Drawing.Size(400, 20);
            
            this.lblModelLabel.Text = "Model:";
            this.lblModelLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblModelLabel.ForeColor = subtextColor;
            this.lblModelLabel.Location = new System.Drawing.Point(18, 90);
            this.lblModelLabel.AutoSize = true;
            
            this.lblModelValue.Text = "-";
            this.lblModelValue.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblModelValue.ForeColor = textColor;
            this.lblModelValue.Location = new System.Drawing.Point(18, 108);
            this.lblModelValue.Size = new System.Drawing.Size(180, 20);
            
            this.lbliOSLabel.Text = "iOS Version:";
            this.lbliOSLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lbliOSLabel.ForeColor = subtextColor;
            this.lbliOSLabel.Location = new System.Drawing.Point(220, 90);
            this.lbliOSLabel.AutoSize = true;
            
            this.lbliOSValue.Text = "-";
            this.lbliOSValue.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lbliOSValue.ForeColor = textColor;
            this.lbliOSValue.Location = new System.Drawing.Point(220, 108);
            this.lbliOSValue.Size = new System.Drawing.Size(180, 20);
            
            this.lblJailbreakLabel.Text = "Jailbreak:";
            this.lblJailbreakLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblJailbreakLabel.ForeColor = subtextColor;
            this.lblJailbreakLabel.Location = new System.Drawing.Point(18, 133);
            this.lblJailbreakLabel.AutoSize = true;
            
            this.lblJailbreakValue.Text = "-";
            this.lblJailbreakValue.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblJailbreakValue.ForeColor = textColor;
            this.lblJailbreakValue.Location = new System.Drawing.Point(18, 151);
            this.lblJailbreakValue.Size = new System.Drawing.Size(400, 20);
            
            this.lblUDIDLabel.Text = "UDID:";
            this.lblUDIDLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblUDIDLabel.ForeColor = subtextColor;
            this.lblUDIDLabel.Location = new System.Drawing.Point(18, 176);
            this.lblUDIDLabel.AutoSize = true;
            
            this.lblUDIDValue.Text = "-";
            this.lblUDIDValue.Font = new System.Drawing.Font("Consolas", 8F);
            this.lblUDIDValue.ForeColor = textColor;
            this.lblUDIDValue.Location = new System.Drawing.Point(18, 194);
            this.lblUDIDValue.Size = new System.Drawing.Size(400, 16);
            
            // Detect Button
            this.btnDetectDevice.Text = "🔍 Detect Device";
            this.btnDetectDevice.Location = new System.Drawing.Point(280, 12);
            this.btnDetectDevice.Size = new System.Drawing.Size(140, 32);
            this.btnDetectDevice.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.btnDetectDevice.BackColor = primaryColor;
            this.btnDetectDevice.ForeColor = System.Drawing.Color.White;
            this.btnDetectDevice.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDetectDevice.FlatAppearance.BorderSize = 0;
            this.btnDetectDevice.Cursor = System.Windows.Forms.Cursors.Hand;
            
            this.panelDeviceInfo.Controls.Add(this.lblDeviceInfoTitle);
            this.panelDeviceInfo.Controls.Add(this.lblDeviceStatusLabel);
            this.panelDeviceInfo.Controls.Add(this.lblDeviceStatusValue);
            this.panelDeviceInfo.Controls.Add(this.lblModelLabel);
            this.panelDeviceInfo.Controls.Add(this.lblModelValue);
            this.panelDeviceInfo.Controls.Add(this.lbliOSLabel);
            this.panelDeviceInfo.Controls.Add(this.lbliOSValue);
            this.panelDeviceInfo.Controls.Add(this.lblJailbreakLabel);
            this.panelDeviceInfo.Controls.Add(this.lblJailbreakValue);
            this.panelDeviceInfo.Controls.Add(this.lblUDIDLabel);
            this.panelDeviceInfo.Controls.Add(this.lblUDIDValue);
            this.panelDeviceInfo.Controls.Add(this.btnDetectDevice);
            
            // === SSH CONNECTION CARD ===
            this.panelConnection.Location = new System.Drawing.Point(460, 0);
            this.panelConnection.Size = new System.Drawing.Size(450, 240);
            this.panelConnection.BackColor = cardColor;
            this.panelConnection.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            
            this.lblConnectionTitle.Text = "SSH Connection";
            this.lblConnectionTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 11F);
            this.lblConnectionTitle.ForeColor = textColor;
            this.lblConnectionTitle.Location = new System.Drawing.Point(18, 12);
            this.lblConnectionTitle.AutoSize = true;
            
            this.lblIPLabel.Text = "Device IP Address:";
            this.lblIPLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblIPLabel.ForeColor = subtextColor;
            this.lblIPLabel.Location = new System.Drawing.Point(18, 45);
            this.lblIPLabel.AutoSize = true;
            
            this.txtDeviceIP.Location = new System.Drawing.Point(18, 65);
            this.txtDeviceIP.Size = new System.Drawing.Size(280, 28);
            this.txtDeviceIP.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtDeviceIP.Text = "192.168.1.1";
            this.txtDeviceIP.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            
            this.lblPortLabel.Text = "Port:";
            this.lblPortLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPortLabel.ForeColor = subtextColor;
            this.lblPortLabel.Location = new System.Drawing.Point(310, 45);
            this.lblPortLabel.AutoSize = true;
            
            this.txtSSHPort.Location = new System.Drawing.Point(310, 65);
            this.txtSSHPort.Size = new System.Drawing.Size(120, 28);
            this.txtSSHPort.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtSSHPort.Text = "22";
            this.txtSSHPort.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            
            // Password Label
            this.lblPasswordLabel.Text = "SSH Password:";
            this.lblPasswordLabel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPasswordLabel.ForeColor = subtextColor;
            this.lblPasswordLabel.Location = new System.Drawing.Point(18, 100);
            this.lblPasswordLabel.AutoSize = true;
            
            // Password TextBox
            this.txtSSHPassword.Location = new System.Drawing.Point(18, 120);
            this.txtSSHPassword.Size = new System.Drawing.Size(335, 28);
            this.txtSSHPassword.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtSSHPassword.Text = "alpine";
            this.txtSSHPassword.PasswordChar = '●';
            this.txtSSHPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            
            // Show Password CheckBox
            this.chkShowPassword.Text = "Show";
            this.chkShowPassword.Location = new System.Drawing.Point(360, 123);
            this.chkShowPassword.Size = new System.Drawing.Size(70, 22);
            this.chkShowPassword.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.chkShowPassword.ForeColor = subtextColor;
            
            // Connect Button
            this.btnConnectSSH.Text = "🔗 Connect SSH";
            this.btnConnectSSH.Location = new System.Drawing.Point(18, 160);
            this.btnConnectSSH.Size = new System.Drawing.Size(200, 38);
            this.btnConnectSSH.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F);
            this.btnConnectSSH.BackColor = accentColor;
            this.btnConnectSSH.ForeColor = System.Drawing.Color.White;
            this.btnConnectSSH.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConnectSSH.FlatAppearance.BorderSize = 0;
            this.btnConnectSSH.Cursor = System.Windows.Forms.Cursors.Hand;
            
            // Disconnect Button
            this.btnDisconnectSSH.Text = "❌ Disconnect";
            this.btnDisconnectSSH.Location = new System.Drawing.Point(230, 160);
            this.btnDisconnectSSH.Size = new System.Drawing.Size(200, 38);
            this.btnDisconnectSSH.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F);
            this.btnDisconnectSSH.BackColor = System.Drawing.Color.FromArgb(108, 117, 125);
            this.btnDisconnectSSH.ForeColor = System.Drawing.Color.White;
            this.btnDisconnectSSH.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDisconnectSSH.FlatAppearance.BorderSize = 0;
            this.btnDisconnectSSH.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDisconnectSSH.Enabled = false;
            
            this.panelConnection.Controls.Add(this.lblConnectionTitle);
            this.panelConnection.Controls.Add(this.lblIPLabel);
            this.panelConnection.Controls.Add(this.txtDeviceIP);
            this.panelConnection.Controls.Add(this.lblPortLabel);
            this.panelConnection.Controls.Add(this.txtSSHPort);
            this.panelConnection.Controls.Add(this.lblPasswordLabel);
            this.panelConnection.Controls.Add(this.txtSSHPassword);
            this.panelConnection.Controls.Add(this.chkShowPassword);
            this.panelConnection.Controls.Add(this.btnConnectSSH);
            this.panelConnection.Controls.Add(this.btnDisconnectSSH);
            
            // === ACTIONS CARD ===
            this.panelActions.Location = new System.Drawing.Point(0, 255);
            this.panelActions.Size = new System.Drawing.Size(910, 140);
            this.panelActions.BackColor = cardColor;
            this.panelActions.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            
            this.lblActionsTitle.Text = "Actions";
            this.lblActionsTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 11F);
            this.lblActionsTitle.ForeColor = textColor;
            this.lblActionsTitle.Location = new System.Drawing.Point(18, 12);
            this.lblActionsTitle.AutoSize = true;
            
            // Bypass Button
            this.btnBypass.Text = "✨ Apply Bypass";
            this.btnBypass.Location = new System.Drawing.Point(18, 50);
            this.btnBypass.Size = new System.Drawing.Size(200, 70);
            this.btnBypass.Font = new System.Drawing.Font("Segoe UI Semibold", 10F);
            this.btnBypass.BackColor = successColor;
            this.btnBypass.ForeColor = System.Drawing.Color.White;
            this.btnBypass.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBypass.FlatAppearance.BorderSize = 0;
            this.btnBypass.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBypass.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // Remove Activation Button
            this.btnRemoveActivation.Text = "🗑️ Remove Activation";
            this.btnRemoveActivation.Location = new System.Drawing.Point(238, 50);
            this.btnRemoveActivation.Size = new System.Drawing.Size(200, 70);
            this.btnRemoveActivation.Font = new System.Drawing.Font("Segoe UI Semibold", 10F);
            this.btnRemoveActivation.BackColor = dangerColor;
            this.btnRemoveActivation.ForeColor = System.Drawing.Color.White;
            this.btnRemoveActivation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRemoveActivation.FlatAppearance.BorderSize = 0;
            this.btnRemoveActivation.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRemoveActivation.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // Backup Button
            this.btnBackup.Text = "💾 Backup State";
            this.btnBackup.Location = new System.Drawing.Point(458, 50);
            this.btnBackup.Size = new System.Drawing.Size(200, 70);
            this.btnBackup.Font = new System.Drawing.Font("Segoe UI Semibold", 10F);
            this.btnBackup.BackColor = primaryColor;
            this.btnBackup.ForeColor = System.Drawing.Color.White;
            this.btnBackup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBackup.FlatAppearance.BorderSize = 0;
            this.btnBackup.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBackup.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // Restore Button
            this.btnRestore.Text = "♻️ Restore State";
            this.btnRestore.Location = new System.Drawing.Point(678, 50);
            this.btnRestore.Size = new System.Drawing.Size(210, 70);
            this.btnRestore.Font = new System.Drawing.Font("Segoe UI Semibold", 10F);
            this.btnRestore.BackColor = warningColor;
            this.btnRestore.ForeColor = System.Drawing.Color.White;
            this.btnRestore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestore.FlatAppearance.BorderSize = 0;
            this.btnRestore.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRestore.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            this.panelActions.Controls.Add(this.lblActionsTitle);
            this.panelActions.Controls.Add(this.btnBypass);
            this.panelActions.Controls.Add(this.btnRemoveActivation);
            this.panelActions.Controls.Add(this.btnBackup);
            this.panelActions.Controls.Add(this.btnRestore);
            
            // === LOG PANEL ===
            this.panelLog.Location = new System.Drawing.Point(0, 410);
            this.panelLog.Size = new System.Drawing.Size(910, 180);
            this.panelLog.BackColor = cardColor;
            this.panelLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            
            this.lblLogTitle.Text = "Operation Log";
            this.lblLogTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 11F);
            this.lblLogTitle.ForeColor = textColor;
            this.lblLogTitle.Location = new System.Drawing.Point(18, 12);
            this.lblLogTitle.AutoSize = true;
            
            this.txtLog.Location = new System.Drawing.Point(18, 40);
            this.txtLog.Size = new System.Drawing.Size(870, 125);
            this.txtLog.Font = new System.Drawing.Font("Consolas", 8.5F);
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.txtLog.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            this.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            
            this.panelLog.Controls.Add(this.lblLogTitle);
            this.panelLog.Controls.Add(this.txtLog);
            
            // === PROGRESS BAR ===
            this.progressBar.Location = new System.Drawing.Point(20, 695);
            this.progressBar.Size = new System.Drawing.Size(720, 8);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.MarqueeAnimationSpeed = 30;
            
            // === STATUS LABEL ===
            this.lblStatus.Location = new System.Drawing.Point(750, 692);
            this.lblStatus.Size = new System.Drawing.Size(180, 18);
            this.lblStatus.Text = "Ready";
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStatus.ForeColor = subtextColor;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            
            // === ADD ALL PANELS TO MAIN CONTAINER ===
            this.panelMain.Controls.Add(this.panelDeviceInfo);
            this.panelMain.Controls.Add(this.panelConnection);
            this.panelMain.Controls.Add(this.panelActions);
            this.panelMain.Controls.Add(this.panelLog);
            
            // === ADD ALL TO FORM ===
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblStatus);
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        // === CONTROL DECLARATIONS ===
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Panel panelMain;
        
        // Device Info Panel
        private System.Windows.Forms.Panel panelDeviceInfo;
        private System.Windows.Forms.Label lblDeviceInfoTitle;
        private System.Windows.Forms.Label lblDeviceStatusLabel;
        private System.Windows.Forms.Label lblDeviceStatusValue;
        private System.Windows.Forms.Label lblModelLabel;
        private System.Windows.Forms.Label lblModelValue;
        private System.Windows.Forms.Label lbliOSLabel;
        private System.Windows.Forms.Label lbliOSValue;
        private System.Windows.Forms.Label lblJailbreakLabel;
        private System.Windows.Forms.Label lblJailbreakValue;
        private System.Windows.Forms.Label lblUDIDLabel;
        private System.Windows.Forms.Label lblUDIDValue;
        private System.Windows.Forms.Button btnDetectDevice;
        
        // Connection Panel
        private System.Windows.Forms.Panel panelConnection;
        private System.Windows.Forms.Label lblConnectionTitle;
        private System.Windows.Forms.Label lblIPLabel;
        private System.Windows.Forms.TextBox txtDeviceIP;
        private System.Windows.Forms.Label lblPortLabel;
        private System.Windows.Forms.TextBox txtSSHPort;
        private System.Windows.Forms.Label lblPasswordLabel;
        private System.Windows.Forms.TextBox txtSSHPassword;
        private System.Windows.Forms.CheckBox chkShowPassword;
        private System.Windows.Forms.Button btnConnectSSH;
        private System.Windows.Forms.Button btnDisconnectSSH;
        
        // Actions Panel
        private System.Windows.Forms.Panel panelActions;
        private System.Windows.Forms.Label lblActionsTitle;
        private System.Windows.Forms.Button btnBypass;
        private System.Windows.Forms.Button btnRemoveActivation;
        private System.Windows.Forms.Button btnBackup;
        private System.Windows.Forms.Button btnRestore;
        
        // Log Panel
        private System.Windows.Forms.Panel panelLog;
        private System.Windows.Forms.Label lblLogTitle;
        private System.Windows.Forms.RichTextBox txtLog;
        
        // Status
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
    }
}
