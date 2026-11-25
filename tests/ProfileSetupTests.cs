using System;
using System.IO;
using SlotboundDropRate;

internal static class ProfileSetupTests
{
    internal static void Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "slotbound-profile-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string active = Path.Combine(directory, BuildCompatibility.ProfileName);
            File.WriteAllText(active, "original profile");
            string hash = new string('D', 64);
            var candidate = ProfileSetup.Prepare(directory, "1.0.5", "26899346", hash);
            string candidatePath = Path.Combine(directory, "slotbound-compatibility.candidate.ini");
            string text = File.ReadAllText(candidatePath);
            if (!text.Contains("build=26899346") || !text.Contains("dataSha256=" + hash)
                || !text.Contains("version=1.0.5")) throw new Exception("Candidate content differs");
            if (File.ReadAllText(active) != "original profile") throw new Exception("Candidate overwrote the active profile");
            string backup = candidate.Activate();
            if (!Path.GetFileName(backup).StartsWith("slotbound-compatibility.backup-", StringComparison.Ordinal)
                || File.ReadAllText(backup) != "original profile") throw new Exception("Original profile was not backed up");
            if (File.ReadAllText(active) != text || File.ReadAllText(candidatePath) != text)
                throw new Exception("Activation must install the validated candidate and keep a copy");
            Reject(() => ProfileSetup.Prepare(directory, "1.0.5\nbuild=1", "26899346", hash));
            Reject(() => ProfileSetup.Prepare(directory, "1.0.5", null, hash));
            Reject(() => ProfileSetup.Prepare(directory, "1.0.5", "26899346", "invalid"));
            if (File.ReadAllText(active) != text || File.ReadAllText(candidatePath) != text)
                throw new Exception("Invalid input changed a profile");
            File.Delete(active);
            if (candidate.Activate() != null || File.ReadAllText(active) != text)
                throw new Exception("Activation without an old profile should not create a backup");
            Console.WriteLine("PASS profile candidate, validation, activation and backup");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (RarityException) { return; }
        throw new Exception("Invalid candidate was accepted");
    }
}
