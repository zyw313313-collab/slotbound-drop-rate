using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SlotboundDropRate
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            bool scan = false;
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "--lang=en":
                    case "--english": L.Zh = false; break;
                    case "--lang=zh":
                    case "--chinese": L.Zh = true; break;
                    case "--scan": scan = true; break;
                    default:
                        Console.Error.WriteLine("Usage: SlotboundDropRate [--scan] [--lang=en|--lang=zh]");
                        return 2;
                }
            }

            if (scan) return ScanCli();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new MainForm()) Application.Run(form);
            return 0;
        }

        private static int ScanCli()
        {
            string output;
            int exitCode = 0;
            try
            {
                using (RaritySession session = RaritySession.Connect())
                {
                    double[] weights = session.ReadWeights();
                    output = L.T("仅扫描：已读取当前爆率（未写入）", "Scan only: current drop rates read (no writes)")
                        + " | PID " + session.ProcessId + Environment.NewLine + Odds.Format(weights);
                }
            }
            catch (Exception error)
            {
                output = L.T("仅扫描失败（未写入）：", "Scan failed (no writes): ") + L.Error(error);
                exitCode = 1;
            }

            try
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scan_result.txt"),
                    output + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception)
            {
                Console.Error.WriteLine(L.T("无法写入 scan_result.txt。", "Could not write scan_result.txt."));
                if (exitCode == 0) exitCode = 2;
            }

            Console.WriteLine(output);
            return exitCode;
        }
    }

    internal static class L
    {
        public static bool Zh = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh";

        public static string T(string zh, string en) { return Zh ? zh : en; }

        public static string Error(Exception error)
        {
            var rarityError = error as RarityException;
            return rarityError != null ? T(rarityError.Chinese, rarityError.Message)
                : T("操作意外失败。", "The operation failed unexpectedly.");
        }
    }

    internal static class Odds
    {
        private static readonly string[] GradeZh = { "普通", "高级", "稀有", "传说", "棱彩" };
        private static readonly string[] GradeEn = { "Normal", "Advanced", "Rare", "Legendary", "Prismatic" };
        public const int Count = 5;

        public static string Grade(int index) { return L.T(GradeZh[index], GradeEn[index]); }

        public static double[] ToWeights(decimal[] percentages)
        {
            if (percentages.Length != Count) return null;

            var weights = new double[Count];
            decimal sum = 0m;
            for (int index = 0; index < Count; index++)
            {
                decimal percent = percentages[index];
                if (percent < 0m || percent > 100m) return null;
                sum += percent;
                weights[index] = (double)(percent / 100m);
            }
            return sum == 100m ? weights : null;
        }

        public static string Format(double[] weights)
        {
            var rows = new string[Count];
            for (int index = 0; index < Count; index++)
            {
                rows[index] = Grade(index) + ": "
                    + (weights[index] * 100d).ToString("G15", CultureInfo.CurrentCulture) + "%";
            }
            return string.Join(Environment.NewLine, rows);
        }
    }

    internal sealed class MainForm : Form
    {
        private enum Operation { Read, Apply, Restore }

        private static readonly Color Accent = Color.FromArgb(255, 160, 210);
        private static readonly Color Muted = Color.FromArgb(174, 168, 191);
        private readonly TextBox[] _inputs = new TextBox[Odds.Count];
        private readonly Label[] _gradeLabels = new Label[Odds.Count];
        private readonly Label _title;
        private readonly Label _sub;
        private readonly GroupBox _group;
        private readonly GroupBox _readbackGroup;
        private readonly Label _readbackText;
        private readonly Label _status;
        private readonly Label _conn;
        private readonly Label _tip;
        private readonly LinkLabel _repoLink;
        private readonly LinkLabel _langLink;
        private readonly Button _readBtn;
        private readonly Button _applyBtn;
        private readonly Button _restoreBtn;
        private readonly Timer _timer;
        private readonly object _sessionGate = new object();

        private RaritySession _session;
        private bool _disposed;
        private double[] _verifiedWeights;
        private Func<string> _statusText = delegate
        {
            return L.T("进入游戏后，点击“读取当前爆率”。", "Start the game, then select Read current odds.");
        };
        private bool _busy;
        private bool _closeRequested;
        private bool _canApply;

        public MainForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            SuspendLayout();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(28, 26, 38);
            ForeColor = Color.FromArgb(235, 230, 245);
            _title = MakeLabel(18, 14, 430, 34, Accent);
            _title.Font = new Font(Font.FontFamily, 15F, FontStyle.Bold);
            _sub = MakeLabel(18, 50, 584, 24, Muted);
            _langLink = MakeLink(510, 18, 92);
            _langLink.TextAlign = ContentAlignment.MiddleRight;
            _langLink.LinkClicked += delegate { L.Zh = !L.Zh; ApplyLanguage(); };

            _group = MakeGroup(18, 80, 286, 208);
            _readbackGroup = MakeGroup(316, 80, 286, 208);
            _readbackText = new Label();
            _readbackText.SetBounds(14, 27, 260, 164);
            _readbackGroup.Controls.Add(_readbackText);

            for (int index = 0; index < Odds.Count; index++)
            {
                var label = new Label();
                label.SetBounds(14, 29 + index * 33, 88, 22);
                _group.Controls.Add(label);
                _gradeLabels[index] = label;

                var input = new TextBox();
                input.AutoSize = false;
                input.SetBounds(104, 27 + index * 33, 106, 24);
                input.TextAlign = HorizontalAlignment.Right;
                input.MaxLength = 32;
                input.BackColor = Color.FromArgb(45, 42, 58);
                input.ForeColor = Color.White;
                input.BorderStyle = BorderStyle.FixedSingle;
                _group.Controls.Add(input);
                _inputs[index] = input;

                var percent = new Label();
                percent.Text = "%";
                percent.SetBounds(217, 29 + index * 33, 22, 22);
                _group.Controls.Add(percent);
            }

            _readBtn = MakeButton(18, 184);
            _readBtn.Click += async delegate { await RunAsync(Operation.Read, null); };
            _applyBtn = MakeButton(214, 192);
            _applyBtn.BackColor = Color.FromArgb(109, 48, 94);
            _applyBtn.FlatAppearance.BorderColor = Accent;
            _applyBtn.Click += async delegate
            {
                double[] weights = ReadDraft();
                if (weights == null)
                {
                    _statusText = delegate
                    {
                        return L.T("请输入 5 个 0 至 100 之间的百分比，总和须为 100%。",
                                   "Enter five percentages from 0 to 100 that add up to 100%.");
                    };
                    UpdateStatus();
                    return;
                }
                await RunAsync(Operation.Apply, weights);
            };
            _restoreBtn = MakeButton(418, 184);
            _restoreBtn.Click += async delegate { await RunAsync(Operation.Restore, null); };

            _status = MakeLabel(18, 344, 584, 55, ForeColor);
            _status.BackColor = Color.FromArgb(37, 33, 50);
            _status.BorderStyle = BorderStyle.FixedSingle;
            _status.Padding = new Padding(9);
            _conn = MakeLabel(18, 406, 400, 22, Muted);
            _repoLink = MakeLink(430, 406, 172);
            _repoLink.TextAlign = ContentAlignment.MiddleRight;
            _repoLink.LinkClicked += delegate
            {
                try { Process.Start("https://github.com/zyw313313-collab/slotbound-drop-rate"); }
                catch (Exception) { }
            };
            _tip = MakeLabel(18, 434, 584, 22, Muted);

            // Apply one scale to the window and every control created above.
            using (Graphics display = CreateGraphics())
            {
                float scale = display.DpiX / 96F;
                ScaleLayout(Controls, scale);
                ClientSize = new Size(Pixels(620, scale), Pixels(470, scale));
            }
            ResumeLayout(true);

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += delegate { CheckForExit(); };
            _timer.Start();
            ActiveControl = _readBtn;
            ApplyLanguage();
        }

        private static int Pixels(int logicalSize, float scale)
        {
            return (int)Math.Round(logicalSize * scale);
        }

        private static void ScaleLayout(Control.ControlCollection controls, float scale)
        {
            foreach (Control control in controls)
            {
                Rectangle bounds = control.Bounds;
                if (control is TextBox) ((TextBox)control).AutoSize = false;
                control.SetBounds(Pixels(bounds.X, scale), Pixels(bounds.Y, scale),
                    Pixels(bounds.Width, scale), Pixels(bounds.Height, scale));
                Padding padding = control.Padding;
                control.Padding = new Padding(Pixels(padding.Left, scale), Pixels(padding.Top, scale),
                    Pixels(padding.Right, scale), Pixels(padding.Bottom, scale));
                ScaleLayout(control.Controls, scale);
            }
        }

        private Label MakeLabel(int left, int top, int width, int height, Color color)
        {
            var label = new Label();
            label.ForeColor = color;
            label.SetBounds(left, top, width, height);
            Controls.Add(label);
            return label;
        }

        private LinkLabel MakeLink(int left, int top, int width)
        {
            var link = new LinkLabel();
            link.LinkColor = Accent;
            link.SetBounds(left, top, width, 22);
            Controls.Add(link);
            return link;
        }

        private GroupBox MakeGroup(int left, int top, int width, int height)
        {
            var group = new GroupBox();
            group.ForeColor = ForeColor;
            group.SetBounds(left, top, width, height);
            Controls.Add(group);
            return group;
        }

        private Button MakeButton(int left, int width)
        {
            var button = new Button();
            button.SetBounds(left, 300, width, 32);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(58, 52, 76);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = Color.FromArgb(110, 100, 140);
            Controls.Add(button);
            return button;
        }

        private void ApplyLanguage()
        {
            Text = L.T("Slotbound 爆率调节", "Slotbound Drop Rate");
            _title.Text = Text;
            _langLink.Text = L.T("English", "中文");
            _sub.Text = L.T("调整本局常规稀有度爆率，不改游戏文件、不改存档。",
                            "Adjust regular rarity odds for this run. No game file or save edits.");
            _group.Text = L.T(" 待应用爆率（%） ", " Odds to apply (%) ");
            _readbackGroup.Text = L.T(" 已验证的爆率 ", " Verified odds ");
            for (int index = 0; index < Odds.Count; index++)
            {
                _gradeLabels[index].Text = Odds.Grade(index);
                _inputs[index].AccessibleName = Odds.Grade(index);
            }
            _readBtn.Text = L.T("读取当前爆率", "Read current odds");
            _applyBtn.Text = L.T("应用一次", "Apply once");
            _restoreBtn.Text = L.T("恢复原始爆率", "Restore original");
            _repoLink.Text = L.T("GitHub 项目主页", "GitHub project");
            _tip.Text = L.T("支持 Steam 版 v1.0.2 / v1.0.3。更改仅对当前进程生效。",
                            "Steam v1.0.2 / v1.0.3. Changes affect this process only.");
            RefreshView();
        }

        private decimal? ParsePercentage(string text)
        {
            decimal percent;
            if (!decimal.TryParse(text.Trim(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                                  CultureInfo.CurrentCulture, out percent)) return null;
            return percent;
        }

        private double[] ReadDraft()
        {
            var percentages = new decimal[Odds.Count];
            for (int index = 0; index < Odds.Count; index++)
            {
                decimal? percent = ParsePercentage(_inputs[index].Text);
                if (!percent.HasValue) return null;
                percentages[index] = percent.Value;
            }
            return Odds.ToWeights(percentages);
        }

        private async Task RunAsync(Operation operation, double[] weights)
        {
            if (_busy) return;
            CheckForExit();
            if (operation != Operation.Read && (_session == null
                || (operation == Operation.Apply && !_canApply)
                || (operation == Operation.Restore && !_session.HasOriginal))) return;

            _busy = true;
            _statusText = delegate
            {
                switch (operation)
                {
                    case Operation.Read: return L.T("正在读取…", "Reading…");
                    case Operation.Apply: return L.T("正在应用并验证…", "Applying and verifying…");
                    default: return L.T("正在恢复并验证…", "Restoring and verifying…");
                }
            };
            RefreshView();
            try
            {
                double[] observed = await Task.Run(delegate
                {
                    lock (_sessionGate)
                    {
                        return _disposed ? null : Perform(operation, weights);
                    }
                });
                if (_disposed) return;
                _verifiedWeights = observed;
                _canApply = true;
                for (int index = 0; index < Odds.Count; index++)
                {
                    _inputs[index].Text = ((decimal)(_verifiedWeights[index] * 100d))
                        .ToString("0.############################", CultureInfo.CurrentCulture);
                }
                _statusText = delegate
                {
                    switch (operation)
                    {
                        case Operation.Read: return L.T("已读取当前爆率，未写入。", "Current odds read. Nothing was written.");
                        case Operation.Apply: return L.T("已应用并验证实际爆率。", "Applied and verified the live odds.");
                        default: return L.T("已恢复并验证原始爆率。", "Restored and verified the original odds.");
                    }
                };
            }
            catch (Exception error)
            {
                if (_disposed) return;
                _canApply = false;
                _verifiedWeights = null;
                _statusText = delegate { return L.T("操作失败：", "Operation failed: ") + L.Error(error); };
            }
            finally
            {
                _busy = false;
                if (!_disposed)
                {
                    if (!CheckForExit()) RefreshView();
                    if (_closeRequested)
                    {
                        _closeRequested = false;
                        Close();
                    }
                }
            }
        }

        private double[] Perform(Operation operation, double[] weights)
        {
            if (operation == Operation.Apply) return _session.Apply(weights);
            if (operation == Operation.Restore) return _session.Restore();
            if (_session != null && _session.HasOriginal) return _session.ReadWeights();

            RaritySession next = RaritySession.Connect();
            double[] observed;
            try { observed = next.ReadWeights(); }
            catch
            {
                next.Dispose();
                throw;
            }
            Detach();
            _session = next;
            return observed;
        }

        private bool CheckForExit()
        {
            if (_busy || _session == null || !_session.HasExited) return false;
            Detach();
            _verifiedWeights = null;
            _canApply = false;
            foreach (TextBox input in _inputs) input.Clear();
            _statusText = delegate
            {
                return L.T("游戏已退出。重新启动游戏后，请再次读取当前爆率。",
                           "The game exited. Restart the game, then read the current odds again.");
            };
            RefreshView();
            return true;
        }

        private void Detach()
        {
            if (_session == null) return;
            _session.Dispose();
            _session = null;
        }

        private void RefreshView()
        {
            _readBtn.Enabled = !_busy;
            _applyBtn.Enabled = !_busy && _canApply;
            _restoreBtn.Enabled = !_busy && _session != null && _session.HasOriginal;
            foreach (TextBox input in _inputs) input.Enabled = !_busy && _canApply;
            _conn.Text = _busy ? L.T("正在处理…", "Working…")
                : _session == null ? L.T("未连接游戏", "Not connected")
                : L.T("已连接 Slotbound", "Connected to Slotbound") + " (PID " + _session.ProcessId + ")";
            _readbackText.Text = _verifiedWeights == null
                ? L.T("没有已验证的爆率。", "No verified odds yet.") : Odds.Format(_verifiedWeights);
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            _status.Text = _closeRequested
                ? L.T("操作完成后关闭…", "Closing after the operation finishes…") : _statusText();
        }

        protected override void OnFormClosing(FormClosingEventArgs args)
        {
            base.OnFormClosing(args);
            if (args.Cancel) return;
            if (_busy)
            {
                args.Cancel = true;
                _closeRequested = true;
                UpdateStatus();
                return;
            }
            CheckForExit();
            if (args.CloseReason == CloseReason.UserClosing && _session != null && _session.HasOriginal)
            {
                DialogResult answer = MessageBox.Show(this,
                    L.T("关闭不会自动恢复爆率。仍要关闭吗？", "Closing does not restore the original odds. Close anyway?"),
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                args.Cancel = answer != DialogResult.Yes;
                if (args.Cancel) UpdateStatus();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null) _timer.Dispose();
                lock (_sessionGate)
                {
                    _disposed = true;
                    Detach();
                }
            }
            base.Dispose(disposing);
        }
    }
}
