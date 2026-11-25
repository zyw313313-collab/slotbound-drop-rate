using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SlotboundDropRate
{
    internal sealed class BuildCompatibility
    {
        internal const string ProfileName = "slotbound-compatibility.ini";
        internal readonly string Build;
        internal readonly string Version;
        internal readonly string DataHash;

        private BuildCompatibility(string build, string version, string dataHash)
        {
            Build = build;
            Version = version;
            DataHash = dataHash;
        }

        internal static BuildCompatibility Load(string directory)
        {
            return ParseProfile(ReadSmall(Path.Combine(directory, ProfileName), 4096));
        }

        internal static BuildCompatibility ParseProfile(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 4096) return null;
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                    int separator = line.IndexOf('=');
                    if (separator <= 0) return null;
                    string name = line.Substring(0, separator).Trim();
                    if (entries.ContainsKey(name)) return null;
                    entries.Add(name, line.Substring(separator + 1).Trim());
                }
            }
            string schema, locator, version, build, dataHash;
            if (entries.Count != 5 || !entries.TryGetValue("schema", out schema) || schema != "1"
                || !entries.TryGetValue("locator", out locator) || locator != "lua-gc64-v1"
                || !entries.TryGetValue("version", out version) || !Regex.IsMatch(version, @"\A[0-9]{1,5}(?:\.[0-9]{1,5}){2}\z")
                || !entries.TryGetValue("build", out build) || !IsBuildId(build)
                || !entries.TryGetValue("dataSha256", out dataHash) || !Regex.IsMatch(dataHash, @"\A[0-9A-Fa-f]{64}\z")) return null;
            return new BuildCompatibility(build, version, dataHash);
        }

        internal static bool CanWrite(BuildCompatibility profile, string build, string dataHash)
        {
            return profile != null && profile.Build == build && HashEquals(profile.DataHash, dataHash);
        }

        internal static bool CanInspect(string gameHash, string luaHash, string expectedGameHash, string expectedLuaHash)
        {
            return HashEquals(expectedGameHash, gameHash) && HashEquals(expectedLuaHash, luaHash);
        }

        internal static string FindSteamBuild(string gamePath)
        {
            DirectoryInfo installation = new FileInfo(gamePath).Directory;
            DirectoryInfo common = installation == null ? null : installation.Parent;
            DirectoryInfo steamapps = common == null ? null : common.Parent;
            if (steamapps == null || !string.Equals(steamapps.Name, "steamapps", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(common.Name, "common", StringComparison.OrdinalIgnoreCase)) return null;
            return ParseSteamBuild(ReadSmall(Path.Combine(steamapps.FullName, "appmanifest_4459590.acf"), 64 * 1024), installation.Name);
        }

        internal static string ParseSteamBuild(string manifest, string installDirectory)
        {
            if (string.IsNullOrEmpty(manifest) || manifest.Length > 64 * 1024 || string.IsNullOrEmpty(installDirectory)) return null;
            if (Field(manifest, "appid") != "4459590" || !string.Equals(Field(manifest, "installdir"), installDirectory, StringComparison.OrdinalIgnoreCase)) return null;
            string build = Field(manifest, "buildid");
            return IsBuildId(build) ? build : null;
        }

        private static bool IsBuildId(string build)
        {
            return build != null && Regex.IsMatch(build, @"\A[1-9][0-9]{0,11}\z");
        }

        private static bool HashEquals(string expected, string actual)
        {
            return !string.IsNullOrEmpty(expected) && string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
        }

        private static string Field(string manifest, string name)
        {
            var matches = Regex.Matches(manifest, "(?mi)^[ \\t]*\"" + name + "\"[ \\t]+\"([^\"\\r\\n]+)\"[ \\t]*\\r?$");
            return matches.Count == 1 ? matches[0].Groups[1].Value : null;
        }

        private static string ReadSmall(string path, int limit)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                using (var reader = new StreamReader(stream))
                    return stream.Length <= limit ? reader.ReadToEnd() : null;
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }
    }
}
