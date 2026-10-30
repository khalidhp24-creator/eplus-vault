using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyTitle("Windows Network Reset Utility")]
[assembly: AssemblyDescription("GUI utility to reset Winsock, TCP/IP stack, flush DNS cache, and manage system reboot on 32-bit & 64-bit Windows.")]
[assembly: AssemblyCompany("Network Utilities")]
[assembly: AssemblyProduct("NetworkResetTool")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace NetworkResetUtility
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
                    // Continue and display warning banner if user cancels UAC prompt
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
        private CheckBox chkWinsock;
        private CheckBox chkTcpIp;
        private CheckBox chkFlushDns;
        private CheckBox chkRestart;
        private NumericUpDown numRestartSeconds;

        private Button btnRunSelected;
        private Button btnOnlyWinsock;
        private Button btnOnlyTcpIp;
        private Button btnOnlyDns;
        private Button btnAbortRestart;
        private Button btnClearLog;

        private ProgressBar progressBar;
        private Label lblStatus;
        private RichTextBox txtLog;
        private Timer countdownTimer;

        private bool isBusy;
        private int remainingRestartSeconds;

        public MainForm()
        {
            InitializeUi();
            AppendBanner();
        }

        private void InitializeUi()
        {
            Text = string.Format(
                "Windows Network Reset Utility ({0}-bit Mode)",
                IntPtr.Size == 8 ? "64" : "32");
            Size = new Size(760, 620);
            MinimumSize = new Size(720, 580);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(15, 23, 42);
            ForeColor = Color.FromArgb(241, 245, 249);
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                Icon = CreateRuntimeIcon();
            }

            // Header Panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(18, 12, 18, 12)
            };

            PictureBox picIcon = new PictureBox
            {
                Image = CreateHeaderBadgeBitmap(48),
                Size = new Size(48, 48),
                Location = new Point(18, 14),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            Label lblTitle = new Label
            {
                Text = "Windows Network Configuration Reset",
                Font = new Font("Segoe UI Semibold", 14.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(78, 12)
            };

            string adminStatus = Program.IsAdministrator() ? "Administrator" : "Standard User (Elevation Required)";
            string osArch = Environment.Is64BitOperatingSystem ? "64-bit OS" : "32-bit OS";
            string procArch = IntPtr.Size == 8 ? "64-bit Process" : "32-bit Process";

            Label lblSubtitle = new Label
            {
                Text = string.Format("Winsock  |  TCP/IP Stack  |  DNS Resolver Cache    [{0} • {1} • {2}]", osArch, procArch, adminStatus),
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                ForeColor = Program.IsAdministrator() ? Color.FromArgb(56, 189, 248) : Color.FromArgb(251, 191, 36),
                AutoSize = true,
                Location = new Point(80, 42)
            };

            headerPanel.Controls.Add(picIcon);
            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);

            // Main Container
            Panel bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 14)
            };

            // GroupBox: Operations
            GroupBox grpTasks = new GroupBox
            {
                Text = " Network Reset Operations ",
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.FromArgb(30, 41, 59),
                Location = new Point(18, 10),
                Size = new Size(706, 192),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            chkWinsock = CreateTaskCheckBox(
                "1. Reset Winsock Catalog  (netsh winsock reset)",
                new Point(18, 28),
                true);

            btnOnlyWinsock = CreateSmallActionButton("Run Step 1", new Point(560, 24));
            btnOnlyWinsock.Click += async (s, e) => await RunSingleOperationAsync(
                "Resetting Winsock Catalog", "netsh", "winsock reset");

            chkTcpIp = CreateTaskCheckBox(
                "2. Reset TCP/IP Stack  (netsh int ip reset)",
                new Point(18, 66),
                true);

            btnOnlyTcpIp = CreateSmallActionButton("Run Step 2", new Point(560, 62));
            btnOnlyTcpIp.Click += async (s, e) => await RunSingleOperationAsync(
                "Resetting TCP/IP Stack", "netsh", "int ip reset");

            chkFlushDns = CreateTaskCheckBox(
                "3. Flush DNS Resolver Cache  (ipconfig /flushdns)",
                new Point(18, 104),
                true);

            btnOnlyDns = CreateSmallActionButton("Run Step 3", new Point(560, 100));
            btnOnlyDns.Click += async (s, e) => await RunSingleOperationAsync(
                "Flushing DNS Cache", "ipconfig", "/flushdns");

            chkRestart = CreateTaskCheckBox(
                "4. Automatically restart computer after full reset",
                new Point(18, 144),
                true);
            chkRestart.ForeColor = Color.FromArgb(253, 224, 71);

            Label lblDelay = new Label
            {
                Text = "Delay (sec):",
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = true,
                Location = new Point(435, 147)
            };

            numRestartSeconds = new NumericUpDown
            {
                Minimum = 5,
                Maximum = 3600,
                Value = 15,
                Location = new Point(518, 144),
                Size = new Size(64, 24),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White
            };

            grpTasks.Controls.Add(chkWinsock);
            grpTasks.Controls.Add(btnOnlyWinsock);
            grpTasks.Controls.Add(chkTcpIp);
            grpTasks.Controls.Add(btnOnlyTcpIp);
            grpTasks.Controls.Add(chkFlushDns);
            grpTasks.Controls.Add(btnOnlyDns);
            grpTasks.Controls.Add(chkRestart);
            grpTasks.Controls.Add(lblDelay);
            grpTasks.Controls.Add(numRestartSeconds);

            // Action Buttons Bar
            btnRunSelected = new Button
            {
                Text = "Run Selected Network Reset Steps",
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(2, 132, 199),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(18, 214),
                Size = new Size(340, 42),
                Cursor = Cursors.Hand
            };
            btnRunSelected.FlatAppearance.BorderSize = 0;
            btnRunSelected.Click += async (s, e) => await ExecuteSelectedWorkflowAsync();

            btnAbortRestart = new Button
            {
                Text = "Abort Pending Restart (shutdown /a)",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(185, 28, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(370, 214),
                Size = new Size(235, 42),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnAbortRestart.FlatAppearance.BorderSize = 0;
            btnAbortRestart.Click += async (s, e) => await AbortScheduledRestartAsync();

            btnClearLog = new Button
            {
                Text = "Clear Log",
                Font = new Font("Segoe UI", 9.0f, FontStyle.Regular),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(615, 214),
                Size = new Size(109, 42),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClearLog.FlatAppearance.BorderSize = 0;
            btnClearLog.Click += (s, e) =>
            {
                txtLog.Clear();
                AppendBanner();
            };

            // Status & Progress
            lblStatus = new Label
            {
                Text = "Ready. Select the desired network reset options and click Run.",
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = false,
                Location = new Point(18, 266),
                Size = new Size(706, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            progressBar = new ProgressBar
            {
                Location = new Point(18, 290),
                Size = new Size(706, 14),
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Console Output RichTextBox
            txtLog = new RichTextBox
            {
                Location = new Point(18, 314),
                Size = new Size(706, 172),
                BackColor = Color.FromArgb(9, 14, 26),
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Consolas", 9.5f, FontStyle.Regular),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            countdownTimer = new Timer { Interval = 1000 };
            countdownTimer.Tick += CountdownTimer_Tick;

            bodyPanel.Controls.Add(grpTasks);
            bodyPanel.Controls.Add(btnRunSelected);
            bodyPanel.Controls.Add(btnAbortRestart);
            bodyPanel.Controls.Add(btnClearLog);
            bodyPanel.Controls.Add(lblStatus);
            bodyPanel.Controls.Add(progressBar);
            bodyPanel.Controls.Add(txtLog);

            Controls.Add(bodyPanel);
            Controls.Add(headerPanel);
        }

        private CheckBox CreateTaskCheckBox(string text, Point location, bool isChecked)
        {
            return new CheckBox
            {
                Text = text,
                Checked = isChecked,
                AutoSize = true,
                Location = location,
                ForeColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 9.75f, FontStyle.Regular)
            };
        }

        private Button CreateSmallActionButton(string text, Point location)
        {
            Button btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(126, 28),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void AppendBanner()
        {
            AppendLog("=========================================\n", Color.FromArgb(56, 189, 248));
            AppendLog(" Windows Network Reset Utility Ready\n", Color.FromArgb(56, 189, 248));
            AppendLog(string.Format(" Architecture : {0} OS / {1}-bit Process\n",
                Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit",
                IntPtr.Size * 8), Color.FromArgb(148, 163, 184));
            AppendLog(string.Format(" Privilege    : {0}\n",
                Program.IsAdministrator() ? "Administrator" : "Standard User (Run as Admin for netsh)"),
                Program.IsAdministrator() ? Color.FromArgb(74, 222, 128) : Color.FromArgb(251, 191, 36));
            AppendLog("=========================================\n\n", Color.FromArgb(56, 189, 248));
        }

        private void SetBusyState(bool busy, string statusMessage)
        {
            isBusy = busy;
            btnRunSelected.Enabled = !busy;
            btnOnlyWinsock.Enabled = !busy;
            btnOnlyTcpIp.Enabled = !busy;
            btnOnlyDns.Enabled = !busy;
            chkWinsock.Enabled = !busy;
            chkTcpIp.Enabled = !busy;
            chkFlushDns.Enabled = !busy;
            chkRestart.Enabled = !busy;
            numRestartSeconds.Enabled = !busy;
            lblStatus.Text = statusMessage;
        }

        private async Task RunSingleOperationAsync(string title, string fileName, string arguments)
        {
            if (isBusy) return;

            SetBusyState(true, title + "...");
            progressBar.Value = 25;

            AppendLog(string.Format(">>> {0} ({1} {2})\n", title, fileName, arguments), Color.FromArgb(56, 189, 248));
            CommandResult result = await RunCommandAsync(fileName, arguments);
            LogCommandResult(result);

            progressBar.Value = 100;
            SetBusyState(false, string.Format("{0} completed (Exit Code: {1}).", title, result.ExitCode));
        }

        private async Task ExecuteSelectedWorkflowAsync()
        {
            if (isBusy) return;

            var steps = new List<Tuple<string, string, string>>();
            if (chkWinsock.Checked)
                steps.Add(Tuple.Create("1. Resetting Winsock...", "netsh", "winsock reset"));
            if (chkTcpIp.Checked)
                steps.Add(Tuple.Create("2. Resetting TCP/IP Stack...", "netsh", "int ip reset"));
            if (chkFlushDns.Checked)
                steps.Add(Tuple.Create("3. Flushing DNS Cache...", "ipconfig", "/flushdns"));

            if (steps.Count == 0 && !chkRestart.Checked)
            {
                MessageBox.Show(
                    "Please select at least one operation to run.",
                    "No Operation Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SetBusyState(true, "Resetting Network Configuration...");
            progressBar.Value = 0;

            AppendLog("=========================================\n", Color.FromArgb(56, 189, 248));
            AppendLog("Resetting Network Configuration...\n", Color.FromArgb(56, 189, 248));
            AppendLog("=========================================\n\n", Color.FromArgb(56, 189, 248));

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                lblStatus.Text = step.Item1;
                AppendLog(step.Item1 + "\n", Color.FromArgb(250, 204, 21));

                CommandResult res = await RunCommandAsync(step.Item2, step.Item3);
                LogCommandResult(res);

                progressBar.Value = (int)(((i + 1) / (double)Math.Max(1, steps.Count)) * 100);
            }

            if (chkRestart.Checked)
            {
                int delaySeconds = (int)numRestartSeconds.Value;
                DialogResult confirm = MessageBox.Show(
                    string.Format("Network reset steps finished.\n\nSchedule computer restart in {0} seconds?", delaySeconds),
                    "Confirm System Restart",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    AppendLog("\n=========================================\n", Color.FromArgb(74, 222, 128));
                    AppendLog(string.Format("Done! Restarting the computer in {0} seconds.\n", delaySeconds), Color.FromArgb(74, 222, 128));
                    AppendLog("=========================================\n", Color.FromArgb(74, 222, 128));

                    string shutdownArgs = string.Format("/r /t {0} /c \"Restarting to complete network reset...\"", delaySeconds);
                    CommandResult shutdownRes = await RunCommandAsync("shutdown", shutdownArgs);
                    LogCommandResult(shutdownRes);

                    if (shutdownRes.ExitCode == 0)
                    {
                        remainingRestartSeconds = delaySeconds;
                        btnAbortRestart.Enabled = true;
                        countdownTimer.Start();
                        SetBusyState(false, string.Format("Restart scheduled! Rebooting in {0} seconds...", remainingRestartSeconds));
                        return;
                    }
                }
                else
                {
                    AppendLog("\n[Info] System restart skipped by user. Please restart manually later.\n", Color.FromArgb(251, 191, 36));
                }
            }
            else
            {
                AppendLog("\n=========================================\n", Color.FromArgb(74, 222, 128));
                AppendLog("All selected network reset commands finished!\n", Color.FromArgb(74, 222, 128));
                AppendLog("=========================================\n", Color.FromArgb(74, 222, 128));
            }

            SetBusyState(false, "Network reset operations completed.");
        }

        private async Task AbortScheduledRestartAsync()
        {
            countdownTimer.Stop();
            CommandResult res = await RunCommandAsync("shutdown", "/a");
            LogCommandResult(res);
            btnAbortRestart.Enabled = false;
            lblStatus.Text = "Scheduled system restart aborted.";
            AppendLog("[Aborted] Scheduled system restart was cancelled (shutdown /a).\n\n", Color.FromArgb(251, 191, 36));
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            remainingRestartSeconds--;
            if (remainingRestartSeconds <= 0)
            {
                countdownTimer.Stop();
                lblStatus.Text = "Restarting computer now...";
            }
            else
            {
                lblStatus.Text = string.Format("Restarting computer in {0} seconds... (Click 'Abort Pending Restart' to cancel)", remainingRestartSeconds);
            }
        }

        private void LogCommandResult(CommandResult res)
        {
            if (!string.IsNullOrWhiteSpace(res.StdOut))
            {
                AppendLog(res.StdOut.TrimEnd() + "\n", Color.FromArgb(226, 232, 240));
            }
            if (!string.IsNullOrWhiteSpace(res.StdErr))
            {
                AppendLog(res.StdErr.TrimEnd() + "\n", Color.FromArgb(248, 113, 113));
            }
            AppendLog(string.Format("[Exit Code: {0}]\n\n", res.ExitCode),
                res.ExitCode == 0 ? Color.FromArgb(74, 222, 128) : Color.FromArgb(248, 113, 113));
        }

        private void AppendLog(string text, Color color)
        {
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
        }

        private static Task<CommandResult> RunCommandAsync(string fileName, string arguments)
        {
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
                        StringBuilder stdout = new StringBuilder();
                        StringBuilder stderr = new StringBuilder();

                        proc.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
                        proc.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

                        proc.Start();
                        proc.BeginOutputReadLine();
                        proc.BeginErrorReadLine();
                        proc.WaitForExit();

                        return new CommandResult
                        {
                            ExitCode = proc.ExitCode,
                            StdOut = stdout.ToString(),
                            StdErr = stderr.ToString()
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new CommandResult
                    {
                        ExitCode = -1,
                        StdOut = string.Empty,
                        StdErr = ex.Message
                    };
                }
            });
        }

        private static Bitmap CreateHeaderBadgeBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF rect = new RectangleF(2, 2, size - 4, size - 4);
                using (LinearGradientBrush bg = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(14, 116, 144),
                    Color.FromArgb(30, 58, 138),
                    45f))
                {
                    g.FillEllipse(bg, rect);
                }
                using (Pen ring = new Pen(Color.FromArgb(56, 189, 248), 2.5f))
                {
                    g.DrawEllipse(ring, rect);
                }
                float pad = size * 0.23f;
                RectangleF inner = new RectangleF(pad, pad, size - 2 * pad, size - 2 * pad);
                using (Pen grid = new Pen(Color.White, 2.0f))
                {
                    g.DrawEllipse(grid, inner);
                    g.DrawLine(grid, pad, size / 2f, size - pad, size / 2f);
                    g.DrawLine(grid, size / 2f, pad, size / 2f, size - pad);
                }
            }
            return bmp;
        }

        private static Icon CreateRuntimeIcon()
        {
            using (Bitmap bmp = CreateHeaderBadgeBitmap(32))
            {
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        private sealed class CommandResult
        {
            public int ExitCode { get; set; }
            public string StdOut { get; set; }
            public string StdErr { get; set; }
        }
    }
}
