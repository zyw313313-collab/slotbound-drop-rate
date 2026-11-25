using System;
using System.Collections.Generic;
using System.Text;
using SlotboundDropRate;

internal static class RarityTests
{
    private static void Main()
    {
        CompatibilityTests.Run();
        var memory = Fixture();
        var session = new OddsTable(memory);
        Equal(session.Read()[4], 0.001, "stock prism");
        Equal(memory.Writes, 0, "read-only scan");
        Reject(() => session.Apply(new[] {0d, 0d, 0d, 0d, 0.99}), "invalid sum");
        Equal(memory.Writes, 0, "invalid input must not write");
        Equal(session.Apply(new[] {0d, 0d, 0d, 0d, 1d})[4], 1, "prism override");
        Equal(memory.WriteAddresses[0], 0x5000 + 24 * 4, "add positive weight before removing previous weight");
        Equal(memory.Values(0x6000, 4), 0.5, "decoy table remains untouched");
        Equal(session.Apply(new[] {0d, 0d, 0d, 1d, 0d})[3], 1, "legendary override");
        Equal(session.Restore()[4], 0.001, "restore original after repeated apply");
        memory.FailNext = 0x5000 + 24 * 2;
        Reject(() => session.Apply(new[] {0d, 0d, 0d, 0d, 1d}), "partial write");
        Equal(session.Read()[0], 0.8, "partial write rolled back");
        Equal(session.Read()[4], 0.001, "partial write preserved original");
        session.Apply(new[] {0d, 0d, 0d, 1d, 0d});
        memory.Node(0x4000, 0, memory.Tab(0x2600), memory.Str("probabilities"));
        Reject(() => session.Restore(), "restore against a different table");
        Equal(memory.Values(0x6000, 4), 0.5, "replacement preserved");
        var ambiguous = Fixture();
        ambiguous.Table(0x2900, 0x7000, 1);
        ambiguous.Node(0x7000, 0, ambiguous.Tab(0x2900), ambiguous.Str("_G"));
        ambiguous.Node(0x7000, 1, ambiguous.Tab(0x2100), ambiguous.Str("package"));
        Reject(() => new OddsTable(ambiguous).Read(), "ambiguous globals");
        Console.WriteLine("PASS rarity resolution, read-only scan, invalid input, both overrides, rollback, restore, ambiguous globals");
    }

    private static void Equal(double actual, double expected, string label)
    {
        if (Math.Abs(actual - expected) > 1e-10) throw new Exception(label + ": expected " + expected + ", got " + actual);
    }

    private static void Reject(Action action, string label)
    {
        try { action(); }
        catch (RarityException) { return; }
        throw new Exception(label + " unexpectedly succeeded");
    }

    private static Heap Fixture()
    {
        var h = new Heap();
        h.Table(0x2000, 0x3000, 1);
        h.Node(0x3000, 0, h.Tab(0x2000), h.Str("_G"));
        h.Node(0x3000, 1, h.Tab(0x2100), h.Str("package"));
        h.Table(0x2100, 0x3100, 1);
        h.Node(0x3100, 1, h.Tab(0x2200), h.Str("loaded"));
        h.Table(0x2200, 0x3200, 1);
        h.Node(0x3200, 0, h.Tab(0x2300), h.Str("src.data.constants"));
        h.Table(0x2300, 0x3300, 1);
        h.Node(0x3300, 1, h.Tab(0x2400), h.Str("unitGrades"));
        h.Table(0x2400, 0x4000, 1);
        h.Node(0x4000, 0, h.Tab(0x2500), h.Str("probabilities"));
        h.Table(0x2500, 0x5000, 7);
        h.Table(0x2600, 0x6000, 7);
        var names = new[] {"normal", "advanced", "rare", "legendary", "prism", "special"};
        var baseWeights = new[] {0.8, 0.15, 0.045, 0.004, 0.001, 0d};
        var decoyWeights = new[] {0.5, 0d, 0d, 0d, 0.5, 0d};
        for (int i = 0; i < names.Length; i++)
        {
            h.Node(0x5000, i, (ulong)BitConverter.DoubleToInt64Bits(baseWeights[i]), h.Str(names[i]));
            h.Node(0x6000, i, (ulong)BitConverter.DoubleToInt64Bits(decoyWeights[i]), h.Str(names[i]));
        }
        return h;
    }

    private sealed class Heap : IOddsMemory
    {
        private readonly byte[] bytes = new byte[0x8000];
        private readonly Dictionary<string, ulong> strings = new Dictionary<string, ulong>();
        private int stringAddress = 0x1100;
        public readonly List<long> WriteAddresses = new List<long>();
        public int Writes;
        public long FailNext;

        public byte[] Read(long address, int length)
        {
            if (address < 0x1000 || address + length > bytes.Length) return null;
            var copy = new byte[length];
            Buffer.BlockCopy(bytes, (int)address, copy, 0, length);
            return copy;
        }
        public IEnumerable<OddsChunk> Chunks()
        {
            yield return new OddsChunk(0x1000, Read(0x1000, bytes.Length - 0x1000));
        }
        public bool WriteNumber(long address, double value)
        {
            ++Writes;
            WriteAddresses.Add(address);
            if (address == FailNext) { FailNext = 0; return false; }
            BitConverter.GetBytes(value).CopyTo(bytes, (int)address);
            return true;
        }
        public double Values(int address, int index) { return BitConverter.ToDouble(bytes, address + index * 24); }
        public ulong Tab(int address) { return 0xFFFA000000000000UL | (ulong)(uint)address; }
        public void Table(int address, int nodes, uint mask)
        {
            bytes[address + 9] = 11;
            BitConverter.GetBytes((ulong)(uint)nodes).CopyTo(bytes, address + 40);
            BitConverter.GetBytes(mask).CopyTo(bytes, address + 52);
        }
        public ulong Str(string text)
        {
            ulong tagged;
            if (strings.TryGetValue(text, out tagged)) return tagged;
            int at = stringAddress;
            stringAddress += 64;
            bytes[at + 9] = 4;
            BitConverter.GetBytes(text.Length).CopyTo(bytes, at + 20);
            Encoding.ASCII.GetBytes(text).CopyTo(bytes, at + 24);
            return strings[text] = 0xFFFD800000000000UL | (ulong)(uint)at;
        }
        public void Node(int address, int index, ulong value, ulong key)
        {
            BitConverter.GetBytes(value).CopyTo(bytes, address + index * 24);
            BitConverter.GetBytes(key).CopyTo(bytes, address + index * 24 + 8);
        }
    }
}
