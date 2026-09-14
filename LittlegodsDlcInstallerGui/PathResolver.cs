using System;
using System.IO;

namespace LittlegodsDlcInstallerGui;

public static class PathResolver
{
    public static string GetDefaultInstallPathForCurrentOS()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Plutonium", "storage", "t6");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "Plutonium", "storage", "t6");
    }
}
