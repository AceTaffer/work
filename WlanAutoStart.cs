using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WlanAutoStart
{
    public class MainForm : Form
    {
        private const string TaskName = "WlanAutoStart";
        private Label lblTitle;
        private Label lblAuthor;
        private GroupBox grpService;
        private Label lblSvcStatusText;
        private Button btnStartSvc;
        private Button btnStopSvc;
        private GroupBox grpAutoStart;
        private NumericUpDown nudDelay;
        private Button btnInstall;
        private Button btnUninstall;
        private GroupBox grpNetwork;
        private ListBox lstIPs;
        private ListView lvWiFi;
        private Button btnRefreshWiFi;
        private GroupBox grpSpeed;
        private Label lblDownSpeed;
        private Label lblUpLabel;
        private Button btnSpeedTest;
        private ProgressBar pbSpeed;
        private GroupBox grpLog;
        private RichTextBox rtbLog;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public MainForm()
        {
            InitializeUI();
            RefreshAll();
            AppendLog("程序已启动。by-acct");
        }

        private void InitializeUI()
        {
            Text = "WLAN 自动配置助手 v2.0";
            ClientSize = new Size(560, 750);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9f);

            int y = 8;

            lblTitle = new Label
            {
                Text = "WLAN 自动配置助手 v2.0",
                Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold),
                Location = new Point(12, y),
                Size = new Size(520, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lblTitle);
            y += 26;

            lblAuthor = new Label
            {
                Text = "by-acct",
                Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Italic),
                ForeColor = Color.DimGray,
                Location = new Point(12, y),
                Size = new Size(520, 16),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lblAuthor);
            y += 22;

            // ---- 服务控制 ----
            grpService = new GroupBox
            {
                Text = "服务控制",
                Location = new Point(12, y),
                Size = new Size(520, 65)
            };
            lblSvcStatusText = new Label
            {
                Text = "检测中...",
                Location = new Point(12, 28),
                Size = new Size(200, 23),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold)
            };
            btnStartSvc = new Button
            {
                Text = "启动 WlanSvc",
                Location = new Point(260, 24),
                Size = new Size(120, 30),
                FlatStyle = FlatStyle.System
            };
            btnStartSvc.Click += BtnStartSvc_Click;
            btnStopSvc = new Button
            {
                Text = "停止 WlanSvc",
                Location = new Point(390, 24),
                Size = new Size(120, 30),
                FlatStyle = FlatStyle.System
            };
            btnStopSvc.Click += BtnStopSvc_Click;
            grpService.Controls.Add(lblSvcStatusText);
            grpService.Controls.Add(btnStartSvc);
            grpService.Controls.Add(btnStopSvc);
            Controls.Add(grpService);
            y += 75;

            // ---- 开机自启动 ----
            grpAutoStart = new GroupBox
            {
                Text = "开机自启动",
                Location = new Point(12, y),
                Size = new Size(520, 60)
            };
            var lblDelay2 = new Label
            {
                Text = "开机延迟:",
                Location = new Point(12, 25),
                Size = new Size(65, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            nudDelay = new NumericUpDown
            {
                Location = new Point(82, 23),
                Size = new Size(55, 23),
                Minimum = 1,
                Maximum = 120,
                Value = 1
            };
            var lblHint2 = new Label
            {
                Text = "秒",
                Location = new Point(142, 25),
                Size = new Size(40, 23),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Gray
            };
            btnInstall = new Button
            {
                Text = "配置开机自启动",
                Location = new Point(240, 22),
                Size = new Size(135, 28),
                FlatStyle = FlatStyle.System
            };
            btnInstall.Click += BtnInstall_Click;
            btnUninstall = new Button
            {
                Text = "卸载",
                Location = new Point(385, 22),
                Size = new Size(125, 28),
                FlatStyle = FlatStyle.System
            };
            btnUninstall.Click += BtnUninstall_Click;
            grpAutoStart.Controls.Add(lblDelay2);
            grpAutoStart.Controls.Add(nudDelay);
            grpAutoStart.Controls.Add(lblHint2);
            grpAutoStart.Controls.Add(btnInstall);
            grpAutoStart.Controls.Add(btnUninstall);
            Controls.Add(grpAutoStart);
            y += 70;

            // ---- 网络信息 ----
            grpNetwork = new GroupBox
            {
                Text = "网络信息",
                Location = new Point(12, y),
                Size = new Size(520, 210)
            };
            var lblIPsTitle = new Label
            {
                Text = "本机 IP 地址:",
                Location = new Point(12, 22),
                Size = new Size(80, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Bold)
            };
            lstIPs = new ListBox
            {
                Location = new Point(12, 44),
                Size = new Size(496, 55),
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };
            var lblWiFiTitle = new Label
            {
                Text = "可用 Wi-Fi 网络:",
                Location = new Point(12, 105),
                Size = new Size(120, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Bold)
            };
            lvWiFi = new ListView
            {
                Location = new Point(12, 127),
                Size = new Size(400, 70),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            lvWiFi.Columns.Add("SSID", 150);
            lvWiFi.Columns.Add("信号", 80);
            lvWiFi.Columns.Add("安全", 70);
            lvWiFi.Columns.Add("状态", 90);
            btnRefreshWiFi = new Button
            {
                Text = "刷新网络",
                Location = new Point(420, 127),
                Size = new Size(88, 70),
                FlatStyle = FlatStyle.System
            };
            btnRefreshWiFi.Click += (s, e) => RefreshWiFiList();
            grpNetwork.Controls.Add(lblIPsTitle);
            grpNetwork.Controls.Add(lstIPs);
            grpNetwork.Controls.Add(lblWiFiTitle);
            grpNetwork.Controls.Add(lvWiFi);
            grpNetwork.Controls.Add(btnRefreshWiFi);
            Controls.Add(grpNetwork);
            y += 220;

            // ---- 网速测试 ----
            grpSpeed = new GroupBox
            {
                Text = "网速测试",
                Location = new Point(12, y),
                Size = new Size(520, 85)
            };
            lblDownSpeed = new Label
            {
                Text = "下载速度: -- Mbps",
                Location = new Point(12, 25),
                Size = new Size(230, 23),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold)
            };
            lblUpLabel = new Label
            {
                Text = "上传: -- (暂不支持)",
                Location = new Point(12, 50),
                Size = new Size(230, 23),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 8f),
                ForeColor = Color.Gray
            };
            pbSpeed = new ProgressBar
            {
                Location = new Point(260, 28),
                Size = new Size(150, 18),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            btnSpeedTest = new Button
            {
                Text = "开始测速",
                Location = new Point(420, 24),
                Size = new Size(90, 42),
                FlatStyle = FlatStyle.System
            };
            btnSpeedTest.Click += BtnSpeedTest_Click;
            grpSpeed.Controls.Add(lblDownSpeed);
            grpSpeed.Controls.Add(lblUpLabel);
            grpSpeed.Controls.Add(pbSpeed);
            grpSpeed.Controls.Add(btnSpeedTest);
            Controls.Add(grpSpeed);
            y += 95;

            // ---- 日志 ----
            grpLog = new GroupBox
            {
                Text = "日志",
                Location = new Point(12, y),
                Size = new Size(520, 130)
            };
            rtbLog = new RichTextBox
            {
                Location = new Point(10, 22),
                Size = new Size(500, 100),
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            grpLog.Controls.Add(rtbLog);
            Controls.Add(grpLog);
        }

        private void RefreshAll()
        {
            RefreshServiceStatus();
            RefreshLocalIPs();
            RefreshWiFiList();
        }

        // ===== 服务控制 =====
        private void RefreshServiceStatus()
        {
            try
            {
                using (var sc = new ServiceController("WlanSvc"))
                {
                    sc.Refresh();
                    bool running = sc.Status == ServiceControllerStatus.Running;
                    lblSvcStatusText.Text = running ? "● WlanSvc 正在运行" : "○ WlanSvc 已停止";
                    lblSvcStatusText.ForeColor = running ? Color.Green : Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblSvcStatusText.Text = "检测失败";
                lblSvcStatusText.ForeColor = Color.Red;
                AppendLog("[错误] 服务状态查询失败: " + ex.Message);
            }
        }

        private void BtnStartSvc_Click(object sender, EventArgs e)
        {
            try
            {
                using (var sc = new ServiceController("WlanSvc"))
                {
                    sc.Refresh();
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        AppendLog("[SKIP] WlanSvc 已经在运行中。");
                    }
                    else
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                        AppendLog("[OK] WlanSvc 已成功启动。");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog("[错误] 启动失败: " + ex.Message);
            }
            RefreshServiceStatus();
        }

        private void BtnStopSvc_Click(object sender, EventArgs e)
        {
            try
            {
                using (var sc = new ServiceController("WlanSvc"))
                {
                    sc.Refresh();
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        AppendLog("[SKIP] WlanSvc 已经处于停止状态。");
                    }
                    else
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                        AppendLog("[OK] WlanSvc 已成功停止。");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog("[错误] 停止失败: " + ex.Message);
            }
            RefreshServiceStatus();
            RefreshWiFiList();
        }

        // ===== 开机自启动 =====
        private void BtnInstall_Click(object sender, EventArgs e)
        {
            try
            {
                int delay = (int)nudDelay.Value;
                string psCommand = string.Format(
                    "$taskName='{0}';" +
                    "if(Get-ScheduledTask -TaskName $taskName -EA 0){{Unregister-ScheduledTask -TaskName $taskName -Confirm:$false}};" +
                    "$action=New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '-WindowStyle Hidden -Command \"Start-Sleep -Seconds {1}; Start-Service WlanSvc -ErrorAction SilentlyContinue\"';" +
                    "$trigger=New-ScheduledTaskTrigger -AtLogOn;" +
                    "$principal=New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest;" +
                    "$settings=New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -Compatibility Win8;" +
                    "Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force;" +
                    "Write-Host 'TASK_CREATED_OK'",
                    TaskName, delay);

                RunPsAsAdmin(psCommand, string.Format("[OK] 开机自启动已配置，延迟 {0} 秒。", delay));
            }
            catch (Exception ex)
            {
                AppendLog("[错误] 配置失败: " + ex.Message);
            }
            RefreshServiceStatus();
        }

        private void BtnUninstall_Click(object sender, EventArgs e)
        {
            try
            {
                string psCommand = string.Format(
                    "$taskName='{0}';" +
                    "if(Get-ScheduledTask -TaskName $taskName -EA 0){{Unregister-ScheduledTask -TaskName $taskName -Confirm:$false;Write-Host 'TASK_REMOVED_OK'}}else{{Write-Host 'TASK_NOT_FOUND'}}",
                    TaskName);

                RunPsAsAdmin(psCommand, "[OK] 开机自启动任务已卸载。");
            }
            catch (Exception ex)
            {
                AppendLog("[错误] 卸载失败: " + ex.Message);
            }
        }

        private void RunPsAsAdmin(string command, string successMsg)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-ExecutionPolicy Bypass -Command \"" + command + "\"",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit();
                if (proc.ExitCode == 0)
                    AppendLog(successMsg);
                else
                    AppendLog("[错误] 操作失败，退出码: " + proc.ExitCode);
            }
        }

        // ===== 网络信息 =====
        private void RefreshLocalIPs()
        {
            lstIPs.Items.Clear();
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork ||
                            ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            string type = ip.Address.AddressFamily == AddressFamily.InterNetwork ? "IPv4" : "IPv6";
                            string line = string.Format("{0,-5} | {1,-40} | {2}", type, ip.Address, ni.Name);
                            lstIPs.Items.Add(line);
                        }
                    }
                }
                if (lstIPs.Items.Count == 0)
                    lstIPs.Items.Add("(未检测到有效的 IP 地址)");
            }
            catch (Exception ex)
            {
                lstIPs.Items.Add("获取失败: " + ex.Message);
            }
        }

        private void RefreshWiFiList()
        {
            lvWiFi.Items.Clear();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show networks mode=bssid",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(936)
                };
                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    var ssidPattern = new Regex(@"SSID\s+\d+\s*:\s*(.+)");
                    var signalPattern = new Regex(@"信号\s*:\s*(\d+)%");
                    var authPattern = new Regex(@"身份验证\s*:\s*(.+)");

                    string currentSsid = null;
                    string signal = null;
                    string auth = null;

                    foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var m = ssidPattern.Match(line);
                        if (m.Success)
                        {
                            if (currentSsid != null)
                            {
                                AddWiFiItem(currentSsid, signal, auth);
                            }
                            currentSsid = m.Groups[1].Value.Trim();
                            signal = null;
                            auth = null;
                            continue;
                        }
                        m = signalPattern.Match(line);
                        if (m.Success) { signal = m.Groups[1].Value; continue; }
                        m = authPattern.Match(line);
                        if (m.Success) { auth = m.Groups[1].Value.Trim(); }
                    }
                    if (currentSsid != null)
                    {
                        AddWiFiItem(currentSsid, signal, auth);
                    }
                }
            }
            catch (Exception ex)
            {
                lvWiFi.Items.Add(new ListViewItem(new[] { "错误", ex.Message, "", "" }));
            }

            if (lvWiFi.Items.Count == 0)
            {
                lvWiFi.Items.Add(new ListViewItem(new[] { "(未扫描到WiFi)", "", "", "" }));
            }
        }

        private void AddWiFiItem(string ssid, string signal, string auth)
        {
            if (string.IsNullOrEmpty(ssid)) return;
            string connected = IsConnectedSSID(ssid) ? "★ 已连接" : "";

            int sigVal = 0;
            int.TryParse(signal, out sigVal);
            string bars = GetSignalBars(sigVal);
            string sigStr = signal != null ? string.Format("{0}% {1}", signal, bars) : "--";

            auth = auth ?? "--";

            var item = new ListViewItem(new[] { ssid, sigStr, auth, connected });
            if (connected.Length > 0) item.ForeColor = Color.Green;
            lvWiFi.Items.Add(item);
        }

        private string GetSignalBars(int percent)
        {
            if (percent >= 80) return "████";
            if (percent >= 60) return "███░";
            if (percent >= 40) return "██░░";
            if (percent >= 20) return "█░░░";
            return "░░░░";
        }

        private bool IsConnectedSSID(string ssid)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(936)
                };
                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    return output.Contains(ssid);
                }
            }
            catch
            {
                return false;
            }
        }

        // ===== 网速测试 =====
        private async void BtnSpeedTest_Click(object sender, EventArgs e)
        {
            btnSpeedTest.Enabled = false;
            pbSpeed.Visible = true;
            lblDownSpeed.Text = "下载速度: 测试中...";
            lblDownSpeed.ForeColor = Color.Black;
            AppendLog("[测速] 开始网速测试...");

            double speedMbps = 0;

            try
            {
                speedMbps = await Task.Run(() => TestDownloadSpeed());
                lblDownSpeed.Text = string.Format("下载速度: {0:F1} Mbps", speedMbps);
                lblDownSpeed.ForeColor = speedMbps > 0 ? Color.Green : Color.Red;
                AppendLog(string.Format("[测速] 下载速度: {0:F1} Mbps", speedMbps));
            }
            catch (Exception ex)
            {
                lblDownSpeed.Text = "下载速度: 测试失败";
                lblDownSpeed.ForeColor = Color.Red;
                AppendLog("[测速] 失败: " + ex.Message);
            }

            pbSpeed.Visible = false;
            btnSpeedTest.Enabled = true;
        }

        private double TestDownloadSpeed()
        {
            var urls = new[]
            {
                "http://speedtest.tele2.net/10MB.zip",
                "http://ipv4.download.thinkbroadband.com/10MB.zip",
            };

            foreach (var url in urls)
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = 15000;
                    request.ReadWriteTimeout = 15000;

                    long totalBytes = 0;
                    var startTime = DateTime.Now;
                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    {
                        var buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            totalBytes += bytesRead;
                            if ((DateTime.Now - startTime).TotalSeconds > 8) break;
                        }
                    }

                    double elapsed = (DateTime.Now - startTime).TotalSeconds;
                    if (elapsed < 0.5) elapsed = 0.5;
                    double speedMbps = (totalBytes * 8.0) / (elapsed * 1000000.0);
                    return speedMbps;
                }
                catch
                {
                    continue;
                }
            }
            return 0;
        }

        // ===== 日志 =====
        private void AppendLog(string msg)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(AppendLog), msg);
                return;
            }
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            rtbLog.AppendText(string.Format("[{0}] {1}\n", timestamp, msg));
            rtbLog.ScrollToCaret();
        }
    }
}
