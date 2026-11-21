using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SlotboundDropRate
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--scan")
            {
                ScanCli();
                return;
            }
            if (args.Length > 0 && args[0] == "--selftest")
            {
                SelfTest();
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void SelfTest()
        {
            string log = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.ExecutablePath), "selftest.txt");
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            try
            {
                IntPtr buf = Marshal.AllocHGlobal(0x200);
                for (int i = 0; i < 0x200; i++) Marshal.WriteByte(buf, i, 0);
                Marshal.WriteByte(buf, 9, 4);
                Marshal.WriteInt32(buf, 0x14, 26);
                byte[] s = Encoding.ASCII.GetBytes("0.8:0.15:0.045:0.004:0.001");
                Marshal.Copy(s, 0, new IntPtr(buf.ToInt64() + 0x18), s.Length);

                IntPtr h = Native.OpenProcess(
                    Native.PROCESS_VM_READ | Native.PROCESS_VM_WRITE | Native.PROCESS_VM_OPERATION | Native.PROCESS_QUERY_INFORMATION,
                    false, Process.GetCurrentProcess().Id);
                sb.AppendLine("self handle=" + h);
                int a, b, c, d;
                Patcher.Scan(h, "0.8:0.15:0.045:0.004:0.001", "0.5:0.3:0.1:0.07:0.03", false, false, out a, out b, out c, out d);
                sb.AppendLine("str hits=" + a + " writes=" + b + " num hits=" + c + " writes=" + d);
                int len = Marshal.ReadInt32(buf, 0x14);
                sb.AppendLine("len after=" + len);
                byte[] after = new byte[32];
                Marshal.Copy(new IntPtr(buf.ToInt64() + 0x18), after, 0, 32);
                sb.AppendLine("data after=" + Encoding.ASCII.GetString(after).TrimEnd('\0'));
                Native.CloseHandle(h);
                Marshal.FreeHGlobal(buf);
            }
            catch (Exception ex) { sb.AppendLine("异常 " + ex); }
            System.IO.File.WriteAllText(log, sb.ToString(), Encoding.UTF8);
        }

        private static void ScanCli()
        {
            string log = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.ExecutablePath), "scan_result.txt");
            try
            {
                Process[] ps = Process.GetProcessesByName("Slotbound");
                if (ps.Length == 0)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("GetProcessesByName=0; session=" + Process.GetCurrentProcess().SessionId + " clr=" + Environment.Version);
                    foreach (Process p in Process.GetProcesses())
                    {
                        string n = "";
                        string e2 = "";
                        try { n = p.ProcessName; } catch (Exception ex) { e2 = ex.GetType().Name; }
                        sb.AppendLine("  " + p.Id + " [" + n + "] " + e2);
                    }
                    System.IO.File.WriteAllText(log, sb.ToString());
                    return;
                }
                IntPtr h = Native.OpenProcess(
                    Native.PROCESS_VM_READ | Native.PROCESS_VM_WRITE | Native.PROCESS_VM_OPERATION | Native.PROCESS_QUERY_INFORMATION,
                    false, ps[0].Id);
                if (h == IntPtr.Zero) { System.IO.File.WriteAllText(log, "打开进程失败 " + Marshal.GetLastWin32Error()); return; }
                int a, b, c, d;
                Patcher.Scan(h, "0.8:0.15:0.045:0.004:0.001", "0.5:0.3:0.1:0.07:0.03", true, false, out a, out b, out c, out d);
                Native.CloseHandle(h);
                System.IO.File.WriteAllText(log, string.Format(
                    "pid={0}\r\nstring_hits={1}\r\ndouble_hits={2}", ps[0].Id, a, c));
            }
            catch (Exception ex)
            {
                try { System.IO.File.WriteAllText(log, "异常 " + ex); } catch { }
            }
        }
    }

    internal static class Native
    {
        public const uint PROCESS_VM_READ = 0x0010;
        public const uint PROCESS_VM_WRITE = 0x0020;
        public const uint PROCESS_VM_OPERATION = 0x0008;
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;

        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_PRIVATE = 0x20000;
        public const uint PAGE_READWRITE = 0x04;
        public const uint PAGE_EXECUTE_READWRITE = 0x40;
        public const uint PAGE_WRITECOPY = 0x08;
        public const uint PAGE_EXECUTE_WRITECOPY = 0x80;

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr h);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, IntPtr size, out IntPtr read);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, IntPtr size, out IntPtr written);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualQueryEx(IntPtr h, IntPtr addr, out MEMORY_BASIC_INFORMATION mbi, IntPtr len);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtectEx(IntPtr h, IntPtr addr, IntPtr size, uint newProtect, out uint oldProtect);
    }

    internal sealed class MainForm : Form
    {
        private static readonly string[] GradeNames = { "普通", "高级", "稀有", "传说", "棱彩" };
        private static readonly decimal[] Defaults = { 80m, 15m, 4.5m, 0.4m, 0.1m };
        private const string DefaultOdds = "0.8:0.15:0.045:0.004:0.001";

        private readonly NumericUpDown[] _inputs = new NumericUpDown[5];
        private readonly TextBox _oddsBox;
        private readonly Label _status;
        private readonly Label _conn;
        private readonly CheckBox _auto;
        private readonly CheckBox _numChk;
        private readonly Button _applyBtn;
        private readonly Button _restoreBtn;
        private readonly Button _scanBtn;
        private readonly Timer _timer;

        private Process _proc;
        private IntPtr _handle = IntPtr.Zero;
        private bool _busy;
        private string _lastNew = "";

        public MainForm()
        {
            Text = "Slotbound 爆率调节";
            ClientSize = new Size(600, 526);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(28, 26, 38);
            ForeColor = Color.FromArgb(235, 230, 245);

            var title = new Label();
            title.Text = "Slotbound 爆率调节";
            title.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(255, 160, 210);
            title.SetBounds(14, 10, 400, 32);
            Controls.Add(title);

            var sub = new Label();
            sub.Text = "运行时修改本局稀有度权重，不改游戏文件、不改存档";
            sub.ForeColor = Color.FromArgb(170, 165, 185);
            sub.SetBounds(16, 44, 560, 20);
            Controls.Add(sub);

            var group = new GroupBox();
            group.Text = " 爆率（%） ";
            group.ForeColor = Color.FromArgb(210, 205, 225);
            group.SetBounds(14, 70, 280, 240);
            Controls.Add(group);

            for (int i = 0; i < 5; i++)
            {
                var lab = new Label();
                lab.Text = GradeNames[i];
                lab.SetBounds(16, 30 + i * 38, 60, 24);
                group.Controls.Add(lab);

                var num = new NumericUpDown();
                num.DecimalPlaces = 3;
                num.Minimum = 0m;
                num.Maximum = 100m;
                num.Increment = 0.1m;
                num.Value = Defaults[i];
                num.SetBounds(80, 28 + i * 38, 100, 26);
                num.BackColor = Color.FromArgb(45, 42, 58);
                num.ForeColor = Color.White;
                num.BorderStyle = BorderStyle.FixedSingle;
                group.Controls.Add(num);
                _inputs[i] = num;

                var pct = new Label();
                pct.Text = "%";
                pct.SetBounds(186, 30 + i * 38, 20, 22);
                group.Controls.Add(pct);
            }

            var info = new Label();
            info.Text = "默认值 = 你存档当前的爆率\r\n（元升级全满：传说 0.4%、棱彩 0.1%）";
            info.ForeColor = Color.FromArgb(150, 146, 168);
            info.SetBounds(310, 78, 276, 48);
            Controls.Add(info);

            var oddsLab = new Label();
            oddsLab.Text = "当前爆率串（内存里用来定位的原文）";
            oddsLab.ForeColor = Color.FromArgb(200, 196, 215);
            oddsLab.SetBounds(310, 132, 280, 20);
            Controls.Add(oddsLab);

            _oddsBox = new TextBox();
            _oddsBox.Text = DefaultOdds;
            _oddsBox.SetBounds(310, 154, 276, 26);
            _oddsBox.BackColor = Color.FromArgb(45, 42, 58);
            _oddsBox.ForeColor = Color.White;
            _oddsBox.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(_oddsBox);

            _applyBtn = MakeButton("应用一次", 14, 324, 120);
            _applyBtn.Click += delegate { Apply(false); };
            Controls.Add(_applyBtn);

            _restoreBtn = MakeButton("恢复默认", 144, 324, 120);
            _restoreBtn.Click += delegate { Restore(); };
            Controls.Add(_restoreBtn);

            _scanBtn = MakeButton("仅扫描", 274, 324, 120);
            _scanBtn.Click += delegate { Apply(true); };
            Controls.Add(_scanBtn);

            _numChk = new CheckBox();
            _numChk.Text = "同时改数值表（实验）";
            _numChk.SetBounds(230, 366, 200, 24);
            _numChk.ForeColor = Color.FromArgb(170, 165, 185);
            Controls.Add(_numChk);

            _auto = new CheckBox();
            _auto.Text = "自动（每秒补一次）";
            _auto.SetBounds(14, 366, 200, 24);
            _auto.ForeColor = Color.FromArgb(210, 205, 225);
            Controls.Add(_auto);

            _status = new Label();
            _status.Text = "等待操作";
            _status.SetBounds(14, 398, 572, 64);
            _status.BorderStyle = BorderStyle.FixedSingle;
            _status.Padding = new Padding(8);
            _status.ForeColor = Color.FromArgb(220, 216, 235);
            Controls.Add(_status);

            _conn = new Label();
            _conn.Text = "未连接游戏";
            _conn.SetBounds(14, 470, 300, 22);
            _conn.ForeColor = Color.FromArgb(160, 156, 178);
            Controls.Add(_conn);

            var link = new LinkLabel();
            link.Text = "GitHub 项目主页";
            link.SetBounds(440, 470, 150, 22);
            link.LinkColor = Color.FromArgb(255, 160, 210);
            link.LinkClicked += delegate { try { Process.Start("https://github.com/zyw313313-collab/slotbound-drop-rate"); } catch { } };
            Controls.Add(link);

            var tip = new Label();
            tip.Text = "只对当前游戏进程生效；重启游戏后重新打开本工具。建议先备份存档。";
            tip.ForeColor = Color.FromArgb(140, 136, 158);
            tip.SetBounds(14, 496, 572, 22);
            Controls.Add(tip);

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += delegate { OnTick(); };
            _timer.Start();

            FormClosing += delegate { Detach(); };
        }

        private Button MakeButton(string text, int x, int y, int w)
        {
            var b = new Button();
            b.Text = text;
            b.SetBounds(x, y, w, 32);
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = Color.FromArgb(58, 52, 76);
            b.ForeColor = Color.White;
            b.FlatAppearance.BorderColor = Color.FromArgb(110, 100, 140);
            return b;
        }

        private void OnTick()
        {
            if (_proc == null || _proc.HasExited)
            {
                _proc = null;
                if (_handle != IntPtr.Zero) { Native.CloseHandle(_handle); _handle = IntPtr.Zero; }
                _conn.Text = "未连接游戏";
            }
            if (_auto.Checked && !_busy)
                Apply(true);
        }

        private bool Attach()
        {
            if (_proc != null && !_proc.HasExited) return true;
            Process[] ps = Process.GetProcessesByName("Slotbound");
            if (ps.Length == 0)
            {
                _conn.Text = "未找到 Slotbound.exe，请先启动游戏";
                return false;
            }
            _proc = ps[0];
            _handle = Native.OpenProcess(
                Native.PROCESS_VM_READ | Native.PROCESS_VM_WRITE | Native.PROCESS_VM_OPERATION | Native.PROCESS_QUERY_INFORMATION,
                false, _proc.Id);
            if (_handle == IntPtr.Zero)
            {
                _conn.Text = "无法打开进程（错误 " + Marshal.GetLastWin32Error() + "），请用管理员身份运行";
                _proc = null;
                return false;
            }
            _conn.Text = "已连接 Slotbound (PID " + _proc.Id + ")";
            return true;
        }

        private void Detach()
        {
            if (_handle != IntPtr.Zero) { Native.CloseHandle(_handle); _handle = IntPtr.Zero; }
            _proc = null;
        }

        private decimal[] ReadValues()
        {
            var vals = new decimal[5];
            decimal sum = 0m;
            for (int i = 0; i < 5; i++) { vals[i] = _inputs[i].Value; sum += vals[i]; }
            if (sum <= 0m) return null;
            if (Math.Abs(sum - 100m) > 0.001m)
                for (int i = 0; i < 5; i++) vals[i] = vals[i] * 100m / sum;
            return vals;
        }

        private static string FormatOdds(decimal[] vals, int maxLen)
        {
            string[] parts = new string[5];
            for (int i = 0; i < 5; i++)
            {
                string s = (vals[i] / 100m).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
                if (s == "") s = "0";
                parts[i] = s;
            }
            string outp = string.Join(":", parts);
            if (outp.Length > maxLen)
            {
                for (int p = 3; p >= 1; p--)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        string fmt = "0." + new string('#', p);
                        string s = (vals[i] / 100m).ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
                        if (s == "") s = "0";
                        parts[i] = s;
                    }
                    outp = string.Join(":", parts);
                    if (outp.Length <= maxLen) break;
                }
            }
            if (outp.Length > maxLen) outp = outp.Substring(0, maxLen);
            return outp;
        }

        private void Apply(bool scanOnly)
        {
            if (_busy) return;
            if (!Attach()) return;
            decimal[] vals = ReadValues();
            if (vals == null)
            {
                _status.Text = "数值无效：至少填一个大于 0 的百分比";
                return;
            }
            string orig = _oddsBox.Text.Trim();
            if (orig.Length < 5)
            {
                _status.Text = "当前爆率串太短";
                return;
            }
            string newStr = FormatOdds(vals, orig.Length);
            _lastNew = newStr;
            _busy = true;
            SetBusy(true);
            _status.Text = scanOnly ? "扫描中…" : "写入中…";

            IntPtr handle = _handle;
            Task.Factory.StartNew(delegate
            {
                int strHits = 0, strWrites = 0, numHits = 0, numWrites = 0;
                string err = null;
                try
                {
                    Patcher.Scan(handle, orig, newStr, scanOnly, _numChk.Checked, out strHits, out strWrites, out numHits, out numWrites);
                }
                catch (Exception ex) { err = ex.Message; }
                BeginInvoke((Action)delegate
                {
                    _busy = false;
                    SetBusy(false);
                    if (err != null)
                    {
                        _status.Text = "出错：" + err;
                        return;
                    }
                    string head = scanOnly ? "扫描结果" : "已应用";
                    _status.Text = string.Format(
                        "{0}\r\n爆率串：匹配 {1} 处，改写 {2} 处\r\n数值表：匹配 {3} 处，改写 {4} 处\r\n新爆率：{5}",
                        head, strHits, strWrites, numHits, numWrites, newStr);
                    if (!scanOnly && strWrites == 0 && numWrites == 0)
                        _status.Text += "\r\n没找到可改的爆率串：先进入一局、拉一次杆再点应用。";
                });
            });
        }

        private void Restore()
        {
            if (_busy) return;
            if (!Attach()) return;
            decimal[] vals = ReadValues();
            if (vals == null) return;
            string orig = _oddsBox.Text.Trim();
            string cur = FormatOdds(vals, orig.Length);
            if (cur == orig)
            {
                _status.Text = "当前百分比就是默认值，无需恢复";
                return;
            }
            _busy = true;
            SetBusy(true);
            IntPtr handle = _handle;
            Task.Factory.StartNew(delegate
            {
                int a, b, c, d;
                string err = null;
                try { Patcher.Scan(handle, cur, orig, false, _numChk.Checked, out a, out b, out c, out d); }
                catch (Exception ex) { err = ex.Message; a = b = c = d = 0; }
                BeginInvoke((Action)delegate
                {
                    _busy = false;
                    SetBusy(false);
                    _status.Text = err != null ? ("恢复出错：" + err) : string.Format("已恢复 {0} 处爆率串", b);
                });
            });
        }

        private void SetBusy(bool busy)
        {
            _applyBtn.Enabled = !busy;
            _restoreBtn.Enabled = !busy;
            _scanBtn.Enabled = !busy;
        }
    }

    internal static class Patcher
    {
        private const int Chunk = 8 * 1024 * 1024;
        private const int Overlap = 64;

        public static void Scan(IntPtr h, string orig, string newStr, bool scanOnly, bool patchNumbers,
            out int strHits, out int strWrites, out int numHits, out int numWrites)
        {
            strHits = 0; strWrites = 0; numHits = 0; numWrites = 0;
            byte[] strPat = Encoding.ASCII.GetBytes(orig);
            List<byte[]> numPats = new List<byte[]>();
            numPats.Add(PackDoubles(0.8, 0.15, 0.045, 0.004, 0.001));
            numPats.Add(PackDoubles(0.82, 0.15, 0.02, 0.001, 0.0002));

            var stringHits = new List<long>();
            var numHitsByPat = new List<List<long>>();
            for (int i = 0; i < numPats.Count; i++) numHitsByPat.Add(new List<long>());

            long addr = 0;
            Native.MEMORY_BASIC_INFORMATION mbi;
            while (Native.VirtualQueryEx(h, new IntPtr(addr), out mbi, new IntPtr(Marshal.SizeOf(typeof(Native.MEMORY_BASIC_INFORMATION)))) != IntPtr.Zero)
            {
                long baseAddr = mbi.BaseAddress.ToInt64();
                long size = mbi.RegionSize.ToInt64();
                if (size <= 0) break;
                bool usable = mbi.State == Native.MEM_COMMIT && mbi.Type == Native.MEM_PRIVATE &&
                              (mbi.Protect == Native.PAGE_READWRITE || mbi.Protect == Native.PAGE_EXECUTE_READWRITE ||
                               mbi.Protect == Native.PAGE_WRITECOPY || mbi.Protect == Native.PAGE_EXECUTE_WRITECOPY);
                if (usable)
                {
                    for (long at = baseAddr; at < baseAddr + size; at += Chunk)
                    {
                        long start = at > baseAddr + Overlap ? at - Overlap : baseAddr;
                        int want = (int)Math.Min(Chunk + Overlap, baseAddr + size - start);
                        byte[] buf = new byte[want];
                        IntPtr got;
                        if (!Native.ReadProcessMemory(h, new IntPtr(start), buf, new IntPtr(want), out got) || got.ToInt64() == 0)
                            continue;
                        int n = (int)got.ToInt64();
                        for (int i = 0; i + strPat.Length <= n; i++)
                        {
                            if (buf[i] != strPat[0]) continue;
                            bool ok = true;
                            for (int j = 1; j < strPat.Length; j++) { if (buf[i + j] != strPat[j]) { ok = false; break; } }
                            if (ok) stringHits.Add(start + i);
                        }
                        for (int p = 0; p < numPats.Count; p++)
                        {
                            byte[] np = numPats[p];
                            for (int i = 0; i + np.Length <= n; i++)
                            {
                                if (buf[i] != np[0]) continue;
                                bool ok = true;
                                for (int j = 1; j < np.Length; j++) { if (buf[i + j] != np[j]) { ok = false; break; } }
                                if (ok && ((start + i) % 8) == 0) numHitsByPat[p].Add(start + i);
                            }
                        }
                    }
                }
                long next = baseAddr + size;
                if (next <= addr) break;
                addr = next;
            }

            strHits = stringHits.Count;
            foreach (var list in numHitsByPat) numHits += list.Count;
            if (scanOnly) return;

            byte[] newBytes = Encoding.ASCII.GetBytes(newStr);
            foreach (long hit in stringHits)
            {
                try
                {
                    if (!LooksLikeLuaString(h, hit, strPat.Length)) continue;
                    if (WriteString(h, hit, newBytes)) strWrites++;
                }
                catch { }
            }

            if (patchNumbers)
            {
                for (int p = 0; p < numPats.Count; p++)
                {
                    foreach (long hit in numHitsByPat[p])
                    {
                        try { if (WriteNumbers(h, hit, newStr)) numWrites++; }
                        catch { }
                    }
                }
            }
        }

        private static bool LooksLikeLuaString(IntPtr h, long dataAddr, int len)
        {
            byte[] hdr = new byte[0x18];
            IntPtr got;
            if (!Native.ReadProcessMemory(h, new IntPtr(dataAddr - 0x18), hdr, new IntPtr(hdr.Length), out got))
                return false;
            if (got.ToInt64() != hdr.Length) return false;
            if (hdr[9] != 4) return false;
            uint l = BitConverter.ToUInt32(hdr, 0x14);
            return l == (uint)len;
        }

        private static bool WriteString(IntPtr h, long dataAddr, byte[] bytes)
        {
            IntPtr written;
            if (!Native.WriteProcessMemory(h, new IntPtr(dataAddr), bytes, new IntPtr(bytes.Length), out written))
                return false;
            if (written.ToInt64() != bytes.Length) return false;
            byte[] zero = new byte[] { 0 };
            Native.WriteProcessMemory(h, new IntPtr(dataAddr + bytes.Length), zero, new IntPtr(1), out written);
            byte[] lenBytes = BitConverter.GetBytes((uint)bytes.Length);
            Native.WriteProcessMemory(h, new IntPtr(dataAddr - 4), lenBytes, new IntPtr(4), out written);
            return true;
        }

        private static bool WriteNumbers(IntPtr h, long addr, string newStr)
        {
            string[] parts = newStr.Split(':');
            if (parts.Length != 5) return false;
            byte[] all = new byte[40];
            for (int i = 0; i < 5; i++)
            {
                double v;
                if (!double.TryParse(parts[i], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out v))
                    return false;
                byte[] b = BitConverter.GetBytes(v);
                Buffer.BlockCopy(b, 0, all, i * 8, 8);
            }
            IntPtr written;
            return Native.WriteProcessMemory(h, new IntPtr(addr), all, new IntPtr(40), out written) && written.ToInt64() == 40;
        }

        private static byte[] PackDoubles(params double[] vals)
        {
            byte[] b = new byte[vals.Length * 8];
            for (int i = 0; i < vals.Length; i++)
                Buffer.BlockCopy(BitConverter.GetBytes(vals[i]), 0, b, i * 8, 8);
            return b;
        }
    }
}
