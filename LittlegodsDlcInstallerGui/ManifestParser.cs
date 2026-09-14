using System;
using System.Collections.Generic;
using System.Linq;

namespace LittlegodsDlcInstallerGui;

public static class ManifestParser
{
    public static List<string> Parse(string manifestText)
    {
        if (string.IsNullOrWhiteSpace(manifestText))
        {
            return new List<string>();
        }

        return manifestText
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
            .Select(line => line.Replace('\\', '/').Trim('/'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }
}
