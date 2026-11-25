using System;
using SlotboundDropRate;

internal static class CompatibilityTests
{
    private static readonly string GameHash = new string('A', 64);
    private static readonly string LuaHash = new string('B', 64);
    private static readonly string DataHash = new string('C', 64);
    private static readonly string Profile = "schema=1\nlocator=lua-gc64-v1\nversion=1.0.4\nbuild=26899345\ndataSha256=" + DataHash;

    internal static void Run()
    {
        var profile = BuildCompatibility.ParseProfile(Profile);
        Check(profile != null, "valid profile");
        Check(BuildCompatibility.ParseProfile(Profile.Replace("schema=1", "schema=2")) == null, "unknown schema");
        Check(BuildCompatibility.ParseProfile(Profile.Replace("gc64-v1", "gc64-v2")) == null, "unknown locator");
        Check(BuildCompatibility.ParseProfile(Profile + "\nbuild=26899346") == null, "duplicate build");
        Check(BuildCompatibility.ParseProfile(Profile + "\naddress=12345") == null, "unexpected write parameter");
        Check(BuildCompatibility.ParseProfile(Profile.Replace(DataHash, "short")) == null, "invalid hash");
        Check(!BuildCompatibility.CanWrite(null, "26899345", DataHash), "missing profile is read-only");
        Check(!BuildCompatibility.CanWrite(profile, "26848798", DataHash), "old build is read-only");
        Check(!BuildCompatibility.CanWrite(profile, null, DataHash), "unknown build is read-only");
        Check(!BuildCompatibility.CanWrite(profile, "26899345", LuaHash), "changed data is read-only");
        Check(BuildCompatibility.CanWrite(profile, "26899345", DataHash.ToLowerInvariant()), "exact current build enables writes");
        Check(BuildCompatibility.CanInspect(GameHash, LuaHash, GameHash, LuaHash), "known runtime permits diagnostics");
        Check(!BuildCompatibility.CanInspect(DataHash, LuaHash, GameHash, LuaHash), "unknown executable blocks diagnostics");
        Check(!BuildCompatibility.CanInspect(GameHash, DataHash, GameHash, LuaHash), "unknown Lua runtime blocks diagnostics");
        const string manifest = "\"AppState\"\n{\n \"appid\" \"4459590\"\n \"installdir\" \"Slotbound\"\n \"buildid\" \"26899345\"\n \"TargetBuildID\" \"26899999\"\n}";
        Check(BuildCompatibility.ParseSteamBuild(manifest, "Slotbound") == "26899345", "reads installed, not target build");
        Check(BuildCompatibility.ParseSteamBuild(manifest, "AnotherGame") == null, "manifest directory mismatch");
        Check(BuildCompatibility.ParseSteamBuild(manifest.Replace("4459590", "123"), "Slotbound") == null, "unrelated Steam app");
        Console.WriteLine("PASS latest build profile, read-only downgrade, runtime checks, Steam manifest");
    }

    private static void Check(bool result, string name)
    {
        if (!result) throw new Exception("Compatibility: " + name);
    }
}
