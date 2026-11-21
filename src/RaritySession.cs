using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SlotboundDropRate
{
    internal sealed class RarityException : Exception
    {
        internal string Chinese { get; private set; }
        internal RarityException(string chinese, string english) : base(english) { Chinese = chinese; }
    }

    internal sealed class OddsChunk
    {
        internal readonly long Address;
        internal readonly byte[] Data;
        internal OddsChunk(long address, byte[] data) { Address = address; Data = data; }
    }

    internal interface IOddsMemory
    {
        byte[] Read(long address, int length);
        IEnumerable<OddsChunk> Chunks();
        bool WriteNumber(long address, double value);
    }

    internal sealed class OddsTable
    {
        private const ulong PointerMask = 0x00007FFFFFFFFFFFUL;
        private const ulong TagString = 0xFFFD800000000000UL;
        private const ulong TagTable = 0xFFFA000000000000UL;
        private const int NodeSize = 24;
        private static readonly byte[] GlobalText = Encoding.ASCII.GetBytes("_G\0");
        private static readonly string[] WeightNames = { "normal", "advanced", "rare", "legendary", "prism" };
        private readonly IOddsMemory memory;
        private long globals;
        private Lookup snapshotTable;
        private double[] originals;
        private double[] lastApplied;
        private bool rollbackUncertain;

        internal OddsTable(IOddsMemory memory) { this.memory = memory; }
        internal bool HasOriginal { get { return originals != null; } }

        internal double[] Read()
        {
            return LookupCurrent().Values;
        }

        internal double[] Apply(double[] weights)
        {
            if (!Valid(weights)) throw new RarityException("五档概率必须在 0–100% 内且合计 100%。", "Enter five percentages between 0 and 100 that add up to 100%.");
            if (rollbackUncertain) throw Uncertain();
            Lookup target = LookupCurrent();
            if (HasOriginal && !snapshotTable.Same(target)) throw Replaced();
            if (!HasOriginal)
            {
                originals = target.Values;
                snapshotTable = target;
            }
            else if (!Equal(target.Values, lastApplied)) throw Changed();
            try
            {
                double[] applied = Write(target, weights);
                lastApplied = applied;
                return applied;
            }
            catch
            {
                if (!rollbackUncertain && lastApplied == null) Reset();
                throw;
            }
        }

        internal double[] Restore()
        {
            if (rollbackUncertain) throw Uncertain();
            if (!HasOriginal) return Read();
            Lookup current = LookupCurrent();
            if (!snapshotTable.Same(current)) throw Replaced();
            if (!Equal(current.Values, lastApplied)) throw Changed();
            double[] restored = Write(current, originals);
            Reset();
            return restored;
        }

        private void Reset()
        {
            originals = null;
            lastApplied = null;
            snapshotTable = null;
        }

        private double[] Write(Lookup target, double[] weights)
        {
            Lookup current = LookupCurrent();
            if (!target.Same(current)) throw Replaced();
            if (!Equal(current.Values, target.Values)) throw Changed();
            var attempted = new List<int>();
            try
            {
                for (int pass = 0; pass < 2; pass++)
                    for (int index = 0; index < weights.Length; index++)
                    {
                        double previous = target.Values[index];
                        if (weights[index] == previous || (weights[index] > previous) != (pass == 0)) continue;
                        Lookup before = Resolve(globals);
                        if (!target.Same(before) || before.Values[index] != previous) throw Changed();
                        attempted.Add(index);
                        if (!memory.WriteNumber(target.Slots[index], weights[index])) throw Changed();
                    }
                Lookup written = Resolve(globals);
                if (target.Same(written) && Equal(written.Values, weights)) return written.Values;
                throw Changed();
            }
            catch
            {
                if (attempted.Count == 0) throw;
                try
                {
                    for (int cursor = attempted.Count - 1; cursor >= 0; cursor--)
                    {
                        int index = attempted[cursor];
                        Lookup before = Resolve(globals);
                        if (!target.Same(before)) throw Uncertain();
                        double currentValue = before.Values[index];
                        if (currentValue == target.Values[index]) continue;
                        if (currentValue != weights[index] || !memory.WriteNumber(target.Slots[index], target.Values[index])) throw Uncertain();
                    }
                    Lookup reverted = Resolve(globals);
                    if (!target.Same(reverted) || !Equal(reverted.Values, target.Values)) throw Uncertain();
                }
                catch
                {
                    rollbackUncertain = true;
                    throw Uncertain();
                }
                throw new RarityException("未能应用：写入未通过回读，已确认恢复本次操作前的数值。", "Not applied: readback failed; values before this operation were restored and verified.");
            }
        }

        private Lookup LookupCurrent()
        {
            if (globals == 0) Locate();
            Lookup result = Resolve(globals);
            if (result == null) throw Replaced();
            if (!Valid(result.Values)) throw new RarityException("游戏返回的概率无效；未写入。请重启游戏再试。", "The game's probabilities are invalid. Nothing was written. Restart the game and retry.");
            return result;
        }

        private void Locate()
        {
            var strings = new HashSet<long>();
            foreach (OddsChunk chunk in memory.Chunks())
                foreach (int offset in Find(chunk.Data, GlobalText))
                {
                    long address = chunk.Address + offset - 24;
                    if ((address & 7) == 0 && ReadString(TagString | (ulong)address) == "_G") strings.Add(address);
                }
            var roots = new HashSet<long>();
            foreach (OddsChunk chunk in memory.Chunks())
                foreach (long text in strings)
                {
                    ulong tag = TagString | (ulong)text;
                    foreach (int offset in Find(chunk.Data, BitConverter.GetBytes(tag)))
                    {
                        long key = chunk.Address + offset;
                        if ((key & 7) != 0) continue;
                        byte[] value = memory.Read(key - 8, 8);
                        long table = value == null ? 0 : Address(BitConverter.ToUInt64(value, 0), TagTable);
                        var nodes = ReadNodes(table);
                        if (nodes == null || key < nodes.Address + 8 || key >= nodes.Address + nodes.Data.Length || (key - nodes.Address - 8) % NodeSize != 0) continue;
                        Lookup candidate = Resolve(table);
                        if (candidate != null && Valid(candidate.Values)) roots.Add(table);
                        if (roots.Count > 1) throw new RarityException("检测到多个可能的游戏权重表；未写入。", "Found multiple possible game probability tables. Nothing was written.");
                    }
                }
            foreach (long root in roots) globals = root;
            if (globals == 0) throw new RarityException("尚未找到游戏权重表；请等游戏加载完毕，再点击读取。", "Game probability table not found. Wait for the game to finish loading, then read again.");
        }

        private Lookup Resolve(long root)
        {
            if (TableField(root, "_G") != root) return null;
            long package = TableField(root, "package");
            long loaded = TableField(package, "loaded");
            long constants = TableField(loaded, "src.data.constants");
            long grade = TableField(constants, "unitGrades");
            long table = TableField(grade, "probabilities");
            var nodes = ReadNodes(table);
            if (nodes == null) return null;
            var addresses = new long[WeightNames.Length];
            var values = new double[WeightNames.Length];
            int found = 0;
            bool special = false;
            for (int offset = 0; offset < nodes.Data.Length; offset += NodeSize)
            {
                string name = ReadString(BitConverter.ToUInt64(nodes.Data, offset + 8));
                if (name == null) continue;
                int index = Array.IndexOf(WeightNames, name);
                if (index < 0 && name != "special") continue;
                double value = BitConverter.ToDouble(nodes.Data, offset);
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1) return null;
                if (name == "special")
                {
                    if (special || value != 0) return null;
                    special = true;
                }
                else
                {
                    if (addresses[index] != 0) return null;
                    addresses[index] = nodes.Address + offset;
                    values[index] = value;
                    found++;
                }
            }
            return special && found == WeightNames.Length ? new Lookup(table, addresses, values) : null;
        }

        private long TableField(long table, string name)
        {
            OddsChunk nodes = ReadNodes(table);
            if (nodes == null) return 0;
            long result = 0;
            for (int off = 0; off < nodes.Data.Length; off += NodeSize)
            {
                ulong value = BitConverter.ToUInt64(nodes.Data, off);
                if (Address(value, TagTable) == 0 || ReadString(BitConverter.ToUInt64(nodes.Data, off + 8)) != name) continue;
                if (result != 0) return 0;
                result = Address(value, TagTable);
            }
            return result;
        }

        private OddsChunk ReadNodes(long table)
        {
            if (table < 0x1000) return null;
            byte[] header = memory.Read(table, 56);
            if (header == null || header[9] != 11) return null;
            ulong nodes = BitConverter.ToUInt64(header, 40);
            uint mask = BitConverter.ToUInt32(header, 52);
            if (nodes < 0x1000 || nodes > PointerMask || (nodes & 7) != 0 || mask > 65535 || (mask & (mask + 1)) != 0) return null;
            byte[] values = memory.Read((long)nodes, (int)(mask + 1) * NodeSize);
            return values == null ? null : new OddsChunk((long)nodes, values);
        }

        private string ReadString(ulong tagged)
        {
            long address = Address(tagged, TagString);
            if (address < 0x1000) return null;
            byte[] header = memory.Read(address, 24);
            if (header == null || header[9] != 4) return null;
            uint length = BitConverter.ToUInt32(header, 20);
            if (length < 1 || length > 32) return null;
            byte[] body = memory.Read(address + 24, (int)length + 1);
            return body == null || body[length] != 0 ? null : Encoding.ASCII.GetString(body, 0, (int)length);
        }

        private static long Address(ulong value, ulong tag)
        {
            return (value & ~PointerMask) == tag ? (long)(value & PointerMask) : 0;
        }

        private static IEnumerable<int> Find(byte[] data, byte[] pattern)
        {
            int start = 0;
            while (start <= data.Length - pattern.Length)
            {
                int candidate = Array.IndexOf(data, pattern[0], start, data.Length - pattern.Length - start + 1);
                if (candidate < 0) yield break;
                int cursor = 1;
                while (cursor < pattern.Length && data[candidate + cursor] == pattern[cursor]) cursor++;
                if (cursor == pattern.Length) yield return candidate;
                start = candidate + 1;
            }
        }

        private static bool Valid(double[] weights)
        {
            if (weights == null || weights.Length != WeightNames.Length) return false;
            double total = 0;
            foreach (double value in weights)
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1) return false;
                total += value;
            }
            return Math.Abs(total - 1) < 1e-9;
        }

        private static bool Equal(double[] left, double[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int index = 0; index < left.Length; index++) if (left[index] != right[index]) return false;
            return true;
        }

        private static RarityException Replaced() { return new RarityException("概率表已失效或更换；未写入。请重新读取；如需清除旧改动，请重启游戏。", "The probability table changed or became unavailable. Nothing was written. Read again, or restart the game to clear earlier changes."); }
        private static RarityException Changed() { return new RarityException("游戏重新计算了概率；未继续写入。请重启游戏再试。", "The game recalculated its odds. Writing was stopped. Restart the game before trying again."); }
        private static RarityException Uncertain() { return new RarityException("无法确认恢复，已停止写入。请退出到桌面并重启游戏。", "Rollback could not be verified. Writing is disabled. Quit to desktop and restart the game."); }

        private sealed class Lookup
        {
            internal readonly long Table;
            internal readonly long[] Slots;
            internal readonly double[] Values;
            internal Lookup(long table, long[] slots, double[] values) { Table = table; Slots = slots; Values = values; }
            internal bool Same(Lookup other)
            {
                if (other == null || Table != other.Table) return false;
                for (int index = 0; index < Slots.Length; index++) if (Slots[index] != other.Slots[index]) return false;
                return true;
            }
        }
    }

    internal sealed class RaritySession : IDisposable
    {
        private readonly ProcessMemory memory;
        private readonly OddsTable odds;
        private RaritySession(ProcessMemory memory) { this.memory = memory; odds = new OddsTable(memory); }
        internal static RaritySession Connect() { return new RaritySession(ProcessMemory.Connect()); }
        internal bool HasOriginal { get { return odds.HasOriginal; } }
        internal int ProcessId { get { return memory.ProcessId; } }
        internal bool HasExited { get { return memory.HasExited; } }
        internal double[] ReadWeights() { return odds.Read(); }
        internal double[] Apply(double[] weights) { return odds.Apply(weights); }
        internal double[] Restore() { return odds.Restore(); }
        public void Dispose() { memory.Dispose(); }
    }

    internal sealed class ProcessMemory : IOddsMemory, IDisposable
    {
        private const uint ReadAccess = 0x100410;
        private const uint WriteAccess = 0x100428;
        private static readonly string[] Files = { "Slotbound.exe", "Slotbound.dat", "lua51.dll" };
        private static readonly string[] Hashes = {
            "2650C2ED3D4CE224D446EF2B6336D239B4421133232154E5E93E568464604CCA",
            "50B013DD401DB5169BBBC01ABB0FF429610F9EDBF4D0338CF9B4589948A7BC7B",
            "94FB3E3D4B1F6ACCE0110E46AADF1ECAB1FA17C4ED1AB34CAEF3C5C2121C208E"
        };
        private readonly int pid;
        private readonly long created;
        private IntPtr reader;
        private IntPtr writer;

        private ProcessMemory(int pid, IntPtr handle, long created) { this.pid = pid; reader = handle; this.created = created; }
        internal int ProcessId { get { return pid; } }
        internal bool HasExited { get { return reader == IntPtr.Zero || WaitForSingleObject(reader, 0) != 258; } }

        internal static ProcessMemory Connect()
        {
            if (IntPtr.Size != 8) throw new RarityException("需要 64 位版本。", "Use the 64-bit application.");
            Process[] games = Process.GetProcessesByName("Slotbound");
            int id = 0;
            foreach (Process process in games) { id = process.Id; process.Dispose(); }
            if (games.Length != 1) throw new RarityException("请只打开一个 Slotbound 游戏窗口。", "Start exactly one Slotbound game instance.");
            IntPtr handle = OpenProcess(ReadAccess, false, id);
            if (handle == IntPtr.Zero) throw AccessError();
            try
            {
                var image = new StringBuilder(32768);
                int size = image.Capacity;
                long created, exited, kernel, user;
                if (!QueryFullProcessImageName(handle, 0, image, ref size) || !GetProcessTimes(handle, out created, out exited, out kernel, out user)) throw AccessError();
                string path = image.ToString();
                if (!string.Equals(Path.GetFileName(path), Files[0], StringComparison.OrdinalIgnoreCase)) throw Unsupported();
                using (var sha = SHA256.Create())
                {
                    for (int index = 0; index < Files.Length; index++)
                    {
                        string file = index == 0 ? path : Path.Combine(Path.GetDirectoryName(path), Files[index]);
                        using (var stream = File.OpenRead(file))
                        {
                            string hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
                            if (hash != Hashes[index]) throw Unsupported();
                        }
                    }
                }
                return new ProcessMemory(id, handle, created);
            }
            catch (IOException) { CloseHandle(handle); throw Unsupported(); }
            catch { CloseHandle(handle); throw; }
        }

        public byte[] Read(long address, int length)
        {
            if (address <= 0 || length <= 0 || reader == IntPtr.Zero) return null;
            byte[] data = new byte[length];
            UIntPtr read;
            return ReadProcessMemory(reader, new IntPtr(address), data, new UIntPtr((uint)length), out read) && read.ToUInt64() == (ulong)length ? data : null;
        }

        public IEnumerable<OddsChunk> Chunks()
        {
            const int chunkSize = 4 * 1024 * 1024;
            long offset = 0;
            MemoryInfo info;
            var infoSize = new UIntPtr((uint)Marshal.SizeOf(typeof(MemoryInfo)));
            while (!HasExited && VirtualQueryEx(reader, new IntPtr(offset), out info, infoSize) != UIntPtr.Zero)
            {
                long start = info.BaseAddress.ToInt64(), size = info.RegionSize.ToInt64();
                long end = start + size;
                if (size <= 0 || end <= offset) yield break;
                if (info.State == 0x1000 && info.Kind == 0x20000 && info.Protect == 4)
                {
                    for (long addr = start; addr < end; addr += chunkSize)
                    {
                        byte[] data = Read(addr, (int)Math.Min(chunkSize + 32, end - addr));
                        if (data != null) yield return new OddsChunk(addr, data);
                    }
                }
                offset = end;
            }
        }

        public bool WriteNumber(long address, double value)
        {
            if (HasExited || (address & 7) != 0) return false;
            if (writer == IntPtr.Zero)
            {
                IntPtr handle = OpenProcess(WriteAccess, false, pid);
                long timestamp, exited, kernel, user;
                if (handle == IntPtr.Zero) throw AccessError();
                if (!GetProcessTimes(handle, out timestamp, out exited, out kernel, out user) || timestamp != created)
                {
                    CloseHandle(handle);
                    throw AccessError();
                }
                writer = handle;
            }
            UIntPtr written;
            return WriteProcessMemory(writer, new IntPtr(address), BitConverter.GetBytes(value), new UIntPtr(8), out written) && written.ToUInt64() == 8;
        }

        public void Dispose()
        {
            if (reader != IntPtr.Zero) { CloseHandle(reader); reader = IntPtr.Zero; }
            if (writer != IntPtr.Zero) { CloseHandle(writer); writer = IntPtr.Zero; }
        }

        private static RarityException AccessError() { return new RarityException("无法访问 Slotbound 进程。请确认工具和游戏使用同一 Windows 用户及权限。", "Cannot access the Slotbound process. Run the tool and game as the same Windows user and privilege level."); }
        private static RarityException Unsupported() { return new RarityException("不支持此版本；只支持已核验的 Steam 构建 26791687，未写入任何数据。", "Unsupported game build. Only the verified Steam build 26791687 is supported. Nothing was written."); }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryInfo
        {
            internal IntPtr BaseAddress, AllocationBase;
            internal uint AllocationProtect;
            internal IntPtr RegionSize;
            internal uint State, Protect, Kind;
        }

        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(IntPtr handle, uint timeout);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetProcessTimes(IntPtr handle, out long created, out long exited, out long kernel, out long user);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool QueryFullProcessImageName(IntPtr handle, int flags, StringBuilder path, ref int length);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern UIntPtr VirtualQueryEx(IntPtr handle, IntPtr address, out MemoryInfo info, UIntPtr size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool ReadProcessMemory(IntPtr handle, IntPtr address, [Out] byte[] result, UIntPtr size, out UIntPtr read);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool WriteProcessMemory(IntPtr handle, IntPtr address, byte[] data, UIntPtr size, out UIntPtr written);
    }
}
