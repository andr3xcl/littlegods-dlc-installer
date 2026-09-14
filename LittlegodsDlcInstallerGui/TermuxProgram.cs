using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LittlegodsDlcInstallerGui;

public static class TermuxProgram
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Littlegods DLC Installer - Termux");
        Console.WriteLine("Discord: discord.littlegod.space");
        Console.WriteLine();

        var operation = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
        var target = GetOption(args, "--path") ?? GetOption(args, "-p");

        if (operation is "help" or "--help" or "-h")
        {
            PrintUsage();
            return 0;
        }

        if (operation is not ("install" or "repair" or "uninstall" or "manifest"))
        {
            Console.Error.WriteLine("Operacion desconocida.");
            PrintUsage();
            return 2;
        }

        if (target is null && operation != "manifest")
        {
            Console.Error.WriteLine("Falta la ruta: usa --path /ruta/Plutonium/storage/t6");
            return 2;
        }

        if (target is not null && !HasRequiredPath(target))
        {
            Console.Error.WriteLine("La ruta debe contener Plutonium/storage/t6.");
            return 2;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
            Console.WriteLine("Cancelando y limpiando archivos nuevos...");
        };

        var service = new InstallerService();
        try
        {
            if (operation == "manifest")
            {
                var files = await service.GetManifestAsync(cancellation.Token);
                Console.WriteLine($"Archivos configurados: {files.Count}");
                for (var index = 0; index < files.Count; index++)
                {
                    Console.WriteLine($"{index + 1}. {files[index]}");
                }
                return 0;
            }

            Directory.CreateDirectory(target!);
            Action<DownloadProgress> progress = value =>
            {
                var percent = value.CurrentFileFraction > 0
                    ? value.CurrentFileFraction * 100
                    : value.TotalFiles == 0 ? 0 : value.CompletedFiles * 100.0 / value.TotalFiles;
                Console.Write($"\r{value.CompletedFiles}/{value.TotalFiles} archivos - {percent:0.0}% - {value.CurrentFile}          ");
            };

            var status = new Action<string>(message => Console.WriteLine($"\n{message}"));
            var result = operation switch
            {
                "install" => await service.InstallAsync(target!, status, progress, cancellation.Token),
                "repair" => await service.RepairAsync(target!, status, progress, cancellation.Token),
                _ => await service.UninstallAsync(target!, status, cancellation.Token)
            };

            Console.WriteLine($"\nOperacion completada. Archivos: {result}");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nOperacion cancelada. Los archivos nuevos fueron eliminados.");
            return 130;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"\nError: {error.Message}");
            return 1;
        }
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static bool HasRequiredPath(string path)
    {
        var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index <= segments.Length - 3; index++)
        {
            if (segments[index].Equals("Plutonium", StringComparison.OrdinalIgnoreCase)
                && segments[index + 1].Equals("storage", StringComparison.OrdinalIgnoreCase)
                && segments[index + 2].Equals("t6", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Uso:");
        Console.WriteLine("  dotnet run -- install --path /ruta/Plutonium/storage/t6");
        Console.WriteLine("  dotnet run -- repair --path /ruta/Plutonium/storage/t6");
        Console.WriteLine("  dotnet run -- uninstall --path /ruta/Plutonium/storage/t6");
        Console.WriteLine("  dotnet run -- manifest");
        Console.WriteLine();
        Console.WriteLine("Pulsa Ctrl+C para cancelar y borrar los archivos nuevos.");
    }
}
