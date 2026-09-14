using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LittlegodsDlcInstallerGui;

public sealed record DownloadProgress(
    int CompletedFiles,
    int TotalFiles,
    string CurrentFile,
    long BytesDownloaded = 0,
    long TotalBytes = 0,
    double CurrentFileFraction = 0);

public sealed record Beta1InstallationStatus(
    bool IsInstalled,
    bool IsInstalledInPlutonium,
    bool IsInstalledInGame);

public sealed class InstallerService
{
    private const string BaseUrl = "https://files.littlegods.space";
    private const string ManifestUrl = BaseUrl + "/manifest.txt";
    private readonly HttpClient _httpClient = new();

    public static IReadOnlyList<string> Beta1StorageFiles => Beta1Manifest.StorageFiles;
    public static IReadOnlyList<string> Beta1GameFiles => Beta1Manifest.GameFiles;

    public static Beta1InstallationStatus CheckBeta1Installation(string plutoniumDirectory, string? baseGameDirectory = null)
    {
        var plutoniumRoot = string.IsNullOrWhiteSpace(plutoniumDirectory) ? null : Path.GetFullPath(plutoniumDirectory);
        var gameRoot = string.IsNullOrWhiteSpace(baseGameDirectory) ? null : Path.GetFullPath(baseGameDirectory);

        var installedInPlutonium = plutoniumRoot is not null && CountManifestFiles(plutoniumRoot, Beta1StorageFiles) > 0;
        var installedInGame = gameRoot is not null && CountManifestFiles(gameRoot, Beta1GameFiles) > 0;

        return new Beta1InstallationStatus(installedInPlutonium || installedInGame, installedInPlutonium, installedInGame);
    }

    public async Task<int> UndoBeta1Async(
        string plutoniumDirectory,
        string? baseGameDirectory = null,
        Action<string>? onStatus = null,
        CancellationToken cancellationToken = default)
    {
        var status = CheckBeta1Installation(plutoniumDirectory, baseGameDirectory);
        if (!status.IsInstalled)
        {
            onStatus?.Invoke("No se detectó BETA1 instalad en la carpeta seleccionada.");
            return 0;
        }

        var removed = await RemoveBeta1Async(plutoniumDirectory, baseGameDirectory, onStatus, cancellationToken);
        var installed = await InstallAsync(plutoniumDirectory, onStatus, cancellationToken: cancellationToken);
        onStatus?.Invoke($"BETA1 revertido. Archivos eliminados: {removed}. Archivos reinstalados: {installed}.");
        return removed + installed;
    }

    public async Task<int> RemoveBeta1Async(
        string plutoniumDirectory,
        string? baseGameDirectory = null,
        Action<string>? onStatus = null,
        CancellationToken cancellationToken = default)
    {
        var status = CheckBeta1Installation(plutoniumDirectory, baseGameDirectory);
        if (!status.IsInstalled)
        {
            onStatus?.Invoke("No se detectó BETA1 instalada en las carpetas seleccionadas.");
            return 0;
        }

        var removed = 0;
        if (status.IsInstalledInPlutonium)
        {
            removed += await RemoveFilesForManifestAsync(plutoniumDirectory, Beta1StorageFiles, onStatus, cancellationToken);
        }

        if (status.IsInstalledInGame && !string.IsNullOrWhiteSpace(baseGameDirectory))
        {
            removed += await RemoveFilesForManifestAsync(baseGameDirectory, Beta1GameFiles, onStatus, cancellationToken);
        }

        return removed;
    }

    private static int CountManifestFiles(string rootDirectory, IEnumerable<string> relativePaths)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
        {
            return 0;
        }

        var found = 0;
        foreach (var relativePath in relativePaths)
        {
            var fullPath = Path.Combine(rootDirectory, Normalize(relativePath));
            if (File.Exists(fullPath))
            {
                found++;
            }
        }

        return found;
    }

    private static Task<int> RemoveFilesForManifestAsync(
        string baseDirectory,
        IEnumerable<string> relativePaths,
        Action<string>? onStatus,
        CancellationToken cancellationToken)
    {
        var removed = 0;
        foreach (var relativePath in relativePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = Path.Combine(baseDirectory, Normalize(relativePath));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                removed++;
                onStatus?.Invoke($"Removed: {relativePath}");
            }
        }

        return Task.FromResult(removed);
    }

    public async Task<List<string>> DownloadManifestAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(ManifestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        return ManifestParser.Parse(text);
    }

    public Task<List<string>> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        return DownloadManifestAsync(cancellationToken);
    }

    public async Task<int> GetInstalledManifestFileCountAsync(
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        var files = await DownloadManifestAsync(cancellationToken);
        return files.Count(relativePath => File.Exists(Path.Combine(targetDirectory, Normalize(relativePath))));
    }

    public async Task<int> InstallAsync(
        string targetDirectory,
        Action<string>? onStatus = null,
        Action<DownloadProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var files = await DownloadManifestAsync(cancellationToken);
        var installed = 0;
        var newFiles = new List<string>();
        onProgress?.Invoke(new DownloadProgress(0, files.Count, "Starting..."));

        try
        {
            foreach (var relativePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fullPath = Path.Combine(targetDirectory, Normalize(relativePath));
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                var remoteUrl = $"{BaseUrl}/{relativePath.Replace('\\', '/')}";
                await DownloadToFileAsync(remoteUrl, fullPath, relativePath, onStatus, onProgress, installed, files.Count, cancellationToken, newFiles);
                installed++;
                onProgress?.Invoke(new DownloadProgress(installed, files.Count, relativePath));
            }
        }
        catch (OperationCanceledException)
        {
            RemoveNewFiles(newFiles);
            throw;
        }

        return installed;
    }

    public async Task<int> RepairAsync(
        string targetDirectory,
        Action<string>? onStatus = null,
        Action<DownloadProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var files = await DownloadManifestAsync(cancellationToken);
        var repaired = 0;
        var newFiles = new List<string>();
        onProgress?.Invoke(new DownloadProgress(0, files.Count, "Checking files..."));

        try
        {
            var processed = 0;
            foreach (var relativePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fullPath = Path.Combine(targetDirectory, Normalize(relativePath));
                var remoteUrl = $"{BaseUrl}/{relativePath.Replace('\\', '/')}";

                if (!File.Exists(fullPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                    await DownloadToFileAsync(remoteUrl, fullPath, relativePath, onStatus, onProgress, processed, files.Count, cancellationToken, newFiles);
                    repaired++;
                    processed++;
                    onProgress?.Invoke(new DownloadProgress(processed, files.Count, relativePath));
                    continue;
                }

                var remoteSize = await TryGetRemoteSizeAsync(remoteUrl, cancellationToken);
                if (remoteSize.HasValue && new FileInfo(fullPath).Length != remoteSize.Value)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                    await DownloadToFileAsync(remoteUrl, fullPath, relativePath, onStatus, onProgress, processed, files.Count, cancellationToken, newFiles);
                    repaired++;
                }

                processed++;
                onProgress?.Invoke(new DownloadProgress(processed, files.Count, relativePath));
            }
        }
        catch (OperationCanceledException)
        {
            RemoveNewFiles(newFiles);
            throw;
        }

        return repaired;
    }

    public async Task<int> UninstallAsync(string targetDirectory, Action<string>? onStatus = null, CancellationToken cancellationToken = default)
    {
        var files = await DownloadManifestAsync(cancellationToken);
        var removed = 0;

        foreach (var relativePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = Path.Combine(targetDirectory, Normalize(relativePath));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                removed++;
            }

            onStatus?.Invoke($"Removed: {relativePath}");
        }

        foreach (var directory in Directory.GetDirectories(targetDirectory, "*", SearchOption.AllDirectories)
                     .OrderByDescending(d => d.Count(c => c == Path.DirectorySeparatorChar)))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }

        if (Directory.Exists(targetDirectory) && !Directory.EnumerateFileSystemEntries(targetDirectory).Any())
        {
            Directory.Delete(targetDirectory);
        }

        return removed;
    }

    private static string Normalize(string relativePath)
    {
        return relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
    }

    private async Task DownloadToFileAsync(
        string remoteUrl,
        string localPath,
        string relativePath,
        Action<string>? onStatus,
        Action<DownloadProgress>? onProgress,
        int completedFiles,
        int totalFiles,
        CancellationToken cancellationToken,
        ICollection<string> newFiles)
    {
        onStatus?.Invoke($"Downloading: {localPath}");
        var tempPath = localPath + ".part";

        using var response = await _httpClient.GetAsync(remoteUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[64 * 1024];
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;
                int bytesRead;
                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

                while ((bytesRead = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await stream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    downloadedBytes += bytesRead;
                    var fraction = totalBytes > 0 ? (double)downloadedBytes / totalBytes : 0;
                    onProgress?.Invoke(new DownloadProgress(completedFiles, totalFiles, relativePath, downloadedBytes, totalBytes, fraction));
                }
            }

            var existed = File.Exists(localPath);
            if (existed)
            {
                File.Delete(localPath);
            }

            File.Move(tempPath, localPath);
            if (!existed)
            {
                newFiles.Add(localPath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private async Task<long?> TryGetRemoteSizeAsync(string remoteUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, remoteUrl);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength is long size)
            {
                return size;
            }
        }
        catch
        {
            // fallback
        }

        return null;
    }

    private static void RemoveNewFiles(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
