using System;
using System.IO;

namespace SFE.SimCore.Tests
{
    /// <summary>Findet Repo-Ordner (golden/, Leveldaten) – in dotnet test und im Unity Test Runner.</summary>
    internal static class TestPaths
    {
        public static string RepoRoot
        {
            get
            {
                foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
                {
                    var dir = new DirectoryInfo(start);
                    while (dir != null)
                    {
                        if (Directory.Exists(Path.Combine(dir.FullName, "golden")) &&
                            Directory.Exists(Path.Combine(dir.FullName, "sim-core")))
                            return dir.FullName;
                        dir = dir.Parent;
                    }
                }
                throw new DirectoryNotFoundException("Repo-Wurzel mit golden/ und sim-core/ nicht gefunden.");
            }
        }

        public static string Golden(string relative) => Path.Combine(RepoRoot, "golden", relative);

        public static string Data(string relative) =>
            Path.Combine(RepoRoot, "unity", "PinguinKolonie", "Assets", "_Project", "Resources", "Data", relative);

        public static string Read(string path) => File.ReadAllText(path);
    }
}
