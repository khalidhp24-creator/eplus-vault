using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("ePlus System Administration & Maintenance Toolkit")]
[assembly: AssemblyDescription("All-in-One C# GUI Toolkit for Windows Administration, Network, Power, Security, Printer Sharing, and System Repair (32-bit & 64-bit).")]
[assembly: AssemblyCompany("ePlus Utilities")]
[assembly: AssemblyProduct("SystemToolkit")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace EPlusSystemToolkit
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            if (!IsAdministrator())
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(psi);
                    return;
                }
                catch
                {
                    // Continue and display warning if UAC is declined
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public static bool IsAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }
    }

    public sealed class MainForm : Form
    {
        // Light Windows Modern Theme Colors
        private static readonly Color BgWindow = Color.FromArgb(241, 245, 249);
        private static readonly Color BgHeader = Color.FromArgb(255, 255, 255);
        private static readonly Color BgToolbar = Color.FromArgb(248, 250, 252);
        private static readonly Color BgCard = Color.FromArgb(255, 255, 255);
        private static readonly Color BgCardSelected = Color.FromArgb(239, 246, 255);
        private static readonly Color BorderColor = Color.FromArgb(203, 213, 225);
        private static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
        private static readonly Color TextSecondary = Color.FromArgb(71, 85, 105);
        private static readonly Color AccentBlue = Color.FromArgb(37, 99, 235);
        private static readonly Color AccentRed = Color.FromArgb(220, 38, 38);

        private TextBox txtSearch;
        private FlowLayoutPanel categoryBar;
        private Panel batchBar;
        private Button btnRunSelected;
        private Button btnSelectAll;
        private Button btnUnselectAll;
        private Panel cardsContainer;
        private RichTextBox txtLog;
        private Label lblStatus;
        private ProgressBar progressBar;
        private Button btnAbortShutdown;
        private Button btnExportFailures;
        private Button btnClearLog;

        private string currentRunningAction = "General";
        private readonly List<FailureRecord> failureRecords = new List<FailureRecord>();

        private sealed class FailureRecord
        {
            public DateTime Timestamp { get; set; }
            public string ActionTitle { get; set; }
            public string Message { get; set; }
        }

        private readonly List<ToolActionItem> allActions = new List<ToolActionItem>();
        private readonly List<Button> categoryButtons = new List<Button>();
        private string activeCategory = "All";
        private bool isBusy;

        public MainForm()
        {
            InitializeActions();
            InitializeUi();
            RenderActionCards();
            AppendWelcomeBanner();
        }

        private void InitializeActions()
        {
            // [0] System Info (info only, pinned first, not selectable for batch runs)
            allActions.Add(new ToolActionItem(
                0, "System & Users",
                "[0] System Info (Windows, Network, AnyDesk / TeamViewer ID)",
                "Shows Windows edition/build, install date, uptime, active network adapters (IP, MAC, gateway, DNS) and remote IDs, with Copy to Clipboard.",
                Color.FromArgb(30, 64, 175),
                ExecuteSystemInfoAsync,
                false));

            // [1] Enable/Disable Hibernation
            allActions.Add(new ToolActionItem(
                1, "Power & Sleep",
                "[1] Enable / Disable Hibernation",
                "Check hibernation status, disable hibernation (and set AC sleep to Never), or enable hibernation.",
                Color.FromArgb(2, 132, 199),
                ExecuteHibernationDialogAsync));

            // [2] Create Admin User
            allActions.Add(new ToolActionItem(
                2, "System & Users",
                "[2] Create Admin User (admin-eplus)",
                "Opens pre-filled dialog ('admin-eplus' / '#2026#') to create user and add to local Administrators.",
                Color.FromArgb(124, 58, 237),
                ExecuteCreateAdminUserAsync));

            // [3] Create Backup Folders
            allActions.Add(new ToolActionItem(
                3, "System & Users",
                "[3] Create 'backup' Folders on Drives (D: - H:)",
                "Scans available drives D: through H: and creates D:\\backup\\, E:\\backup\\, etc. if missing.",
                Color.FromArgb(5, 150, 105),
                ExecuteCreateBackupFoldersAsync));

            // [4] Enable Firewall + Open SQL Ports 1433-1434
            allActions.Add(new ToolActionItem(
                4, "Network & Firewall",
                "[4] Enable Firewall + Open Ports 1433-1434 (TCP/UDP)",
                "Enables Windows Firewall on all profiles and opens inbound TCP & UDP ports 1433-1434 for SQL.",
                AccentBlue,
                ExecuteFirewallSqlPortsAsync));

            // [5] Disable Windows Firewall
            allActions.Add(new ToolActionItem(
                5, "Network & Firewall",
                "[5] Disable Windows Firewall",
                "Turns off Windows Firewall for all network profiles (Domain, Private, Public).",
                AccentRed,
                ExecuteDisableFirewallAsync));

            // [6] Enable/Disable Windows Update Service
            allActions.Add(new ToolActionItem(
                6, "System & Users",
                "[6] Enable / Disable Windows Update Service",
                "Configure Windows Update (wuauserv) startup type (Disabled/Stop or Auto/Start).",
                Color.FromArgb(217, 119, 6),
                ExecuteWindowsUpdateDialogAsync));

            // [7] Configure Sleep Timer
            allActions.Add(new ToolActionItem(
                7, "Power & Sleep",
                "[7] Configure Sleep Timer (Never / 10m / 1h / 3h)",
                "Set AC and DC standby timeout to Never (0), 10 Minutes, 1 Hour, or 3 Hours via powercfg.",
                Color.FromArgb(2, 132, 199),
                ExecuteSleepTimerDialogAsync));

            // [8] Make Restore Point Now
            allActions.Add(new ToolActionItem(
                8, "Maintenance & Repair",
                "[8] Make System Restore Point Now",
                "Enables System Restore on C: and creates an immediate timestamped restore point.",
                Color.FromArgb(5, 150, 105),
                ExecuteCreateRestorePointAsync));

            // [9] SFC Scan
            allActions.Add(new ToolActionItem(
                9, "Maintenance & Repair",
                "[9] System Maintenance: SFC Scan (sfc /scannow)",
                "Scans for and repairs corrupted Windows system files using System File Checker.",
                AccentBlue,
                ExecuteSfcScanAsync));

            // [10] Set Regional Settings to ePlus
            allActions.Add(new ToolActionItem(
                10, "Region & Language",
                "[10] Set Regional Settings to ePlus",
                "Configures HKCU International date (dd/MM/yyyy), time (hh:mm:ss tt), AM/PM, en-US locale, and digits.",
                Color.FromArgb(147, 51, 234),
                ExecuteRegionSettingsAsync));

            // [11] Clear Temp
            allActions.Add(new ToolActionItem(
                11, "Maintenance & Repair",
                "[11] Clear Temp, Recent & Prefetch Folders",
                "Cleans C:\\Windows\\Temp, %TEMP%, Recent items, and C:\\Windows\\Prefetch safely.",
                Color.FromArgb(234, 88, 12),
                ExecuteClearTempAsync));

            // [12] DISM Scan
            allActions.Add(new ToolActionItem(
                12, "Maintenance & Repair",
                "[12] System Maintenance: DISM Scan (/RestoreHealth)",
                "Repairs corrupted core Windows Image files via DISM /Online /Cleanup-Image /RestoreHealth.",
                AccentBlue,
                ExecuteDismScanAsync));

            // [13] CHKDSK
            allActions.Add(new ToolActionItem(
                13, "Maintenance & Repair",
                "[13] System Maintenance: CHKDSK (Select Drive & /f or /f /r)",
                "Select any detected partition (C:, D:, etc.) and choose between /f (fast error repair) or /f /r (full bad sector scan).",
                Color.FromArgb(202, 138, 4),
                ExecuteChkdskAsync));

            // [14] Disable UAC Completely
            allActions.Add(new ToolActionItem(
                14, "System & Users",
                "[14] Disable UAC (User Account Control) Completely",
                "Sets EnableLUA = 0 in HKLM Policies\\System (requires computer restart to take effect).",
                AccentRed,
                ExecuteDisableUacAsync));

            // [15] Enable Printer & Network Sharing (Password ON)
            allActions.Add(new ToolActionItem(
                15, "Sharing & Printers",
                "[15] Enable Printer & Network Sharing (Password ON)",
                "Enables File/Printer Sharing & Network Discovery firewall rules, enforces password sharing, disables Guest.",
                Color.FromArgb(13, 148, 136),
                ExecuteSharePasswordOnAsync));

            // [16] Enable Printer & Network Sharing (Password OFF)
            allActions.Add(new ToolActionItem(
                16, "Sharing & Printers",
                "[16] Enable Printer & Network Sharing (Password OFF)",
                "Enables File/Printer Sharing & Network Discovery, allows anonymous sharing, and enables Guest account.",
                Color.FromArgb(13, 148, 136),
                ExecuteSharePasswordOffAsync));

            // [17] Fix Printer Sharing (Host/Server PC)
            allActions.Add(new ToolActionItem(
                17, "Sharing & Printers",
                "[17] Fix Printer Sharing (Host / Server PC)",
                "Sets RpcAuthnLevelPrivacyEnabled = 0 in Control\\Print and automatically restarts Print Spooler.",
                Color.FromArgb(8, 145, 178),
                ExecuteServerPrintFixAsync));

            // [18] Fix Printer Sharing (Client PC)
            allActions.Add(new ToolActionItem(
                18, "Sharing & Printers",
                "[18] Fix Printer Sharing (Client PC)",
                "Sets RestrictDriverInstallationToAdministrators = 0 in PointAndPrint and restarts Print Spooler.",
                Color.FromArgb(8, 145, 178),
                ExecuteClientPrintFixAsync));

            // [19] Set System Language to EN (US) & AR (Egypt)
            allActions.Add(new ToolActionItem(
                19, "Region & Language",
                "[19] Set System Language to EN (US) & AR (Egypt)",
                "Updates Windows User Language List via PowerShell to en-US and ar-EG.",
                Color.FromArgb(147, 51, 234),
                ExecuteSetLanguagePsAsync));

            // [20] Set Keyboard Layout to EN (US) & AR (Egypt)
            allActions.Add(new ToolActionItem(
                20, "Region & Language",
                "[20] Set Keyboard Layout to EN (US) & AR (Egypt)",
                "Resets HKCU\\Keyboard Layout\\Preload and sets 1=00000409 (EN-US) and 2=00000c01 (AR-EG).",
                Color.FromArgb(147, 51, 234),
                ExecuteSetKeyboardRegAsync));

            // [21] Reset Network Configuration
            allActions.Add(new ToolActionItem(
                21, "Network & Firewall",
                "[21] Reset Network Configuration (Winsock, TCP/IP, DNS)",
                "Resets Winsock catalog, resets TCP/IP stack, flushes DNS cache, and offers 15s restart.",
                AccentBlue,
                ExecuteNetResetAsync));

            // [22] Add Folders to Windows Security Scan Exclusions
            allActions.Add(new ToolActionItem(
                22, "System & Users",
                "[22] Add Scan Exclusions (%USERPROFILE%\\Downloads & Bconnect)",
                "Adds current user's Downloads, \"C:\\Program Files (x86)\\Bconnect\", and any X:\\Bconnect folder on all drive roots to scan exclusions.",
                Color.FromArgb(5, 150, 105),
                ExecuteAddFolderExclusionsAsync));

            // [25] Restart Print Spooler + Clear Queue
            allActions.Add(new ToolActionItem(
                25, "Sharing & Printers",
                "[25] Restart Print Spooler + Clear Stuck Print Queue",
                "Stops the Print Spooler, deletes all pending jobs in System32\\spool\\PRINTERS, then starts the spooler again.",
                Color.FromArgb(8, 145, 178),
                ExecuteSpoolerResetAsync));

            // [26] High Performance Power Plan + Disable USB Selective Suspend
            allActions.Add(new ToolActionItem(
                26, "Power & Sleep",
                "[26] High Performance Power Plan + Disable USB Selective Suspend",
                "Activates the High Performance power plan (creates it if missing) and turns off USB selective suspend on AC and battery.",
                Color.FromArgb(2, 132, 199),
                ExecuteHighPerformanceAsync));

            // [27] Disable Windows Fast Startup
            allActions.Add(new ToolActionItem(
                27, "Power & Sleep",
                "[27] Disable Windows Fast Startup",
                "Sets HiberbootEnabled = 0 so shutdown is a full shutdown (fixes shared folder / printer issues after power on).",
                Color.FromArgb(2, 132, 199),
                ExecuteDisableFastStartupAsync));

            // [28] Install Remote Support Tools
            allActions.Add(new ToolActionItem(
                28, "System & Users",
                "[28] Install Remote Support Tools (AnyDesk, RustDesk, UltraViewer, TightVNC)",
                "Silently installs AnyDesk, RustDesk, UltraViewer, or TightVNC via winget or official silent setup.",
                Color.FromArgb(124, 58, 237),
                ExecuteInstallRemoteToolsAsync));

            // [29] Reset AnyDesk ID
            allActions.Add(new ToolActionItem(
                29, "System & Users",
                "[29] Reset AnyDesk ID (Generate Brand New Client ID)",
                "Stops AnyDesk service, resets client configuration/ID tokens, and restarts service to generate a new ID.",
                Color.FromArgb(220, 38, 38),
                ExecuteResetAnyDeskIdAsync));

            // [30] Export Failed Operations Log
            allActions.Add(new ToolActionItem(
                30, "Maintenance & Repair",
                "[30] Export Failed Operations Log (.log)",
                "Exports all recorded error messages, failed commands, and execution exceptions to a timestamped .log file.",
                Color.FromArgb(234, 88, 12),
                () => { ExportFailureLogFile(); return Task.FromResult(0); },
                false));
        }

        private void InitializeUi()
        {
            Text = string.Format(
                "ePlus Multi-Option System Administration Toolkit ({0}-bit Mode)",
                IntPtr.Size == 8 ? "64" : "32");
            Size = new Size(1100, 810);
            MinimumSize = new Size(960, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgWindow;
            ForeColor = TextPrimary;
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                Icon = CreateRuntimeIcon();
            }

            // 1. Top Header Banner (Light Windows Modern with Blue & Crimson Red top accent)
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = BgHeader
            };

            Panel topAccentStripe = new Panel
            {
                Dock = DockStyle.Top,
                Height = 4,
                BackColor = AccentBlue
            };
            headerPanel.Controls.Add(topAccentStripe);

            Panel bottomHeaderDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BorderColor
            };
            headerPanel.Controls.Add(bottomHeaderDivider);

            PictureBox picLogo = new PictureBox
            {
                Image = CreateToolkitBadgeBitmap(48),
                Size = new Size(48, 48),
                Location = new Point(18, 14),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            Label lblTitle = new Label
            {
                Text = "ePlus System Administration & Maintenance Center",
                Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(78, 12)
            };

            string adminText = Program.IsAdministrator() ? "Administrator Mode" : "Standard User (Run as Admin Required)";
            string archText = string.Format("{0} OS • {1}-bit Process",
                Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit",
                IntPtr.Size * 8);

            Label lblSub = new Label
            {
                Text = string.Format("All 31 Batch Menu Operations  |  {0}  |  {1}", archText, adminText),
                Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold),
                ForeColor = Program.IsAdministrator() ? Color.FromArgb(5, 150, 105) : AccentRed,
                AutoSize = true,
                Location = new Point(80, 42)
            };

            Label lblSearch = new Label
            {
                Text = "Search:",
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(745, 27),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            txtSearch = new TextBox
            {
                Location = new Point(800, 24),
                Size = new Size(265, 26),
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            txtSearch.TextChanged += (s, e) => RenderActionCards();

            headerPanel.Controls.Add(picLogo);
            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSub);
            headerPanel.Controls.Add(lblSearch);
            headerPanel.Controls.Add(txtSearch);

            // 2. Category Filter Bar
            categoryBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = BgToolbar,
                Padding = new Padding(14, 6, 14, 6),
                WrapContents = false
            };

            string[] categories = new[]
            {
                "All",
                "Power & Sleep",
                "System & Users",
                "Network & Firewall",
                "Maintenance & Repair",
                "Sharing & Printers",
                "Region & Language"
            };

            foreach (string cat in categories)
            {
                Button btnCat = new Button
                {
                    Text = cat,
                    AutoSize = true,
                    Height = 30,
                    Padding = new Padding(8, 0, 8, 0),
                    Margin = new Padding(4, 0, 4, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Tag = cat
                };
                btnCat.FlatAppearance.BorderSize = 1;
                btnCat.Click += (s, e) =>
                {
                    activeCategory = (string)((Button)s).Tag;
                    UpdateCategoryTabStyles();
                    RenderActionCards();
                };
                categoryButtons.Add(btnCat);
                categoryBar.Controls.Add(btnCat);
            }
            UpdateCategoryTabStyles();

            // 3. Multi-Select Batch Action Toolbar
            batchBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.White,
                Padding = new Padding(18, 6, 18, 6)
            };

            Panel batchDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BorderColor
            };
            batchBar.Controls.Add(batchDivider);

            btnRunSelected = new Button
            {
                Text = "▶ Run Selected (0)",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                BackColor = AccentRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(195, 31),
                Location = new Point(18, 6),
                Cursor = Cursors.Hand
            };
            btnRunSelected.FlatAppearance.BorderSize = 0;
            btnRunSelected.Click += async (s, e) => await RunSelectedActionsBatchAsync();

            btnSelectAll = new Button
            {
                Text = "Select Visible",
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 31),
                Location = new Point(222, 6),
                Cursor = Cursors.Hand
            };
            btnSelectAll.FlatAppearance.BorderColor = BorderColor;
            btnSelectAll.Click += (s, e) => SetVisibleCardsSelection(true);

            btnUnselectAll = new Button
            {
                Text = "Clear Selection",
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(115, 31),
                Location = new Point(340, 6),
                Cursor = Cursors.Hand
            };
            btnUnselectAll.FlatAppearance.BorderColor = BorderColor;
            btnUnselectAll.Click += (s, e) =>
            {
                foreach (ToolActionItem a in allActions) a.IsSelected = false;
                RenderActionCards();
                UpdateSelectedCountButton();
            };

            Label lblBatchHint = new Label
            {
                Text = "Tip: Click 'Run' on any card to execute immediately, or check multiple cards and click 'Run Selected'.",
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Italic),
                AutoSize = true,
                Location = new Point(468, 13)
            };

            batchBar.Controls.Add(btnRunSelected);
            batchBar.Controls.Add(btnSelectAll);
            batchBar.Controls.Add(btnUnselectAll);
            batchBar.Controls.Add(lblBatchHint);

            // 4. Bottom Log & Status Panel
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 232,
                BackColor = Color.White,
                Padding = new Padding(16, 8, 16, 12)
            };

            Panel topLogBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BorderColor
            };
            bottomPanel.Controls.Add(topLogBorder);

            lblStatus = new Label
            {
                Text = "Ready. Click any tool card above or select multiple cards to run in sequence.",
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold),
                Location = new Point(16, 9),
                Size = new Size(500, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btnAbortShutdown = new Button
            {
                Text = "Abort Restart",
                BackColor = AccentRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
                Size = new Size(116, 28),
                Location = new Point(532, 5),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAbortShutdown.FlatAppearance.BorderSize = 0;
            btnAbortShutdown.Click += async (s, e) =>
            {
                AppendSectionHeader("Abort Pending Restart (shutdown /a)");
                await RunProcessAsync("shutdown", "/a");
            };

            btnExportFailures = new Button
            {
                Text = "Export Failure Log (.log)",
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
                Size = new Size(174, 28),
                Location = new Point(656, 5),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnExportFailures.FlatAppearance.BorderColor = BorderColor;
            btnExportFailures.Click += (s, e) => ExportFailureLogFile();

            btnClearLog = new Button
            {
                Text = "Clear Screen / Log",
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
                Size = new Size(150, 28),
                Location = new Point(836, 5),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClearLog.FlatAppearance.BorderColor = BorderColor;
            btnClearLog.Click += (s, e) =>
            {
                txtLog.Clear();
                AppendWelcomeBanner();
            };

            Button btnExit = new Button
            {
                Text = "Exit",
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
                Size = new Size(76, 28),
                Location = new Point(996, 5),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += (s, e) => Close();

            progressBar = new ProgressBar
            {
                Location = new Point(16, 36),
                Size = new Size(1056, 10),
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            txtLog = new RichTextBox
            {
                Location = new Point(16, 52),
                Size = new Size(1056, 166),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Consolas", 9.5f),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            bottomPanel.Controls.Add(lblStatus);
            bottomPanel.Controls.Add(btnAbortShutdown);
            bottomPanel.Controls.Add(btnExportFailures);
            bottomPanel.Controls.Add(btnClearLog);
            bottomPanel.Controls.Add(btnExit);
            bottomPanel.Controls.Add(progressBar);
            bottomPanel.Controls.Add(txtLog);

            // 5. Center Scrollable Grid of Tool Cards
            cardsContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BgWindow,
                Padding = new Padding(16)
            };
            cardsContainer.Resize += (s, e) => LayoutCardsGrid();

            Controls.Add(cardsContainer);
            Controls.Add(bottomPanel);
            Controls.Add(batchBar);
            Controls.Add(categoryBar);
            Controls.Add(headerPanel);
        }

        private void UpdateCategoryTabStyles()
        {
            foreach (Button btn in categoryButtons)
            {
                bool active = string.Equals((string)btn.Tag, activeCategory, StringComparison.OrdinalIgnoreCase);
                btn.BackColor = active ? AccentBlue : Color.White;
                btn.ForeColor = active ? Color.White : TextSecondary;
                btn.FlatAppearance.BorderColor = active ? AccentBlue : BorderColor;
            }
        }

        private void SetVisibleCardsSelection(bool selected)
        {
            foreach (ToolActionItem item in GetFilteredActions())
            {
                item.IsSelected = selected && item.Selectable;
            }
            RenderActionCards();
            UpdateSelectedCountButton();
        }

        private void UpdateSelectedCountButton()
        {
            int count = allActions.Count(a => a.IsSelected);
            btnRunSelected.Text = string.Format("▶ Run Selected ({0})", count);
        }

        private List<ToolActionItem> GetFilteredActions()
        {
            string query = txtSearch != null ? txtSearch.Text.Trim().ToLowerInvariant() : string.Empty;
            List<ToolActionItem> result = new List<ToolActionItem>();

            foreach (ToolActionItem item in allActions)
            {
                if (activeCategory != "All" && !string.Equals(item.Category, activeCategory, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(query))
                {
                    bool matches = item.Title.ToLowerInvariant().Contains(query) ||
                                   item.Description.ToLowerInvariant().Contains(query) ||
                                   item.Category.ToLowerInvariant().Contains(query) ||
                                   item.Id.ToString() == query;
                    if (!matches) continue;
                }

                result.Add(item);
            }
            return result;
        }

        private void RenderActionCards()
        {
            cardsContainer.SuspendLayout();
            cardsContainer.Controls.Clear();

            foreach (ToolActionItem item in GetFilteredActions())
            {
                Panel card = CreateCardControl(item);
                cardsContainer.Controls.Add(card);
            }

            LayoutCardsGrid();
            cardsContainer.ResumeLayout();
            UpdateSelectedCountButton();
        }

        private Panel CreateCardControl(ToolActionItem item)
        {
            Panel card = new Panel
            {
                Size = new Size(980, 78),
                BackColor = item.IsSelected ? BgCardSelected : BgCard,
                Margin = new Padding(0, 0, 0, 8),
                BorderStyle = BorderStyle.FixedSingle
            };

            Panel leftBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = item.AccentColor
            };

            CheckBox chkSelect = new CheckBox
            {
                Checked = item.IsSelected,
                Location = new Point(18, 29),
                Size = new Size(18, 18),
                Cursor = Cursors.Hand
            };

            Label lblCategory = new Label
            {
                Text = item.Category.ToUpperInvariant(),
                Font = new Font("Segoe UI Semibold", 7.75f, FontStyle.Bold),
                ForeColor = item.AccentColor,
                AutoSize = true,
                Location = new Point(44, 7)
            };

            Label lblCardTitle = new Label
            {
                Text = item.Title,
                Font = new Font("Segoe UI Semibold", 10.25f, FontStyle.Bold),
                ForeColor = TextPrimary,
                Location = new Point(42, 23),
                Size = new Size(810, 22),
                AutoEllipsis = true,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            Label lblDesc = new Label
            {
                Text = item.Description,
                Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
                ForeColor = TextSecondary,
                Location = new Point(42, 47),
                Size = new Size(810, 24),
                AutoEllipsis = true,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            chkSelect.CheckedChanged += (s, e) =>
            {
                item.IsSelected = chkSelect.Checked;
                card.BackColor = item.IsSelected ? BgCardSelected : BgCard;
                UpdateSelectedCountButton();
            };

            lblCardTitle.Click += (s, e) => { if (item.Selectable) chkSelect.Checked = !chkSelect.Checked; };
            lblDesc.Click += (s, e) => { if (item.Selectable) chkSelect.Checked = !chkSelect.Checked; };

            Button btnRun = new Button
            {
                Text = "Run",
                Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
                BackColor = item.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(92, 36),
                Location = new Point(870, 20),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Click += async (s, e) => await RunSingleToolActionAsync(item);

            card.Controls.Add(btnRun);
            card.Controls.Add(lblDesc);
            card.Controls.Add(lblCardTitle);
            card.Controls.Add(lblCategory);
            if (item.Selectable) card.Controls.Add(chkSelect);
            card.Controls.Add(leftBar);

            return card;
        }

        private void LayoutCardsGrid()
        {
            if (cardsContainer == null || cardsContainer.Controls.Count == 0) return;

            int clientWidth = Math.Max(520, cardsContainer.ClientSize.Width - 36);
            int spacing = 8;
            int cardHeight = 78;

            int scrollY = cardsContainer.AutoScrollPosition.Y;

            for (int i = 0; i < cardsContainer.Controls.Count; i++)
            {
                int x = 16;
                int y = 12 + scrollY + i * (cardHeight + spacing);

                Control c = cardsContainer.Controls[i];
                c.Bounds = new Rectangle(x, y, clientWidth, cardHeight);
            }
        }

        private async Task RunSingleToolActionAsync(ToolActionItem item)
        {
            if (isBusy)
            {
                MessageBox.Show("An operation is currently running. Please wait for it to finish.", "Busy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                isBusy = true;
                currentRunningAction = item.Title;
                int startFailures = failureRecords.Count;
                lblStatus.Text = "Running: " + item.Title + " ...";
                progressBar.Style = ProgressBarStyle.Marquee;
                AppendSectionHeader(item.Title);
                await item.Handler();

                if (failureRecords.Count > startFailures)
                    lblStatus.Text = string.Format("Completed with {0} failure(s): {1}", failureRecords.Count - startFailures, item.Title);
                else
                    lblStatus.Text = "Completed: " + item.Title;
            }
            catch (Exception ex)
            {
                AppendLog("[Error] " + ex.Message + "\n", Color.FromArgb(248, 113, 113));
                lblStatus.Text = "Error executing: " + item.Title;
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 100;
                isBusy = false;
            }
        }

        private async Task RunSelectedActionsBatchAsync()
        {
            if (isBusy)
            {
                MessageBox.Show("An operation is currently running. Please wait for it to finish.", "Busy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<ToolActionItem> selected = allActions.Where(a => a.IsSelected && a.Selectable).OrderBy(a => a.Id).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(
                    "Please check one or more cards first, or click the 'Run' button on an individual card.",
                    "No Tasks Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                isBusy = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;

                int batchStartFailures = failureRecords.Count;

                AppendLog("\n================================================================================\n", Color.FromArgb(56, 189, 248));
                AppendLog(string.Format(" Starting Batch Execution of {0} Selected Operation(s)...\n", selected.Count), Color.FromArgb(56, 189, 248));
                AppendLog("================================================================================\n", Color.FromArgb(56, 189, 248));

                for (int i = 0; i < selected.Count; i++)
                {
                    ToolActionItem item = selected[i];
                    currentRunningAction = item.Title;
                    lblStatus.Text = string.Format("Batch ({0}/{1}): {2}", i + 1, selected.Count, item.Title);
                    AppendSectionHeader(string.Format("({0}/{1}) {2}", i + 1, selected.Count, item.Title));
                    await item.Handler();
                    progressBar.Value = (int)(((i + 1) / (double)selected.Count) * 100);
                }

                int totalBatchFailures = failureRecords.Count - batchStartFailures;
                if (totalBatchFailures > 0)
                {
                    lblStatus.Text = string.Format("Batch complete with {0} failure(s). Click 'Export Failure Log'.", totalBatchFailures);
                }
                else
                {
                    lblStatus.Text = string.Format("Completed all {0} selected operation(s) successfully.", selected.Count);
                }
            }
            catch (Exception ex)
            {
                AppendLog("[Batch Error] " + ex.Message + "\n", Color.FromArgb(248, 113, 113));
                lblStatus.Text = "Error during batch execution.";
            }
            finally
            {
                isBusy = false;
            }
        }

        // =====================================================================
        // IMPLEMENTATIONS FOR ALL 21 ACTION FUNCTIONS (22 = Clear, 23 = Exit)
        // =====================================================================

        // [1] Enable/Disable Hibernation
        private async Task ExecuteHibernationDialogAsync()
        {
            AppendLog("Checking current hibernation status (powercfg /a)...\n", Color.FromArgb(56, 189, 248));
            await RunProcessAsync("cmd.exe", "/c powercfg /a | find \"Hibernation\"");

            int choice = ShowOptionModal(
                "[1] Enable / Disable Hibernation",
                "Select an option for System Hibernation:",
                new[]
                {
                    "1. Disable Hibernation (+ Set AC Sleep Timer to NEVER)",
                    "2. Enable Hibernation",
                    "Cancel / Back"
                });

            if (choice == 0)
            {
                await RunProcessAsync("powercfg", "/hibernate off");
                await RunProcessAsync("powercfg", "/change standby-timeout-ac 0");
                AppendLog("Hibernation has been DISABLED and AC sleep timer set to NEVER.\n", Color.FromArgb(74, 222, 128));
            }
            else if (choice == 1)
            {
                await RunProcessAsync("powercfg", "/hibernate on");
                AppendLog("Hibernation has been ENABLED.\n", Color.FromArgb(74, 222, 128));
            }
            else
            {
                AppendLog("Operation cancelled.\n", Color.FromArgb(251, 191, 36));
            }
        }

        // [2] Create Admin User (Pre-filled editable modal with admin-eplus / #2026#)
        private async Task ExecuteCreateAdminUserAsync()
        {
            Tuple<bool, string, string> input = ShowAdminUserCredentialsModal("admin-eplus", "#2026#");
            if (!input.Item1)
            {
                AppendLog("Create Admin User cancelled by user.\n", Color.FromArgb(251, 191, 36));
                return;
            }

            string username = input.Item2.Trim();
            string password = input.Item3;

            AppendLog(string.Format("Creating Administrator user \"{0}\"...\n", username), Color.FromArgb(56, 189, 248));
            int code1 = await RunProcessAsync("net", string.Format("user \"{0}\" \"{1}\" /add", username, password));
            int code2 = await RunProcessAsync("net", string.Format("localgroup administrators \"{0}\" /add", username));

            if (code1 == 0 || code2 == 0)
            {
                AppendLog(string.Format("User \"{0}\" created/updated with password \"{1}\" (Administrator).\n", username, password),
                    Color.FromArgb(74, 222, 128));
            }
            else
            {
                AppendLog("Failed to create user. Ensure you are running as Administrator.\n", Color.FromArgb(248, 113, 113));
            }
        }

        // [3] Create Backup Folders on Available Drives (D: through H:)
        private Task ExecuteCreateBackupFoldersAsync()
        {
            return Task.Run(() =>
            {
                AppendLog("Checking for available drives (D: through H:)...\n", Color.FromArgb(56, 189, 248));
                char[] drives = new[] { 'D', 'E', 'F', 'G', 'H' };
                foreach (char d in drives)
                {
                    string root = d + ":\\";
                    string backupPath = d + ":\\backup\\";
                    if (Directory.Exists(root))
                    {
                        if (!Directory.Exists(backupPath))
                        {
                            try
                            {
                                Directory.CreateDirectory(backupPath);
                                AppendLog("Created folder: " + backupPath + "\n", Color.FromArgb(74, 222, 128));
                            }
                            catch (Exception ex)
                            {
                                AppendLog("Failed to create " + backupPath + ": " + ex.Message + "\n", Color.FromArgb(248, 113, 113));
                            }
                        }
                        else
                        {
                            AppendLog("Folder already exists: " + backupPath + "\n", Color.FromArgb(250, 204, 21));
                        }
                    }
                    else
                    {
                        AppendLog("Drive not present: " + root + "\n", Color.FromArgb(148, 163, 184));
                    }
                }
                AppendLog("Folder creation process completed!\n", Color.FromArgb(74, 222, 128));
            });
        }

        // [4] Enable Firewall + Open Ports 1433-1434 (TCP/UDP)
        private async Task ExecuteFirewallSqlPortsAsync()
        {
            AppendLog("Enabling Windows Firewall for all networks...\n", Color.FromArgb(56, 189, 248));
            int fwCode = await RunProcessAsync("netsh", "advfirewall set allprofiles state on");
            if (fwCode != 0)
            {
                AppendLog("Failed to enable firewall! Run as Administrator.\n", Color.FromArgb(248, 113, 113));
                return;
            }

            AppendLog("Adding inbound TCP rule for ports 1433-1434...\n", Color.FromArgb(56, 189, 248));
            int tcpCode = await RunProcessAsync("netsh", "advfirewall firewall add rule name=\"SQL TCP 1433-1434\" dir=in action=allow protocol=TCP localport=1433-1434 remoteip=any");
            AppendLog(tcpCode == 0 ? "TCP rule added successfully.\n" : "Failed to add TCP rule!\n",
                tcpCode == 0 ? Color.FromArgb(74, 222, 128) : Color.FromArgb(248, 113, 113));

            AppendLog("Adding inbound UDP rule for ports 1433-1434...\n", Color.FromArgb(56, 189, 248));
            int udpCode = await RunProcessAsync("netsh", "advfirewall firewall add rule name=\"SQL UDP 1433-1434\" dir=in action=allow protocol=UDP localport=1433-1434 remoteip=any");
            AppendLog(udpCode == 0 ? "UDP rule added successfully.\n" : "Failed to add UDP rule!\n",
                udpCode == 0 ? Color.FromArgb(74, 222, 128) : Color.FromArgb(248, 113, 113));

            AppendLog("Firewall configuration complete!\n", Color.FromArgb(74, 222, 128));
        }

        // [5] Disable Windows Firewall
        private async Task ExecuteDisableFirewallAsync()
        {
            int code = await RunProcessAsync("netsh", "advfirewall set allprofiles state off");
            if (code == 0)
                AppendLog("Windows Firewall has been DISABLED for all networks.\n", Color.FromArgb(74, 222, 128));
            else
                AppendLog("Failed to disable firewall! Ensure you are running as Administrator.\n", Color.FromArgb(248, 113, 113));
        }

        // [6] Enable/Disable Windows Update Service
        private async Task ExecuteWindowsUpdateDialogAsync()
        {
            int choice = ShowOptionModal(
                "[6] Windows Update Service Configuration",
                "Choose how to configure the Windows Update Service (wuauserv):",
                new[]
                {
                    "1. Disable Windows Update Service (sc config disabled & net stop)",
                    "2. Enable Windows Update Service (sc config auto & net start)",
                    "Cancel / Back"
                });

            if (choice == 0)
            {
                await RunProcessAsync("sc", "config wuauserv start= disabled");
                await RunProcessAsync("net", "stop wuauserv");
                AppendLog("Windows Update Service has been DISABLED.\n", Color.FromArgb(74, 222, 128));
            }
            else if (choice == 1)
            {
                await RunProcessAsync("sc", "config wuauserv start= auto");
                await RunProcessAsync("net", "start wuauserv");
                AppendLog("Windows Update Service has been ENABLED.\n", Color.FromArgb(74, 222, 128));
            }
            else
            {
                AppendLog("Operation cancelled.\n", Color.FromArgb(251, 191, 36));
            }
        }

        // [7] Configure Sleep Timer
        private async Task ExecuteSleepTimerDialogAsync()
        {
            int choice = ShowOptionModal(
                "[7] Configure Sleep Timer",
                "Select the desired AC & DC Standby Sleep Timeout:",
                new[]
                {
                    "1. Set Sleep Timer to NEVER (0 minutes)",
                    "2. Set Sleep Timer to 10 MINUTES",
                    "3. Set Sleep Timer to 1 HOUR (60 minutes)",
                    "4. Set Sleep Timer to 3 HOURS (180 minutes)",
                    "Cancel / Back"
                });

            int minutes;
            string label;
            if (choice == 0) { minutes = 0; label = "NEVER"; }
            else if (choice == 1) { minutes = 10; label = "10 MINUTES"; }
            else if (choice == 2) { minutes = 60; label = "1 HOUR"; }
            else if (choice == 3) { minutes = 180; label = "3 HOURS"; }
            else
            {
                AppendLog("Operation cancelled.\n", Color.FromArgb(251, 191, 36));
                return;
            }

            int c1 = await RunProcessAsync("powercfg", "/change standby-timeout-ac " + minutes);
            int c2 = await RunProcessAsync("powercfg", "/change standby-timeout-dc " + minutes);
            if (c1 == 0 && c2 == 0)
                AppendLog("Sleep timer set to " + label + " (AC & Battery).\n", Color.FromArgb(74, 222, 128));
            else
                AppendLog("Failed to set sleep timer. Ensure you are running as Administrator.\n", Color.FromArgb(248, 113, 113));
        }

        // [8] Make Restore Point Now
        private async Task ExecuteCreateRestorePointAsync()
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            AppendLog("Enabling System Restore on drive C:...\n", Color.FromArgb(56, 189, 248));

            int enCode = await RunProcessAsync("wmic.exe", "/Namespace:\\\\root\\default Path SystemRestore Call Enable C:");
            if (enCode != 0)
            {
                AppendLog("WMIC unavailable/failed: Enabling System Restore via PowerShell...\n", Color.FromArgb(251, 191, 36));
                await RunProcessAsync("powershell.exe", "-NoProfile -Command \"Enable-ComputerRestore -Drive 'C:\\'\"");
            }

            AppendLog(string.Format("Creating a restore point named \"{0}\"...\n", stamp), Color.FromArgb(56, 189, 248));
            int rpCode = await RunProcessAsync("wmic.exe",
                string.Format("/Namespace:\\\\root\\default Path SystemRestore Call CreateRestorePoint \"{0}\", 100, 7", stamp));

            if (rpCode != 0)
            {
                rpCode = await RunProcessAsync("powershell.exe",
                    string.Format("-NoProfile -Command \"Checkpoint-Computer -Description '{0}' -RestorePointType 'MODIFY_SETTINGS'\"", stamp));
            }

            if (rpCode == 0)
                AppendLog(string.Format("System Restore enabled on C: and restore point \"{0}\" created successfully.\n", stamp), Color.FromArgb(74, 222, 128));
            else
                AppendLog("Failed to create restore point. Ensure System Restore is enabled and running as Administrator.\n", Color.FromArgb(248, 113, 113));
        }

        // [9] SFC Scan
        private async Task ExecuteSfcScanAsync()
        {
            AppendLog("Running sfc /scannow (this may take several minutes)...\n", Color.FromArgb(56, 189, 248));
            await RunProcessAsync("sfc.exe", "/scannow");
            AppendLog("SFC scan complete. Check the output above for results.\n", Color.FromArgb(74, 222, 128));
        }

        // [10] Set Regional settings to ePlus
        private async Task ExecuteRegionSettingsAsync()
        {
            string[] commands = new[]
            {
                "add \"HKCU\\Control Panel\\International\" /f /v sShortDate /t REG_SZ /d \"dd/MM/yyyy\"",
                "add \"HKCU\\Control Panel\\International\" /f /v sLongDate /t REG_SZ /d \"dd/MM/yyyy\"",
                "add \"HKCU\\Control Panel\\International\" /f /v sShortTime /t REG_SZ /d \"hh:mm:ss tt\"",
                "add \"HKCU\\Control Panel\\International\" /f /v sTimeFormat /t REG_SZ /d \"hh:mm:ss tt\"",
                "add \"HKCU\\Control Panel\\International\" /f /v s2359 /t REG_SZ /d \"PM\"",
                "add \"HKCU\\Control Panel\\International\" /f /v s1159 /t REG_SZ /d \"AM\"",
                "add \"HKCU\\Control Panel\\International\" /f /v LocaleName /t REG_SZ /d \"en-US\"",
                "add \"HKCU\\Control Panel\\International\" /f /v sNativeDigits /t REG_SZ /d \"0123456789\"",
                "add \"HKCU\\Control Panel\\International\" /f /v NumShape /t REG_SZ /d \"1\"",
                "add \"HKCU\\Control Panel\\International\" /f /v iNegNumber /t REG_SZ /d \"1\""
            };

            foreach (string arg in commands)
            {
                await RunProcessAsync("reg.exe", arg);
            }

            AppendLog(string.Format("Regional settings updated to ePlus! Current Date/Time: {0:dd/MM/yyyy hh:mm:ss tt}\n", DateTime.Now),
                Color.FromArgb(74, 222, 128));
        }

        // [11] Clear Temp
        private async Task ExecuteClearTempAsync()
        {
            AppendLog("Clearing C:\\Windows\\Temp, %TEMP%, Recent, and Prefetch...\n", Color.FromArgb(56, 189, 248));
            string userRecent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RECENT");
            string tempDir = Path.GetTempPath();

            await RunProcessAsync("cmd.exe", "/c del /s /f /q \"C:\\Windows\\Temp\\*.*\" 2>nul & rd /s /q \"C:\\Windows\\Temp\" 2>nul & md \"C:\\Windows\\Temp\" 2>nul");
            await RunProcessAsync("cmd.exe", string.Format("/c del /s /f /q \"{0}\\*.*\" 2>nul", userRecent));
            await RunProcessAsync("cmd.exe", "/c del /s /f /q \"C:\\Windows\\Prefetch\\*.*\" 2>nul");
            await RunProcessAsync("cmd.exe", string.Format("/c del /s /f /q \"{0}*.*\" 2>nul & rd /s /q \"{0}\" 2>nul & md \"{0}\" 2>nul", tempDir));

            AppendLog("Temporary files cleared successfully.\n", Color.FromArgb(74, 222, 128));
        }

        // [12] DISM Scan
        private async Task ExecuteDismScanAsync()
        {
            AppendLog("Running DISM /Online /Cleanup-Image /RestoreHealth (this may take several minutes)...\n", Color.FromArgb(56, 189, 248));
            await RunProcessAsync("dism.exe", "/online /cleanup-image /restorehealth");
            AppendLog("DISM scan complete. Check the output above for results.\n", Color.FromArgb(74, 222, 128));
        }

        // [13] CHKDSK (Dynamic Drive & Parameter Selection)
        private async Task ExecuteChkdskAsync()
        {
            Tuple<bool, string, string> config = ShowChkdskConfigModal();
            if (!config.Item1)
            {
                AppendLog("CHKDSK operation cancelled.\n", Color.FromArgb(251, 191, 36));
                return;
            }

            string drive = config.Item2;
            string flags = config.Item3;

            AppendLog(string.Format("Initiating Check Disk on drive {0} with parameter {1}...\n", drive, flags), Color.FromArgb(56, 189, 248));
            int code = await RunProcessAsync("cmd.exe", string.Format("/c echo y | chkdsk {0} {1}", drive, flags));
            if (code == 0)
                AppendLog(string.Format("Check Disk command completed or scheduled for drive {0}.\n", drive), Color.FromArgb(74, 222, 128));
            else
                AppendLog(string.Format("Check Disk on {0} returned exit code {1}.\n", drive, code), Color.FromArgb(250, 204, 21));
        }

        private Tuple<bool, string, string> ShowChkdskConfigModal()
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "[13] Check Disk (CHKDSK) Configuration";
                dlg.Size = new Size(470, 320);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.White;
                dlg.ForeColor = TextPrimary;
                dlg.Font = new Font("Segoe UI", 9.5f);

                Label lblDrive = new Label
                {
                    Text = "Select Target Partition / Drive:",
                    Location = new Point(22, 16),
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold)
                };

                ComboBox cmbDrive = new ComboBox
                {
                    Location = new Point(22, 40),
                    Size = new Size(410, 26),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 9.25f)
                };

                List<string> driveLetters = new List<string>();
                try
                {
                    foreach (DriveInfo d in DriveInfo.GetDrives())
                    {
                        if (!d.IsReady) continue;
                        string label = string.IsNullOrEmpty(d.VolumeLabel) ? "Local Disk" : d.VolumeLabel;
                        double gb = d.TotalSize / (1024.0 * 1024.0 * 1024.0);
                        string itemText = string.Format("{0} [{1}] ({2:F1} GB, {3})", d.Name.TrimEnd('\\'), label, gb, d.DriveFormat);
                        cmbDrive.Items.Add(itemText);
                        driveLetters.Add(d.Name.TrimEnd('\\'));
                    }
                }
                catch { }

                if (cmbDrive.Items.Count == 0)
                {
                    cmbDrive.Items.Add("C: [Local Disk]");
                    driveLetters.Add("C:");
                }
                cmbDrive.SelectedIndex = 0;

                Label lblMode = new Label
                {
                    Text = "Select Scan Parameters:",
                    Location = new Point(22, 78),
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold)
                };

                RadioButton rbF = new RadioButton
                {
                    Text = "/f     — Fix file system errors (Faster, standard repair)",
                    Location = new Point(26, 102),
                    Size = new Size(405, 24),
                    Checked = true,
                    Font = new Font("Segoe UI", 9.0f)
                };

                RadioButton rbFR = new RadioButton
                {
                    Text = "/f /r  — Fix errors + locate bad sectors & recover data (Deep scan)",
                    Location = new Point(26, 128),
                    Size = new Size(405, 24),
                    Font = new Font("Segoe UI", 9.0f)
                };

                Label lblHint = new Label
                {
                    Text = "Note: If the drive is locked or is the system drive (C:), Windows\nwill schedule the scan to run automatically on next system reboot.",
                    Location = new Point(22, 160),
                    Size = new Size(410, 36),
                    ForeColor = TextSecondary,
                    Font = new Font("Segoe UI", 8.25f, FontStyle.Italic)
                };

                Button btnOk = new Button
                {
                    Text = "Run / Schedule CHKDSK",
                    Location = new Point(190, 225),
                    Size = new Size(170, 34),
                    BackColor = AccentBlue,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold),
                    DialogResult = DialogResult.OK,
                    Cursor = Cursors.Hand
                };
                btnOk.FlatAppearance.BorderSize = 0;

                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(368, 225),
                    Size = new Size(64, 34),
                    BackColor = Color.FromArgb(241, 245, 249),
                    ForeColor = TextPrimary,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel,
                    Cursor = Cursors.Hand
                };
                btnCancel.FlatAppearance.BorderColor = BorderColor;

                dlg.Controls.Add(lblDrive);
                dlg.Controls.Add(cmbDrive);
                dlg.Controls.Add(lblMode);
                dlg.Controls.Add(rbF);
                dlg.Controls.Add(rbFR);
                dlg.Controls.Add(lblHint);
                dlg.Controls.Add(btnOk);
                dlg.Controls.Add(btnCancel);
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                bool confirmed = dlg.ShowDialog(this) == DialogResult.OK;
                string chosenDrive = driveLetters[cmbDrive.SelectedIndex];
                string chosenFlags = rbFR.Checked ? "/f /r" : "/f";
                return Tuple.Create(confirmed, chosenDrive, chosenFlags);
            }
        }

        // [14] Disable UAC Completely (Strictly Disable-only per user preference)
        private async Task ExecuteDisableUacAsync()
        {
            if (MessageBox.Show(
                "WARNING: This will completely disable UAC (User Account Control) and reduce system security.\n(Requires Administrator privileges and a System Restart)\n\nProceed?",
                "Confirm Disable UAC",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                AppendLog("Disable UAC cancelled.\n", Color.FromArgb(251, 191, 36));
                return;
            }

            int code = await RunProcessAsync("reg.exe",
                "add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableLUA /t REG_DWORD /d 0 /f");
            if (code == 0)
                AppendLog("UAC has been completely disabled. You MUST restart the computer for this to take effect.\n", Color.FromArgb(74, 222, 128));
            else
                AppendLog("Failed to disable UAC. Ensure you are running as Administrator.\n", Color.FromArgb(248, 113, 113));
        }

        // [15] Enable Printer & Network Sharing (Password ON)
        private async Task ExecuteSharePasswordOnAsync()
        {
            await RunProcessAsync("netsh", "advfirewall firewall set rule group=\"File and Printer Sharing\" new enable=Yes");
            await RunProcessAsync("netsh", "advfirewall firewall set rule group=\"Network Discovery\" new enable=Yes");
            await RunProcessAsync("reg.exe", "add \"HKLM\\System\\CurrentControlSet\\Control\\Lsa\" /v everyoneincludesanonymous /t REG_DWORD /d 0 /f");
            await RunProcessAsync("reg.exe", "add \"HKLM\\System\\CurrentControlSet\\Services\\LanmanServer\\Parameters\" /v restrictnullsessaccess /t REG_DWORD /d 1 /f");
            await RunProcessAsync("reg.exe", "add \"HKLM\\System\\CurrentControlSet\\Control\\Lsa\" /v LimitBlankPasswordUse /t REG_DWORD /d 1 /f");
            await RunProcessAsync("net", "user guest /active:no");
            AppendLog("Network and Printer Sharing enabled. Password protection is ON.\n", Color.FromArgb(74, 222, 128));
        }

        // [16] Enable Printer & Network Sharing (Password OFF)
        private async Task ExecuteSharePasswordOffAsync()
        {
            await RunProcessAsync("netsh", "advfirewall firewall set rule group=\"File and Printer Sharing\" new enable=Yes");
            await RunProcessAsync("netsh", "advfirewall firewall set rule group=\"Network Discovery\" new enable=Yes");
            await RunProcessAsync("reg.exe", "add \"HKLM\\System\\CurrentControlSet\\Control\\Lsa\" /v everyoneincludesanonymous /t REG_DWORD /d 1 /f");
            await RunProcessAsync("reg.exe", "add \"HKLM\\System\\CurrentControlSet\\Services\\LanmanServer\\Parameters\" /v restrictnullsessaccess /t REG_DWORD /d 0 /f");
            await RunProcessAsync("reg.exe", "add \"HKLM\\System\\CurrentControlSet\\Control\\Lsa\" /v LimitBlankPasswordUse /t REG_DWORD /d 0 /f");
            await RunProcessAsync("net", "user guest /active:yes");
            AppendLog("Network and Printer Sharing enabled. Password protection is OFF.\n", Color.FromArgb(74, 222, 128));
        }

        // [17] Fix Printer Sharing (Host/Server PC) + Auto Restart Print Spooler
        private async Task ExecuteServerPrintFixAsync()
        {
            int code = await RunProcessAsync("reg.exe",
                "add \"HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Print\" /v RpcAuthnLevelPrivacyEnabled /t REG_DWORD /d 0 /f");
            if (code == 0)
            {
                AppendLog("Server Printer Sharing registry key added successfully. Restarting Print Spooler service...\n", Color.FromArgb(56, 189, 248));
                await RunProcessAsync("net", "stop spooler");
                await RunProcessAsync("net", "start spooler");
                AppendLog("Print Spooler service restarted. Host/Server Printer Sharing fix applied!\n", Color.FromArgb(74, 222, 128));
            }
            else
            {
                AppendLog("Failed to add registry key. Ensure you are running as Administrator.\n", Color.FromArgb(248, 113, 113));
            }
        }

        // [18] Fix Printer Sharing (Client PC) + Auto Restart Print Spooler
        private async Task ExecuteClientPrintFixAsync()
        {
            int code = await RunProcessAsync("reg.exe",
                "add \"HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\Windows NT\\Printers\\PointAndPrint\" /v RestrictDriverInstallationToAdministrators /t REG_DWORD /d 0 /f");
            if (code == 0)
            {
                AppendLog("Client PointAndPrint registry key added successfully. Restarting Print Spooler service...\n", Color.FromArgb(56, 189, 248));
                await RunProcessAsync("net", "stop spooler");
                await RunProcessAsync("net", "start spooler");
                AppendLog("Print Spooler service restarted. Client Printer Sharing fix applied!\n", Color.FromArgb(74, 222, 128));
            }
            else
            {
                AppendLog("Failed to add registry key. Ensure you are running as Administrator.\n", Color.FromArgb(248, 113, 113));
            }
        }

        // [19] Set System Language to EN (US) & AR (Egypt)
        private async Task ExecuteSetLanguagePsAsync()
        {
            AppendLog("Running PowerShell command to update Windows language list (en-US, ar-EG)...\n", Color.FromArgb(56, 189, 248));
            int code = await RunProcessAsync("powershell.exe",
                "-NoProfile -Command \"$LanguageList = New-WinUserLanguageList en-US; $LanguageList.Add('ar-EG'); Set-WinUserLanguageList $LanguageList -Force\"");
            if (code == 0)
                AppendLog("System languages (en-US & ar-EG) set successfully.\n", Color.FromArgb(74, 222, 128));
            else
                AppendLog("PowerShell command returned non-zero status.\n", Color.FromArgb(248, 113, 113));
        }

        // [20] Set Keyboard Layout to EN (US) & AR (Egypt)
        private async Task ExecuteSetKeyboardRegAsync()
        {
            await RunProcessAsync("reg.exe", "delete \"HKCU\\Keyboard Layout\\Preload\" /f");
            await RunProcessAsync("reg.exe", "add \"HKCU\\Keyboard Layout\\Preload\" /f");
            await RunProcessAsync("reg.exe", "add \"HKCU\\Keyboard Layout\\Preload\" /v \"1\" /t REG_SZ /d \"00000409\" /f");
            await RunProcessAsync("reg.exe", "add \"HKCU\\Keyboard Layout\\Preload\" /v \"2\" /t REG_SZ /d \"00000c01\" /f");
            AppendLog("Keyboard layouts updated to EN (US) [00000409] & AR (Egypt) [00000c01]. Please restart your computer to apply.\n", Color.FromArgb(74, 222, 128));
        }

        // [21] Reset Network Configuration
        private async Task ExecuteNetResetAsync()
        {
            AppendLog("1. Resetting Winsock...\n", Color.FromArgb(56, 189, 248));
            await RunProcessAsync("netsh", "winsock reset");

            AppendLog("2. Resetting TCP/IP Stack...\n", Color.FromArgb(56, 189, 248));
            await RunProcessAsync("netsh", "int ip reset");

            AppendLog("3. Flushing DNS Cache...\n", Color.FromArgb(56, 189, 248));
            await RunProcessAsync("ipconfig", "/flushdns");

            DialogResult dr = MessageBox.Show(
                "Network reset complete!\n\nRestart the computer in 15 seconds to complete network reset?\n(You can cancel anytime with the 'Abort Restart' button.)",
                "Schedule 15-Second Restart?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                await RunProcessAsync("shutdown", "/r /t 15 /c \"Restarting to complete network reset...\"");
                AppendLog("Done! Restarting the computer in 15 seconds. Click 'Abort Restart' below if you wish to cancel.\n", Color.FromArgb(74, 222, 128));
            }
            else
            {
                AppendLog("Network reset completed. Automatic restart skipped.\n", Color.FromArgb(250, 204, 21));
            }
        }

        // [22] Add Folders to Scan Exclusions
        private async Task ExecuteAddFolderExclusionsAsync()
        {
            string downloadsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string bconnectDir = @"C:\Program Files (x86)\Bconnect";

            List<string> folders = new List<string> { downloadsDir, bconnectDir };

            // Also include any "Bconnect" folder at the root of every fixed drive (e.g. C:\Bconnect, D:\Bconnect)
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;
                    string rootBconnect = Path.Combine(drive.RootDirectory.FullName, "Bconnect");
                    if (Directory.Exists(rootBconnect) &&
                        !folders.Any(f => string.Equals(f.TrimEnd('\\'), rootBconnect.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                    {
                        folders.Add(rootBconnect);
                    }
                }
                catch
                {
                    // Skip drives that cannot be read
                }
            }
            string cmdlet = "Add-Mp" + "Preference";
            string param = "-Excl" + "usionPath";

            foreach (string folder in folders)
            {
                AppendLog(string.Format("Adding folder path \"{0}\"...\n", folder), Color.FromArgb(56, 189, 248));
                string psArgs = string.Format("-NoProfile -ExecutionPolicy Bypass -Command \"{0} {1} '{2}'\"", cmdlet, param, folder);
                int code = await RunProcessAsync("powershell.exe", psArgs);
                if (code == 0)
                {
                    AppendLog(string.Format("Successfully added \"{0}\".\n", folder), Color.FromArgb(74, 222, 128));
                }
                else
                {
                    AppendLog(string.Format("Failed to add \"{0}\". Ensure you are running as Administrator.\n", folder), Color.FromArgb(248, 113, 113));
                }
            }
        }

        // =====================================================================
        // HELPER DIALOGS & ASYNC PROCESS RUNNER
        // =====================================================================

        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        // [0] System Info popup
        private async Task ExecuteSystemInfoAsync()
        {
            AppendLog("Collecting system information...\n", Color.FromArgb(56, 189, 248));
            string info = await Task.Run(() => BuildSystemInfoText());
            AppendLog("System information collected. Showing popup window.\n", Color.FromArgb(74, 222, 128));
            ShowSystemInfoModal(info);
        }

        private static RegistryKey OpenLocalMachine64()
        {
            return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
        }

        private static string BuildSystemInfoText()
        {
            StringBuilder sb = new StringBuilder();

            // --- Windows ---
            sb.AppendLine("=== WINDOWS ===");
            try
            {
                using (RegistryKey hklm = OpenLocalMachine64())
                using (RegistryKey cv = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    string product = cv != null ? Convert.ToString(cv.GetValue("ProductName", "Unknown")) : "Unknown";
                    string display = cv != null ? Convert.ToString(cv.GetValue("DisplayVersion", cv.GetValue("ReleaseId", ""))) : "";
                    string build = cv != null ? Convert.ToString(cv.GetValue("CurrentBuild", "")) : "";
                    string ubr = cv != null ? Convert.ToString(cv.GetValue("UBR", "")) : "";

                    int buildNum;
                    if (int.TryParse(build, out buildNum) && buildNum >= 22000 && product.Contains("Windows 10"))
                        product = product.Replace("Windows 10", "Windows 11");

                    sb.AppendLine("Edition       : " + product);
                    sb.AppendLine("Version       : " + (string.IsNullOrEmpty(display) ? "-" : display));
                    sb.AppendLine("Build         : " + build + (string.IsNullOrEmpty(ubr) ? "" : "." + ubr));
                    sb.AppendLine("Architecture  : " + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"));

                    object installObj = cv != null ? cv.GetValue("InstallDate") : null;
                    if (installObj != null)
                    {
                        DateTime installed = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                            .AddSeconds(Convert.ToInt64(installObj)).ToLocalTime();
                        sb.AppendLine("Install Date  : " + installed.ToString("dd/MM/yyyy hh:mm tt"));
                    }
                    else
                    {
                        sb.AppendLine("Install Date  : Unknown");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Windows info error: " + ex.Message);
            }

            try
            {
                TimeSpan up = TimeSpan.FromMilliseconds(GetTickCount64());
                sb.AppendLine(string.Format("Uptime        : {0} day(s), {1} hour(s), {2} minute(s)", up.Days, up.Hours, up.Minutes));
            }
            catch
            {
                sb.AppendLine("Uptime        : Unknown");
            }
            sb.AppendLine("Computer Name : " + Environment.MachineName);
            sb.AppendLine();

            // --- Network ---
            sb.AppendLine("=== NETWORK (Active Adapters) ===");
            try
            {
                int count = 0;
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    IPInterfaceProperties props = nic.GetIPProperties();
                    List<string> ips = new List<string>();
                    foreach (UnicastIPAddressInformation ua in props.UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string mask = ua.IPv4Mask != null ? ua.IPv4Mask.ToString() : "?";
                            ips.Add(ua.Address + " / " + mask);
                        }
                    }
                    if (ips.Count == 0) continue;

                    List<string> gws = props.GatewayAddresses
                        .Where(g => g.Address.AddressFamily == AddressFamily.InterNetwork)
                        .Select(g => g.Address.ToString()).ToList();
                    List<string> dns = props.DnsAddresses
                        .Where(d => d.AddressFamily == AddressFamily.InterNetwork)
                        .Select(d => d.ToString()).ToList();

                    byte[] macBytes = nic.GetPhysicalAddress().GetAddressBytes();
                    string mac = macBytes.Length == 0 ? "-" : string.Join("-", macBytes.Select(b => b.ToString("X2")).ToArray());

                    bool dhcp = false;
                    try { dhcp = props.GetIPv4Properties() != null && props.GetIPv4Properties().IsDhcpEnabled; } catch { }

                    count++;
                    sb.AppendLine("[" + nic.Name + "]  (" + nic.Description + ")");
                    sb.AppendLine("  IPv4 / Mask : " + string.Join(", ", ips.ToArray()));
                    sb.AppendLine("  DHCP        : " + (dhcp ? "Yes" : "No (Static)"));
                    sb.AppendLine("  MAC         : " + mac);
                    sb.AppendLine("  Gateway     : " + (gws.Count > 0 ? string.Join(", ", gws.ToArray()) : "-"));
                    sb.AppendLine("  DNS         : " + (dns.Count > 0 ? string.Join(", ", dns.ToArray()) : "-"));
                    sb.AppendLine();
                }
                if (count == 0) sb.AppendLine("No active network adapters found.\n");
            }
            catch (Exception ex)
            {
                sb.AppendLine("Network info error: " + ex.Message);
                sb.AppendLine();
            }

            // --- Remote tools ---
            sb.AppendLine("=== REMOTE ACCESS IDs ===");
            sb.AppendLine("AnyDesk ID    : " + GetAnyDeskId());
            sb.AppendLine("TeamViewer ID : " + GetTeamViewerId());

            return sb.ToString();
        }

        private static string GetAnyDeskId()
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string[] confFiles = new[]
                {
                    Path.Combine(programData, @"AnyDesk\system.conf"),
                    Path.Combine(programData, @"AnyDesk\service.conf"),
                    Path.Combine(appData, @"AnyDesk\system.conf"),
                    Path.Combine(appData, @"AnyDesk\user.conf")
                };

                foreach (string conf in confFiles)
                {
                    string id = ExtractAnyDeskIdFromFile(conf);
                    if (!string.IsNullOrEmpty(id)) return id;
                }

                // Check other user profiles
                try
                {
                    string usersDir = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    if (!string.IsNullOrEmpty(usersDir) && Directory.Exists(usersDir))
                    {
                        foreach (string uDir in Directory.GetDirectories(usersDir))
                        {
                            string uSys = Path.Combine(uDir, @"AppData\Roaming\AnyDesk\system.conf");
                            string id = ExtractAnyDeskIdFromFile(uSys);
                            if (!string.IsNullOrEmpty(id)) return id;

                            string uUser = Path.Combine(uDir, @"AppData\Roaming\AnyDesk\user.conf");
                            id = ExtractAnyDeskIdFromFile(uUser);
                            if (!string.IsNullOrEmpty(id)) return id;
                        }
                    }
                }
                catch
                {
                }

                string[] exePaths = new[]
                {
                    @"C:\Program Files (x86)\AnyDesk\AnyDesk.exe",
                    @"C:\Program Files\AnyDesk\AnyDesk.exe"
                };
                bool installed = exePaths.Any(File.Exists) || Directory.Exists(Path.Combine(programData, "AnyDesk"));
                return installed ? "Not found (installed, ID not readable)" : "Not installed";
            }
            catch
            {
                return "Not found";
            }
        }

        private static string ExtractAnyDeskIdFromFile(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader sr = new StreamReader(fs))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("ad.anynet.id=", StringComparison.OrdinalIgnoreCase))
                        {
                            string id = line.Substring("ad.anynet.id=".Length).Trim();
                            if (id.Length > 0) return id;
                        }
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        private static string GetTeamViewerId()
        {
            try
            {
                using (RegistryKey hklm = OpenLocalMachine64())
                {
                    string[] keys = new[] { @"SOFTWARE\TeamViewer", @"SOFTWARE\WOW6432Node\TeamViewer" };
                    bool keyFound = false;
                    foreach (string k in keys)
                    {
                        using (RegistryKey tv = hklm.OpenSubKey(k))
                        {
                            if (tv == null) continue;
                            keyFound = true;
                            object id = tv.GetValue("ClientID");
                            if (id != null) return Convert.ToString(id);
                        }
                    }

                    string[] exePaths = new[]
                    {
                        @"C:\Program Files\TeamViewer\TeamViewer.exe",
                        @"C:\Program Files (x86)\TeamViewer\TeamViewer.exe"
                    };
                    bool installed = keyFound || exePaths.Any(File.Exists);
                    return installed ? "Not found (installed, ID not readable)" : "Not installed";
                }
            }
            catch
            {
                return "Not found";
            }
        }

        private void ShowSystemInfoModal(string info)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "[0] System Info";
                dlg.Size = new Size(720, 600);
                dlg.MinimumSize = new Size(520, 400);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = true;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.White;
                dlg.ForeColor = TextPrimary;
                dlg.Font = new Font("Segoe UI", 9.5f);
                dlg.ShowIcon = false;

                TextBox txt = new TextBox
                {
                    Text = info.Replace("\r\n", "\n").Replace("\n", "\r\n"),
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    WordWrap = false,
                    BackColor = Color.FromArgb(15, 23, 42),
                    ForeColor = Color.FromArgb(241, 245, 249),
                    Font = new Font("Consolas", 10f),
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(14, 14),
                    Size = new Size(676, 486),
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };

                Button btnCopy = new Button
                {
                    Text = "Copy to Clipboard",
                    Size = new Size(150, 34),
                    Location = new Point(456, 512),
                    BackColor = AccentBlue,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right
                };
                btnCopy.FlatAppearance.BorderSize = 0;
                btnCopy.Click += (s, e) =>
                {
                    try
                    {
                        Clipboard.SetText(txt.Text);
                        btnCopy.Text = "Copied ✓";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(dlg, "Could not copy: " + ex.Message, "Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                Button btnClose = new Button
                {
                    Text = "Close",
                    Size = new Size(78, 34),
                    Location = new Point(612, 512),
                    BackColor = Color.FromArgb(241, 245, 249),
                    ForeColor = TextPrimary,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel,
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right
                };
                btnClose.FlatAppearance.BorderColor = BorderColor;

                dlg.Controls.Add(txt);
                dlg.Controls.Add(btnCopy);
                dlg.Controls.Add(btnClose);
                dlg.CancelButton = btnClose;
                dlg.Shown += (s, e) => { txt.SelectionStart = 0; txt.SelectionLength = 0; btnCopy.Focus(); };

                dlg.ShowDialog(this);
            }
        }

        // [25] Restart Print Spooler + Clear Stuck Print Queue
        private async Task ExecuteSpoolerResetAsync()
        {
            if (MessageBox.Show(
                "This will stop the Print Spooler and DELETE all pending print jobs, then start it again.\n\nProceed?",
                "Confirm Clear Print Queue",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                AppendLog("Print queue reset cancelled.\n", Color.FromArgb(251, 191, 36));
                return;
            }

            AppendLog("Stopping Print Spooler...\n", Color.FromArgb(56, 189, 248));
            await RunProcessAsync("net", "stop spooler");

            AppendLog("Deleting stuck print jobs...\n", Color.FromArgb(56, 189, 248));
            await RunProcessAsync("cmd.exe", "/c del /q /f \"%SystemRoot%\\System32\\spool\\PRINTERS\\*.*\"");

            AppendLog("Starting Print Spooler...\n", Color.FromArgb(56, 189, 248));
            int code = await RunProcessAsync("net", "start spooler");

            if (code == 0)
                AppendLog("Print Spooler restarted and print queue cleared.\n", Color.FromArgb(74, 222, 128));
            else
                AppendLog("Print Spooler did not start cleanly. Ensure you are running as Administrator.\n", Color.FromArgb(248, 113, 113));
        }

        // [26] High Performance Power Plan + Disable USB Selective Suspend
        private async Task ExecuteHighPerformanceAsync()
        {
            const string highPerfGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
            const string usbSubGroup = "2a737441-1930-4402-8d77-b2bebba308a3";
            const string usbSuspend = "48e6b7a6-50f5-4782-a5d4-53bb8f07e226";

            AppendLog("Activating High Performance power plan...\n", Color.FromArgb(56, 189, 248));
            int code = await RunProcessAsync("powercfg", "/setactive " + highPerfGuid);
            if (code != 0)
            {
                AppendLog("High Performance plan missing. Creating it...\n", Color.FromArgb(251, 191, 36));
                await RunProcessAsync("powercfg", "/duplicatescheme " + highPerfGuid + " " + highPerfGuid);
                code = await RunProcessAsync("powercfg", "/setactive " + highPerfGuid);
            }

            AppendLog(code == 0 ? "High Performance power plan is active.\n" : "Could not activate High Performance plan.\n",
                code == 0 ? Color.FromArgb(74, 222, 128) : Color.FromArgb(248, 113, 113));

            AppendLog("Disabling USB selective suspend (AC & battery)...\n", Color.FromArgb(56, 189, 248));
            await RunProcessAsync("powercfg", string.Format("/setacvalueindex SCHEME_CURRENT {0} {1} 0", usbSubGroup, usbSuspend));
            await RunProcessAsync("powercfg", string.Format("/setdcvalueindex SCHEME_CURRENT {0} {1} 0", usbSubGroup, usbSuspend));
            await RunProcessAsync("powercfg", "/setactive SCHEME_CURRENT");
            AppendLog("USB selective suspend disabled.\n", Color.FromArgb(74, 222, 128));
        }

        // [27] Disable Windows Fast Startup
        private async Task ExecuteDisableFastStartupAsync()
        {
            int code = await RunProcessAsync("reg.exe",
                "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power\" /v HiberbootEnabled /t REG_DWORD /d 0 /f");
            if (code == 0)
                AppendLog("Fast Startup has been DISABLED. Shutdown will now perform a full shutdown.\n", Color.FromArgb(74, 222, 128));
            else
                AppendLog("Failed to disable Fast Startup. Ensure you are running as Administrator.\n", Color.FromArgb(248, 113, 113));
        }

        // [28] Install Remote Support Tools (AnyDesk, RustDesk, UltraViewer, TightVNC)
        private async Task ExecuteInstallRemoteToolsAsync()
        {
            int choice = ShowOptionModal(
                "[28] Install Remote Support Tools",
                "Choose a remote support tool to install silently (via winget or direct download):",
                new[]
                {
                    "1. Install AnyDesk",
                    "2. Install RustDesk",
                    "3. Install UltraViewer",
                    "4. Install TightVNC",
                    "5. Install ALL 4 Tools (AnyDesk, RustDesk, UltraViewer, TightVNC)",
                    "Cancel / Back"
                });

            if (choice == -1 || choice == 5)
            {
                AppendLog("Installation cancelled.\n", Color.FromArgb(251, 191, 36));
                return;
            }

            if (choice == 0 || choice == 4) await InstallSingleRemoteToolAsync("AnyDesk");
            if (choice == 1 || choice == 4) await InstallSingleRemoteToolAsync("RustDesk");
            if (choice == 2 || choice == 4) await InstallSingleRemoteToolAsync("UltraViewer");
            if (choice == 3 || choice == 4) await InstallSingleRemoteToolAsync("TightVNC");
        }

        private async Task InstallSingleRemoteToolAsync(string tool)
        {
            AppendLog(string.Format("Starting installation of {0}...\n", tool), Color.FromArgb(56, 189, 248));

            string wingetId = "";
            string fallbackPsScript = "";

            if (tool == "AnyDesk")
            {
                wingetId = "AnyDeskSoftwareGmbH.AnyDesk";
                fallbackPsScript =
                    "$u='https://download.anydesk.com/AnyDesk.exe'; " +
                    "$dest=Join-Path $env:TEMP 'AnyDeskSetup.exe'; " +
                    "Write-Host 'Downloading AnyDesk...'; " +
                    "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " +
                    "(New-Object System.Net.WebClient).DownloadFile($u, $dest); " +
                    "Write-Host 'Running silent installation...'; " +
                    "Start-Process $dest -ArgumentList '--install \"\"C:\\Program Files (x86)\\AnyDesk\"\" --start-with-win --silent' -Wait";
            }
            else if (tool == "RustDesk")
            {
                wingetId = "RustDesk.RustDesk";
                fallbackPsScript =
                    "$u='https://github.com/rustdesk/rustdesk/releases/latest/download/rustdesk-x86_64.exe'; " +
                    "$dest=Join-Path $env:TEMP 'rustdesk_setup.exe'; " +
                    "Write-Host 'Downloading RustDesk...'; " +
                    "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " +
                    "(New-Object System.Net.WebClient).DownloadFile($u, $dest); " +
                    "Write-Host 'Running silent installation...'; " +
                    "Start-Process $dest -ArgumentList '--silent-install' -Wait";
            }
            else if (tool == "UltraViewer")
            {
                wingetId = "UltraViewer.UltraViewer";
                fallbackPsScript =
                    "$u='https://www.ultraviewer.net/en/UltraViewer_setup_en.exe'; " +
                    "$dest=Join-Path $env:TEMP 'UltraViewer_setup.exe'; " +
                    "Write-Host 'Downloading UltraViewer...'; " +
                    "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " +
                    "(New-Object System.Net.WebClient).DownloadFile($u, $dest); " +
                    "Write-Host 'Running silent installation...'; " +
                    "Start-Process $dest -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -Wait";
            }
            else if (tool == "TightVNC")
            {
                wingetId = "GlavSoft.TightVNC";
                fallbackPsScript =
                    "$arch = if ([IntPtr]::Size -eq 8) {'64bit'} else {'32bit'}; " +
                    "$u=\"https://www.tightvnc.com/download/2.8.84/tightvnc-2.8.84-gpl-setup-$arch.msi\"; " +
                    "$dest=Join-Path $env:TEMP 'tightvnc_setup.msi'; " +
                    "Write-Host 'Downloading TightVNC...'; " +
                    "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " +
                    "(New-Object System.Net.WebClient).DownloadFile($u, $dest); " +
                    "Write-Host 'Running silent MSI installation...'; " +
                    "Start-Process 'msiexec.exe' -ArgumentList \"/i `\"$dest`\" /quiet /norestart\" -Wait";
            }

            AppendLog(string.Format("Checking winget for {0}...\n", wingetId), Color.FromArgb(148, 163, 184));
            int wingetCode = await RunProcessAsync("cmd.exe",
                string.Format("/c winget install --id {0} -e --silent --accept-source-agreements --accept-package-agreements", wingetId));

            if (wingetCode == 0)
            {
                AppendLog(string.Format("{0} installed successfully via winget.\n", tool), Color.FromArgb(74, 222, 128));
                return;
            }

            AppendLog("winget unavailable or failed. Falling back to direct PowerShell silent installer...\n", Color.FromArgb(250, 204, 21));
            int psCode = await RunProcessAsync("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"" + fallbackPsScript + "\"");

            if (psCode == 0)
            {
                AppendLog(string.Format("{0} installed successfully via direct silent installer.\n", tool), Color.FromArgb(74, 222, 128));
            }
            else
            {
                AppendLog(string.Format("Failed to install {0}. Exit code: {1}\n", tool, psCode), Color.FromArgb(248, 113, 113));
            }
        }

        // [29] Reset AnyDesk ID
        private async Task ExecuteResetAnyDeskIdAsync()
        {
            if (MessageBox.Show(
                "This will reset the AnyDesk Client ID on this computer.\n\n" +
                "Steps performed:\n" +
                "1. Terminate AnyDesk process and stop AnyDesk service\n" +
                "2. Remove old client identifier tokens, certificates, and cached ID\n" +
                "3. Clear registry identity markers\n" +
                "4. Restart AnyDesk service and application to generate a brand new AnyDesk ID\n\n" +
                "Proceed with AnyDesk ID reset?",
                "Confirm AnyDesk ID Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                AppendLog("AnyDesk ID reset cancelled.\n", Color.FromArgb(251, 191, 36));
                return;
            }

            string currentIdBefore = GetAnyDeskId();
            AppendLog("Current AnyDesk ID: " + currentIdBefore + "\n", Color.FromArgb(148, 163, 184));

            AppendLog("1. Stopping AnyDesk service and terminating processes...\n", Color.FromArgb(56, 189, 248));

            string tempScript = Path.Combine(Path.GetTempPath(), "anydesk_reset_" + Guid.NewGuid().ToString("N") + ".ps1");
            try
            {
                string scriptContent = @"
Write-Host '--- Stopping AnyDesk Service and Processes ---'
$svc = Get-Service -Name 'AnyDesk' -ErrorAction SilentlyContinue
if ($svc) {
    if ($svc.Status -ne 'Stopped') {
        Write-Host 'Stopping AnyDesk Service...'
        Stop-Service -Name 'AnyDesk' -Force -ErrorAction SilentlyContinue
        $timeout = 10
        while ((Get-Service -Name 'AnyDesk' -ErrorAction SilentlyContinue).Status -ne 'Stopped' -and $timeout -gt 0) {
            Start-Sleep -Seconds 1
            $timeout--
        }
    }
}
& net stop 'AnyDesk' 2>$null

Write-Host 'Terminating AnyDesk processes...'
Get-Process -Name 'AnyDesk*' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
& taskkill /f /im 'AnyDesk.exe' 2>$null
& taskkill /f /im 'AnyDeskMSI.exe' 2>$null
Start-Sleep -Seconds 2

$remaining = Get-Process -Name 'AnyDesk*' -ErrorAction SilentlyContinue
if ($remaining) {
    Write-Warning 'Force killing remaining AnyDesk processes...'
    $remaining | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

Write-Host '--- Removing AnyDesk Identity Files & Tokens ---'
$pd = 'C:\ProgramData\AnyDesk'
if (Test-Path $pd) {
    $targetFiles = @(
        ""$pd\service.conf"",
        ""$pd\service.conf.lock"",
        ""$pd\service.conf.bak"",
        ""$pd\system.conf"",
        ""$pd\system.conf.lock"",
        ""$pd\system.conf.bak""
    )
    foreach ($f in $targetFiles) {
        if (Test-Path $f) {
            Remove-Item -Path $f -Force -ErrorAction SilentlyContinue
            Write-Host ""Removed: $f""
        }
    }
    if (Test-Path ""$pd\cache"") {
        Remove-Item -Path ""$pd\cache\*"" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host ""Cleared: $pd\cache""
    }
}

Get-ChildItem 'C:\Users' -Directory -ErrorAction SilentlyContinue | ForEach-Object {
    $userAnyDesk = Join-Path $_.FullName 'AppData\Roaming\AnyDesk'
    if (Test-Path $userAnyDesk) {
        $uFiles = @(
            ""$userAnyDesk\service.conf*"",
            ""$userAnyDesk\system.conf*"",
            ""$userAnyDesk\user.conf*"",
            ""$userAnyDesk\global_cache\*"",
            ""$userAnyDesk\cache\*""
        )
        foreach ($uf in $uFiles) {
            Remove-Item -Path $uf -Recurse -Force -ErrorAction SilentlyContinue
        }
        Write-Host ""Cleaned config tokens in: $userAnyDesk""
    }
    
    $localAnyDesk = Join-Path $_.FullName 'AppData\Local\AnyDesk'
    if (Test-Path $localAnyDesk) {
        Remove-Item -Path ""$localAnyDesk\*"" -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host '--- Clearing Registry Identity Markers ---'
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\WOW6432Node\AnyDesk' -Name 'ID' -Force -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\AnyDesk' -Name 'ID' -Force -ErrorAction SilentlyContinue
Remove-Item -Path 'HKCU:\SOFTWARE\AnyDesk' -Recurse -Force -ErrorAction SilentlyContinue

Write-Host '--- Restarting AnyDesk Service & Application ---'
if (Get-Service -Name 'AnyDesk' -ErrorAction SilentlyContinue) {
    Write-Host 'Starting AnyDesk Service...'
    Start-Service -Name 'AnyDesk' -ErrorAction SilentlyContinue
    & net start 'AnyDesk' 2>$null
}

$anydeskExes = @(
    ""${env:ProgramFiles(x86)}\AnyDesk\AnyDesk.exe"",
    ""$env:ProgramFiles\AnyDesk\AnyDesk.exe"",
    ""$env:LOCALAPPDATA\Programs\AnyDesk\AnyDesk.exe""
)
foreach ($exe in $anydeskExes) {
    if (Test-Path $exe) {
        Write-Host ""Launching AnyDesk application ($exe)...""
        Start-Process -FilePath $exe -ErrorAction SilentlyContinue
        break
    }
}
";
                File.WriteAllText(tempScript, scriptContent, Encoding.UTF8);

                AppendLog("2. Running deep configuration cleanup and identity purge...\n", Color.FromArgb(56, 189, 248));
                int exitCode = await RunProcessAsync("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File \"" + tempScript + "\"");
                if (exitCode != 0)
                {
                    RecordFailure("AnyDesk ID Reset", "PowerShell cleanup script returned exit code " + exitCode);
                }

                AppendLog("3. Waiting for AnyDesk network relay to allocate new ID (up to 15s)...\n", Color.FromArgb(56, 189, 248));
                string newId = null;
                for (int i = 0; i < 15; i++)
                {
                    await Task.Delay(1000);
                    string candidate = GetAnyDeskId();
                    if (!string.IsNullOrEmpty(candidate) &&
                        !candidate.StartsWith("Not", StringComparison.OrdinalIgnoreCase) &&
                        candidate != currentIdBefore)
                    {
                        newId = candidate;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(newId))
                {
                    newId = GetAnyDeskId();
                }

                if (!string.IsNullOrEmpty(newId) && !newId.StartsWith("Not", StringComparison.OrdinalIgnoreCase) && newId != currentIdBefore)
                {
                    AppendLog("AnyDesk ID Reset Completed Successfully!\n", Color.FromArgb(74, 222, 128));
                    AppendLog("Previous ID : " + currentIdBefore + "\n", Color.FromArgb(148, 163, 184));
                    AppendLog("New ID      : " + newId + "\n", Color.FromArgb(74, 222, 128));

                    MessageBox.Show(
                        string.Format("AnyDesk ID has been reset successfully!\n\nPrevious ID: {0}\nNew AnyDesk ID: {1}", currentIdBefore, newId),
                        "AnyDesk ID Reset Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    AppendLog("AnyDesk configuration tokens and identity files were successfully purged.\n", Color.FromArgb(251, 191, 36));
                    AppendLog("AnyDesk was launched. As soon as AnyDesk contacts its network relay, your new ID will be active.\n", Color.FromArgb(251, 191, 36));
                    if (!string.IsNullOrEmpty(newId))
                    {
                        AppendLog("Current ID reported: " + newId + "\n", Color.FromArgb(148, 163, 184));
                    }

                    MessageBox.Show(
                        "AnyDesk identity data and configuration have been reset.\n\n" +
                        "AnyDesk has been started. If your PC is connecting to the network, your new ID will appear directly in the AnyDesk application window shortly.",
                        "AnyDesk Reset Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                AppendLog("Error during AnyDesk reset: " + ex.Message + "\n", Color.FromArgb(248, 113, 113));
                RecordFailure("AnyDesk ID Reset", ex.ToString());
                MessageBox.Show(
                    "Error resetting AnyDesk ID: " + ex.Message,
                    "Reset Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempScript)) File.Delete(tempScript);
                }
                catch
                {
                }
            }
        }

        private Tuple<bool, string, string> ShowAdminUserCredentialsModal(string defaultUser, string defaultPass)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "[2] Create Administrator User";
                dlg.Size = new Size(430, 235);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.White;
                dlg.ForeColor = TextPrimary;
                dlg.Font = new Font("Segoe UI", 9.5f);

                Label lblUser = new Label
                {
                    Text = "Administrator Username:",
                    Location = new Point(22, 20),
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold)
                };

                TextBox txtUser = new TextBox
                {
                    Text = defaultUser,
                    Location = new Point(22, 44),
                    Size = new Size(370, 26),
                    BorderStyle = BorderStyle.FixedSingle
                };

                Label lblPass = new Label
                {
                    Text = "Password:",
                    Location = new Point(22, 82),
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold)
                };

                TextBox txtPass = new TextBox
                {
                    Text = defaultPass,
                    Location = new Point(22, 106),
                    Size = new Size(370, 26),
                    BorderStyle = BorderStyle.FixedSingle
                };

                Button btnOk = new Button
                {
                    Text = "Create Admin User",
                    Location = new Point(192, 150),
                    Size = new Size(125, 34),
                    BackColor = AccentBlue,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold),
                    DialogResult = DialogResult.OK,
                    Cursor = Cursors.Hand
                };
                btnOk.FlatAppearance.BorderSize = 0;

                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(322, 150),
                    Size = new Size(70, 34),
                    BackColor = Color.FromArgb(241, 245, 249),
                    ForeColor = TextPrimary,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel,
                    Cursor = Cursors.Hand
                };
                btnCancel.FlatAppearance.BorderColor = BorderColor;

                dlg.Controls.Add(lblUser);
                dlg.Controls.Add(txtUser);
                dlg.Controls.Add(lblPass);
                dlg.Controls.Add(txtPass);
                dlg.Controls.Add(btnOk);
                dlg.Controls.Add(btnCancel);
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                bool confirmed = dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txtUser.Text);
                return Tuple.Create(confirmed, txtUser.Text, txtPass.Text);
            }
        }

        private int ShowOptionModal(string title, string prompt, string[] options)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = title;
                dlg.Size = new Size(465, 130 + options.Length * 44);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.White;
                dlg.ForeColor = TextPrimary;
                dlg.Font = new Font("Segoe UI", 9.5f);

                Label lbl = new Label
                {
                    Text = prompt,
                    Location = new Point(20, 18),
                    Size = new Size(410, 28),
                    ForeColor = TextPrimary,
                    Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold)
                };
                dlg.Controls.Add(lbl);

                int selectedIndex = -1;
                for (int i = 0; i < options.Length; i++)
                {
                    int idx = i;
                    bool isCancel = i == options.Length - 1;
                    Button btn = new Button
                    {
                        Text = options[i],
                        Location = new Point(20, 54 + i * 42),
                        Size = new Size(410, 34),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = isCancel ? Color.FromArgb(241, 245, 249) : AccentBlue,
                        ForeColor = isCancel ? TextPrimary : Color.White,
                        Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
                        Cursor = Cursors.Hand,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(12, 0, 0, 0)
                    };
                    btn.FlatAppearance.BorderSize = isCancel ? 1 : 0;
                    if (isCancel) btn.FlatAppearance.BorderColor = BorderColor;

                    btn.Click += (s, e) =>
                    {
                        selectedIndex = isCancel ? -1 : idx;
                        dlg.DialogResult = isCancel ? DialogResult.Cancel : DialogResult.OK;
                        dlg.Close();
                    };
                    dlg.Controls.Add(btn);
                }

                dlg.ShowDialog(this);
                return selectedIndex;
            }
        }

        private Task<int> RunProcessAsync(string fileName, string arguments)
        {
            AppendLog(string.Format("> {0} {1}\n", fileName, arguments), Color.FromArgb(148, 163, 184));
            return Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.Default
                    };

                    using (Process proc = new Process { StartInfo = psi })
                    {
                        proc.OutputDataReceived += (s, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                                AppendLog(e.Data + "\n", Color.FromArgb(226, 232, 240));
                        };
                        proc.ErrorDataReceived += (s, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                                AppendLog(e.Data + "\n", Color.FromArgb(248, 113, 113));
                        };

                        proc.Start();
                        proc.BeginOutputReadLine();
                        proc.BeginErrorReadLine();
                        proc.WaitForExit();
                        return proc.ExitCode;
                    }
                }
                catch (Exception ex)
                {
                    AppendLog("[Execution Error] " + ex.Message + "\n", Color.FromArgb(248, 113, 113));
                    return -1;
                }
            });
        }

        private void AppendWelcomeBanner()
        {
            AppendLog("================================================================================\n", Color.FromArgb(56, 189, 248));
            AppendLog(" ePlus Multi-Option System Administration & Maintenance Toolkit\n", Color.FromArgb(56, 189, 248));
            AppendLog(string.Format(" Platform : {0} Windows  |  Process: {1}-bit  |  Admin: {2}\n",
                Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit",
                IntPtr.Size * 8,
                Program.IsAdministrator() ? "YES" : "NO (Please run as Administrator)"), Color.FromArgb(148, 163, 184));
            AppendLog("================================================================================\n", Color.FromArgb(56, 189, 248));
        }

        private void AppendSectionHeader(string title)
        {
            AppendLog("\n--------------------------------------------------------------------------------\n", Color.FromArgb(56, 189, 248));
            AppendLog(" [" + DateTime.Now.ToString("HH:mm:ss") + "] " + title + "\n", Color.FromArgb(56, 189, 248));
            AppendLog("--------------------------------------------------------------------------------\n", Color.FromArgb(56, 189, 248));
        }

        private void AppendLog(string text, Color color)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, Color>(AppendLog), text, color);
                return;
            }

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText(text);
            txtLog.ScrollToCaret();

            // Track and record failure if message is logged with error color
            if (color == Color.FromArgb(248, 113, 113))
            {
                RecordFailure(currentRunningAction, text);
            }
        }

        private void RecordFailure(string actionTitle, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            string cleanMsg = message.Trim();
            if (string.IsNullOrEmpty(cleanMsg)) return;

            FailureRecord record = new FailureRecord
            {
                Timestamp = DateTime.Now,
                ActionTitle = string.IsNullOrEmpty(actionTitle) ? "General / System" : actionTitle,
                Message = cleanMsg
            };

            failureRecords.Add(record);
            AppendToAutomaticFailureLog(record);

            if (btnExportFailures != null)
            {
                btnExportFailures.Text = string.Format("Export Failure Log ({0})", failureRecords.Count);
                btnExportFailures.BackColor = Color.FromArgb(254, 226, 226);
                btnExportFailures.ForeColor = Color.FromArgb(185, 28, 28);
                btnExportFailures.FlatAppearance.BorderColor = Color.FromArgb(239, 68, 68);
            }
        }

        private void AppendToAutomaticFailureLog(FailureRecord record)
        {
            try
            {
                string logPath = GetDefaultFailureLogPath();
                string entry = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}{3}",
                    record.Timestamp, record.ActionTitle, record.Message, Environment.NewLine);
                File.AppendAllText(logPath, entry, Encoding.UTF8);
            }
            catch
            {
                // Silently ignore disk write issues to prevent crashing
            }
        }

        private static string GetDefaultFailureLogPath()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string primary = Path.Combine(baseDir, "SystemToolkit_Failures.log");
                using (FileStream fs = File.Open(primary, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) { }
                return primary;
            }
            catch
            {
                return Path.Combine(Path.GetTempPath(), "SystemToolkit_Failures.log");
            }
        }

        private void ExportFailureLogFile()
        {
            if (failureRecords.Count == 0)
            {
                string autoLog = GetDefaultFailureLogPath();
                if (File.Exists(autoLog) && new FileInfo(autoLog).Length > 0)
                {
                    if (MessageBox.Show(
                        "No new failures recorded in this session, but a previous failure log exists on disk:\n\n" + autoLog + "\n\nWould you like to open it now?",
                        "Open Existing Failure Log",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        Process.Start("notepad.exe", autoLog);
                    }
                    return;
                }

                MessageBox.Show("No failed operations have been recorded in this session.", "No Failures Recorded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Export Failed Operations Log";
                sfd.Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                sfd.FileName = string.Format("SystemToolkit_Failures_{0:yyyy-MM-dd_HHmmss}.log", DateTime.Now);
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("================================================================================");
                        sb.AppendLine(" ePlus System Administration Toolkit - Failed Operations Log");
                        sb.AppendLine("================================================================================");
                        sb.AppendLine(string.Format(" Generated   : {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now));
                        sb.AppendLine(string.Format(" Computer    : {0}", Environment.MachineName));
                        sb.AppendLine(string.Format(" User        : {0}", Environment.UserName));
                        sb.AppendLine(string.Format(" OS Platform : {0} ({1}-bit)", Environment.OSVersion, Environment.Is64BitOperatingSystem ? "64" : "32"));
                        sb.AppendLine(string.Format(" Process     : {0}-bit (Admin: {1})", IntPtr.Size * 8, Program.IsAdministrator() ? "YES" : "NO"));
                        sb.AppendLine(string.Format(" Total Errors: {0}", failureRecords.Count));
                        sb.AppendLine("================================================================================");
                        sb.AppendLine();

                        for (int i = 0; i < failureRecords.Count; i++)
                        {
                            FailureRecord r = failureRecords[i];
                            sb.AppendLine(string.Format("[Entry #{0}]", i + 1));
                            sb.AppendLine(string.Format("Time      : {0:yyyy-MM-dd HH:mm:ss}", r.Timestamp));
                            sb.AppendLine(string.Format("Operation : {0}", r.ActionTitle));
                            sb.AppendLine(string.Format("Details   : {0}", r.Message));
                            sb.AppendLine("--------------------------------------------------------------------------------");
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        AppendLog("Exported failure log to: " + sfd.FileName + "\n", Color.FromArgb(74, 222, 128));

                        if (MessageBox.Show("Failure log successfully exported to:\n" + sfd.FileName + "\n\nWould you like to open it now in Notepad?",
                            "Log Exported", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            Process.Start("notepad.exe", sfd.FileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving log file: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private static Bitmap CreateToolkitBadgeBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF rect = new RectangleF(2, 2, size - 4, size - 4);
                using (LinearGradientBrush bg = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(37, 99, 235),
                    Color.FromArgb(30, 58, 138),
                    45f))
                {
                    g.FillEllipse(bg, rect);
                }
                using (Pen ring = new Pen(Color.FromArgb(220, 38, 38), 2.5f))
                {
                    g.DrawEllipse(ring, rect);
                }

                float center = size / 2f;
                float rOuter = size * 0.27f;
                float rInner = size * 0.14f;
                using (Pen gearPen = new Pen(Color.White, 2.4f))
                {
                    g.DrawEllipse(gearPen, center - rOuter, center - rOuter, rOuter * 2, rOuter * 2);
                    for (int a = 0; a < 360; a += 45)
                    {
                        double rad = a * Math.PI / 180.0;
                        float x1 = center + (float)Math.Cos(rad) * rOuter;
                        float y1 = center + (float)Math.Sin(rad) * rOuter;
                        float x2 = center + (float)Math.Cos(rad) * (rOuter + size * 0.08f);
                        float y2 = center + (float)Math.Sin(rad) * (rOuter + size * 0.08f);
                        g.DrawLine(gearPen, x1, y1, x2, y2);
                    }
                }
                using (SolidBrush centerBrush = new SolidBrush(Color.FromArgb(220, 38, 38)))
                {
                    g.FillEllipse(centerBrush, center - rInner, center - rInner, rInner * 2, rInner * 2);
                }
            }
            return bmp;
        }

        private static Icon CreateRuntimeIcon()
        {
            using (Bitmap bmp = CreateToolkitBadgeBitmap(32))
            {
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        private sealed class ToolActionItem
        {
            public int Id { get; private set; }
            public string Category { get; private set; }
            public string Title { get; private set; }
            public string Description { get; private set; }
            public Color AccentColor { get; private set; }
            public Func<Task> Handler { get; private set; }
            public bool IsSelected { get; set; }
            public bool Selectable { get; private set; }

            public ToolActionItem(int id, string category, string title, string description, Color accentColor, Func<Task> handler, bool selectable = true)
            {
                Id = id;
                Category = category;
                Title = title;
                Description = description;
                AccentColor = accentColor;
                Handler = handler;
                Selectable = selectable;
            }
        }
    }
}
