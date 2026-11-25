using System;
using System.IO;
using System.Text;

namespace SlotboundDropRate
{
    internal sealed class ProfileSetup
    {
        internal const string CandidateName = "slotbound-compatibility.candidate.ini";
        private readonly string directory;
        private readonly string content;

        private ProfileSetup(string directory, string content)
        {
            this.directory = directory;
            this.content = content;
        }

        internal static ProfileSetup Prepare(string directory, string version, string build, string dataHash)
        {
            string content = string.Join(Environment.NewLine, new[] {
                "# Locally generated, not verified by the project.",
                "schema=1", "locator=lua-gc64-v1", "version=" + version,
                "build=" + build, "dataSha256=" + dataHash, ""
            });
            if (BuildCompatibility.ParseProfile(content) == null)
                throw new RarityException("游戏版本、Steam 构建号或哈希无效。请输入类似 1.0.5 的版本。",
                    "Invalid game version, Steam build, or hash. Enter a version such as 1.0.5.");
            var candidate = new ProfileSetup(directory, content);
            candidate.Save(CandidateName, null);
            return candidate;
        }

        internal string Activate()
        {
            string backupName = "slotbound-compatibility.backup-" + Guid.NewGuid().ToString("N") + ".ini";
            return Save(BuildCompatibility.ProfileName, backupName);
        }

        private string Save(string name, string backupName)
        {
            string target = Path.Combine(directory, name);
            string backup = backupName == null ? null : Path.Combine(directory, backupName);
            string pending = Path.Combine(directory, ".slotbound-profile-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(file, new UTF8Encoding(false))) writer.Write(content);
                if (File.Exists(target))
                {
                    File.Replace(pending, target, backup);
                    return backup;
                }
                File.Move(pending, target);
                return null;
            }
            finally
            {
                if (File.Exists(pending)) File.Delete(pending);
            }
        }
    }
}
