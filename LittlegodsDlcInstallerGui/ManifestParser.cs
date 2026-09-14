using System;
using System.Collections.Generic;
using System.IO;
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

        var paths = manifestText
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
            .Select(line => line.Replace('\\', '/').Trim('/'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        foreach (var path in paths)
        {
            if (path.StartsWith("/", StringComparison.Ordinal)
                || path.StartsWith("\\", StringComparison.Ordinal)
                || path.Contains(":", StringComparison.Ordinal)
                || path.Split('/').Any(segment => segment is ".." or "."))
            {
                throw new InvalidDataException($"Manifest path is not a safe relative path: {path}");
            }
        }

        return paths;
    }
}
