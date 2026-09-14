using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace LittlegodsDlcInstallerGui;

public class MainWindow : Window
{
    private readonly InstallerService _service = new();
    private readonly TextBox _pathBox = new();
    private readonly ComboBox _languageCombo = new();
    private readonly ComboBox _themeCombo = new();
    private readonly Button _languageToggleButton = new();
    private readonly Button _themeToggleButton = new();
    private readonly TextBlock _logBox = new();
    private readonly TextBlock _titleText = new();
    private readonly TextBlock _pathLabel = new();
    private readonly TextBlock _linuxNote = new();
    private readonly TextBlock _languageTitle = new();
    private readonly TextBlock _themeTitle = new();
    private readonly TextBlock _progressText = new();
    private readonly TextBlock _discordText = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Image _logoImage = new();
    private readonly ScrollViewer _logScrollViewer = new();
    private readonly Button _browseButton = new();
    private readonly Button _manifestButton = new();
    private readonly Button _installButton = new();
    private readonly Button _repairButton = new();
    private readonly Button _uninstallButton = new();
    private readonly Button _beta1ManifestButton = new();
    private readonly Grid _root = new();
    private readonly List<Border> _themePanels = new();
    private readonly List<Button> _operationButtons = new();
    private readonly Button _cancelButton = new();
    private CancellationTokenSource? _operationCancellation;
    private bool _updatingLocalizedText;
    private string _currentLanguage = "es";
    private bool _hasValidPlutoniumPath;
    private bool _hasOriginalManifestFiles;

    public MainWindow()
    {
        Width = 900;
        Height = 620;
        MinWidth = 760;
        MinHeight = 560;
        Title = "Littlegods DLC Installer";
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        Background = new SolidColorBrush(Color.FromRgb(15, 16, 22));

        BuildLayout();
        LoadDefaultPath();
        _languageCombo.SelectedIndex = 0;
        _themeCombo.SelectedIndex = 1;
        ApplyThemeAppearance();
    }

    private void BuildLayout()
    {
        _root.Margin = new Thickness(22);
        _root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        _root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var header = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            Margin = new Thickness(0, 0, 0, 18)
        };
        _logoImage.Width = 92;
        _logoImage.Height = 92;
        _logoImage.Stretch = Stretch.Uniform;
        LoadLogo();
        header.Children.Add(_logoImage);

        _titleText.Text = "Littlegods DLC 5 Installer";
        _titleText.FontSize = 28;
        _titleText.FontWeight = FontWeight.Bold;
        _titleText.VerticalAlignment = VerticalAlignment.Center;
        _titleText.Margin = new Thickness(18, 0, 0, 0);
        _titleText.Foreground = Brushes.White;
        header.Children.Add(_titleText);
        Grid.SetColumn(_titleText, 1);

        _pathLabel.Text = "Plutonium path";
        _pathLabel.FontWeight = FontWeight.SemiBold;
        _pathLabel.Margin = new Thickness(0, 0, 0, 6);
        _pathLabel.Foreground = Brushes.White;

        var pathCard = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(28, 31, 39)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(58, 64, 78)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10)
        };
        _themePanels.Add(pathCard);

        var pathRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Margin = new Thickness(0)
        };

        _pathBox.Height = 38;
        _pathBox.Margin = new Thickness(0, 0, 10, 0);
        _pathBox.Text = string.Empty;
        _pathBox.PlaceholderText = "Ejemplo: /home/usuario/.local/share/Steam/.../Plutonium/storage/t6";
        _pathBox.Background = new SolidColorBrush(Color.FromRgb(18, 20, 28));
        _pathBox.Foreground = Brushes.White;
        _pathBox.BorderBrush = new SolidColorBrush(Color.FromRgb(88, 94, 111));
        _pathBox.CornerRadius = new CornerRadius(9);
        _pathBox.TextChanged += (_, _) =>
        {
            _hasValidPlutoniumPath = ContainsRequiredPlutoniumPath(_pathBox.Text?.Trim() ?? string.Empty);
            _hasOriginalManifestFiles = false;
            ApplyActionAvailability();
        };
        _pathBox.LostFocus += async (_, _) => await RefreshActionAvailabilityAsync();

        ConfigureButton(_browseButton, "Browse", Color.FromRgb(78, 136, 255), 130);
        _browseButton.Click += BrowseButton_Click;

        pathRow.Children.Add(_pathBox);
        Grid.SetColumn(_pathBox, 0);
        pathRow.Children.Add(_browseButton);
        Grid.SetColumn(_browseButton, 1);
        pathCard.Child = pathRow;

        _linuxNote.Text = OperatingSystem.IsLinux() ? "Linux: elige la carpeta manualmente." : "Windows: ruta predeterminada aplicada automáticamente.";
        _linuxNote.Foreground = Brushes.Orange;
        _linuxNote.Margin = new Thickness(0, 4, 0, 8);
        _linuxNote.FontSize = 12;

        _languageTitle.Text = "Language / Idioma / Idioma";
        _languageTitle.FontWeight = FontWeight.SemiBold;
        _languageTitle.Margin = new Thickness(0, 4, 0, 8);
        _languageTitle.Foreground = Brushes.White;

        var settingsCard = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(28, 31, 39)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(58, 64, 78)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12)
        };
        _themePanels.Add(settingsCard);

        var settingsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 18,
            Margin = new Thickness(0)
        };

        var languagePanel = new StackPanel { Spacing = 8, Width = 250 };
        _languageCombo.Width = 250;
        _languageCombo.Height = 40;
        _languageCombo.Background = new SolidColorBrush(Color.FromRgb(18, 20, 28));
        _languageCombo.Foreground = Brushes.White;
        _languageCombo.BorderBrush = new SolidColorBrush(Color.FromRgb(88, 94, 111));
        _languageCombo.CornerRadius = new CornerRadius(8);
        _languageCombo.IsVisible = true;
        _languageCombo.ItemsSource = new List<string> { "Español", "English", "Português" };
        _languageCombo.SelectedIndex = 0;
        _languageCombo.SelectionChanged += (_, _) =>
        {
            var selected = _languageCombo.SelectedIndex;
            _currentLanguage = selected switch
            {
                1 => "en",
                2 => "pt",
                _ => "es"
            };

            UpdateLocalizedText();
        };
        languagePanel.Children.Add(_languageTitle);
        languagePanel.Children.Add(_languageCombo);

        var themePanel = new StackPanel { Spacing = 8, Width = 250 };
        _themeTitle.Text = "Theme / Tema";
        _themeTitle.FontWeight = FontWeight.SemiBold;
        _themeTitle.Margin = new Thickness(0, 0, 0, 0);
        _themeTitle.Foreground = Brushes.White;
        _themeCombo.Width = 250;
        _themeCombo.Height = 40;
        _themeCombo.Background = new SolidColorBrush(Color.FromRgb(18, 20, 28));
        _themeCombo.Foreground = Brushes.White;
        _themeCombo.BorderBrush = new SolidColorBrush(Color.FromRgb(88, 94, 111));
        _themeCombo.CornerRadius = new CornerRadius(8);
        _themeCombo.IsVisible = true;
        _themeCombo.ItemsSource = new List<string> { "Claro", "Oscuro", "Sistema" };
        _themeCombo.SelectedIndex = 1;
        _themeCombo.SelectionChanged += (_, _) =>
        {
            if (_updatingLocalizedText || Application.Current is null) return;

            var selected = _themeCombo.SelectedIndex;
            if (selected == 0)
            {
                Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
            }
            else if (selected == 1)
            {
                Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            }
            else
            {
                Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
            }

            ApplyThemeAppearance();
        };
        themePanel.Children.Add(_themeTitle);
        themePanel.Children.Add(_themeCombo);

        settingsRow.Children.Add(languagePanel);
        settingsRow.Children.Add(themePanel);
        settingsCard.Child = settingsRow;

        var logBackground = new SolidColorBrush(Color.FromRgb(18, 20, 28));

        var logBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(58, 64, 78)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12),
            Height = 150,
            MinHeight = 150,
            MaxHeight = 150,
            Background = logBackground,
            Child = _logScrollViewer
        };
        _logScrollViewer.Content = _logBox;
        _logScrollViewer.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
        _logScrollViewer.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
        _themePanels.Add(logBorder);

        _logBox.Text = "Ready";
        _logBox.TextWrapping = TextWrapping.Wrap;
        _logBox.Margin = new Thickness(0);
        _logBox.Foreground = Brushes.White;

        _progressText.Text = "Ready - 0 files";
        _progressText.Height = 20;
        _progressText.TextWrapping = TextWrapping.NoWrap;
        _progressText.TextTrimming = TextTrimming.CharacterEllipsis;
        _progressText.Margin = new Thickness(0, 0, 0, 6);
        _progressText.Foreground = Brushes.White;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 1;
        _progressBar.Value = 0;
        _progressBar.Height = 12;
        _progressBar.Margin = new Thickness(0, 0, 0, 12);

        var actionCard = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(28, 31, 39)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(58, 64, 78)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 12, 0, 0)
        };
        _themePanels.Add(actionCard);

        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(0)
        };

        ConfigureButton(_installButton, "Install", Color.FromRgb(27, 130, 92));
        ConfigureButton(_repairButton, "Repair", Color.FromRgb(245, 147, 30));
        ConfigureButton(_uninstallButton, "Uninstall", Color.FromRgb(199, 64, 64));
        ConfigureButton(_manifestButton, "Manifest", Color.FromRgb(78, 136, 255));
        ConfigureButton(_beta1ManifestButton, "Manifest BETA1", Color.FromRgb(78, 136, 255), 145);

        _cancelButton.Content = "Cancelar";
        _cancelButton.Width = 120;
        _cancelButton.Height = 38;
        _cancelButton.Background = new SolidColorBrush(Color.FromRgb(116, 58, 170));
        _cancelButton.Foreground = Brushes.White;
        _cancelButton.CornerRadius = new CornerRadius(8);
        _cancelButton.IsVisible = false;
        _cancelButton.Click += CancelButton_Click;
        _cancelButton.PointerEntered += (_, _) => _cancelButton.Background = new SolidColorBrush(Color.FromRgb(145, 82, 202));
        _cancelButton.PointerExited += (_, _) => _cancelButton.Background = new SolidColorBrush(Color.FromRgb(116, 58, 170));

        _installButton.Click += async (_, _) => await RunOperationAsync("install");
        _repairButton.Click += async (_, _) => await RunOperationAsync("repair");
        _uninstallButton.Click += async (_, _) => await RunOperationAsync("uninstall");
        _manifestButton.Click += ManifestButton_Click;
        _beta1ManifestButton.Click += Beta1ManifestButton_Click;

        _operationButtons.Add(_installButton);
        _operationButtons.Add(_repairButton);
        _operationButtons.Add(_uninstallButton);

        actionRow.Children.Add(_installButton);
        actionRow.Children.Add(_repairButton);
        actionRow.Children.Add(_uninstallButton);
        actionRow.Children.Add(_manifestButton);
        actionRow.Children.Add(_beta1ManifestButton);
        actionRow.Children.Add(_cancelButton);
        actionCard.Child = actionRow;

        var content = new StackPanel { Spacing = 0 };
        content.Children.Add(header);
        content.Children.Add(_pathLabel);
        content.Children.Add(pathCard);
        content.Children.Add(_linuxNote);
        content.Children.Add(settingsCard);
        content.Children.Add(_progressText);
        content.Children.Add(_progressBar);
        content.Children.Add(logBorder);
        content.Children.Add(actionCard);
        _discordText.Text = "discord.littlegod.space";
        _discordText.FontSize = 12;
        _discordText.HorizontalAlignment = HorizontalAlignment.Right;
        _discordText.Margin = new Thickness(0, 10, 2, 0);
        _discordText.Foreground = Brushes.Gray;
        content.Children.Add(_discordText);
        _root.Children.Add(content);

        Content = _root;
        UpdateLocalizedText();
    }

    private void LoadLogo()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "LittlegodsDlcInstaller.png"),
            Path.Combine(AppContext.BaseDirectory, "littlegods-dlc-installer.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "LittlegodsDlcInstallerGui", "LittlegodsDlcInstaller.png")
        };

        var logoPath = candidates.FirstOrDefault(File.Exists);
        if (logoPath is not null)
        {
            _logoImage.Source = new Bitmap(logoPath);
        }
    }

    private static void ConfigureButton(Button button, string text, Color color, double width = 120)
    {
        var normalBrush = new SolidColorBrush(color);
        var hoverBrush = new SolidColorBrush(Color.FromRgb(
            (byte)Math.Min(color.R + 28, 255),
            (byte)Math.Min(color.G + 28, 255),
            (byte)Math.Min(color.B + 28, 255)));

        button.Content = text;
        button.Width = width;
        button.Height = 38;
        button.Background = normalBrush;
        button.Foreground = Brushes.White;
        button.CornerRadius = new CornerRadius(8);
        button.PointerEntered += (_, _) => button.Background = hoverBrush;
        button.PointerExited += (_, _) => button.Background = normalBrush;
    }

    private static PathIcon CreateIcon(string path)
    {
        return new PathIcon
        {
            Data = StreamGeometry.Parse(path),
            Width = 22,
            Height = 22
        };
    }

    private void ApplyThemeAppearance()
    {
        var isDark = Application.Current?.RequestedThemeVariant == Avalonia.Styling.ThemeVariant.Dark;

        var bg = isDark ? new SolidColorBrush(Color.FromRgb(15, 16, 22)) : new SolidColorBrush(Color.FromRgb(244, 246, 250));
        var panelBrush = isDark ? new SolidColorBrush(Color.FromRgb(28, 31, 39)) : new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var border = isDark ? new SolidColorBrush(Color.FromRgb(58, 64, 78)) : new SolidColorBrush(Color.FromRgb(210, 216, 224));
        var text = isDark ? Brushes.White : Brushes.Black;

        Background = bg;
        _root.Background = bg;
        _titleText.Foreground = text;
        _pathLabel.Foreground = text;
        _languageTitle.Foreground = text;
        _themeTitle.Foreground = text;
        _linuxNote.Foreground = isDark ? Brushes.Orange : Brushes.DarkOrange;
        _logBox.Foreground = text;
        _progressText.Foreground = text;
        _discordText.Foreground = isDark ? Brushes.LightGray : Brushes.DimGray;

        foreach (var themePanel in _themePanels)
        {
            themePanel.Background = panelBrush;
            themePanel.BorderBrush = border;
        }

        _pathBox.Background = isDark ? new SolidColorBrush(Color.FromRgb(18, 20, 28)) : new SolidColorBrush(Color.FromRgb(255, 255, 255));
        _pathBox.Foreground = text;
        _pathBox.BorderBrush = border;
        _languageCombo.Background = isDark ? new SolidColorBrush(Color.FromRgb(18, 20, 28)) : new SolidColorBrush(Color.FromRgb(255, 255, 255));
        _languageCombo.Foreground = text;
        _themeCombo.Background = isDark ? new SolidColorBrush(Color.FromRgb(18, 20, 28)) : new SolidColorBrush(Color.FromRgb(255, 255, 255));
        _themeCombo.Foreground = text;
    }

    private void LoadDefaultPath()
    {
        if (OperatingSystem.IsWindows())
        {
            var defaultPath = PathResolver.GetDefaultInstallPathForCurrentOS();
            _pathBox.Text = defaultPath;
        }
        else
        {
            _pathBox.Text = string.Empty;
        }

        _ = RefreshActionAvailabilityAsync();
    }

    private async Task RefreshActionAvailabilityAsync()
    {
        if (_operationCancellation is not null)
        {
            return;
        }

        var target = _pathBox.Text?.Trim() ?? string.Empty;
        _hasValidPlutoniumPath = ContainsRequiredPlutoniumPath(target);
        _hasOriginalManifestFiles = false;
        ApplyActionAvailability();

        if (!_hasValidPlutoniumPath)
        {
            return;
        }

        try
        {
            var installedCount = await _service.GetInstalledManifestFileCountAsync(target);
            if (string.Equals(target, _pathBox.Text?.Trim(), StringComparison.Ordinal))
            {
                _hasOriginalManifestFiles = installedCount > 0;
                ApplyActionAvailability();
            }
        }
        catch
        {
            _hasOriginalManifestFiles = false;
            ApplyActionAvailability();
        }
    }

    private void ApplyActionAvailability()
    {
        if (_operationCancellation is not null)
        {
            foreach (var button in _operationButtons)
            {
                button.IsEnabled = false;
            }

            return;
        }

        _installButton.IsEnabled = _hasValidPlutoniumPath;
        _repairButton.IsEnabled = _hasValidPlutoniumPath && _hasOriginalManifestFiles;
        _uninstallButton.IsEnabled = _hasValidPlutoniumPath && _hasOriginalManifestFiles;
    }

    private string Text(string key)
    {
        return _currentLanguage switch
        {
            "en" => key switch
            {
                "path" => "Plutonium path", "linux" => "Linux: choose the folder manually.", "windows" => "Windows: default path applied automatically.", "pathRule" => "Required: Plutonium/storage/t6",
                "language" => "Language", "theme" => "Theme", "browse" => "Browse",
                "themeToggleTooltip" => "Switch light or dark theme", "languageToggleTooltip" => "Change language",
                "install" => "Install", "repair" => "Repair", "uninstall" => "Uninstall",
                "cancel" => "Cancel", "ready" => "Ready - 0 files", "selectPath" => "Select a valid Plutonium path",
                "invalidPath" => "The path must contain Plutonium/storage/t6.",
                "starting" => "Starting", "canceling" => "Cancelling operation...",
                "cancelled" => "Operation cancelled. Downloaded files were removed.",
                "folderTitle" => "Select the Plutonium folder", "downloading" => "Downloading",
                "removed" => "Removed", "error" => "Error", "completed" => "completed",
                "manifest" => "Manifest", "loadingManifest" => "Loading manifest...", "close" => "Close",
                "beta1Manifest" => "BETA1 manifest", "loadingBeta1Manifest" => "Loading BETA1 manifest...",
                "beta1InstallTitle" => "Install BETA2", "beta1Question" => "Did you install BETA1 before? If so, you can remove it before installing BETA2.",
                "beta1GameLabel" => "Only needed to undo BETA1: base game folder", "beta1GamePlaceholder" => "Base game root folder",
                "beta1GameFolderTitle" => "Select the base game folder", "beta1RemoveMissing" => "Select the base game folder to undo BETA1.",
                "beta1NotFound" => "No BETA1 installation was detected in the selected folders.", "beta1RemoveInstall" => "Undo BETA1 and install",
                "plutoniumManifestPrefix" => "Plutonium/storage/t6", "gameManifestPrefix" => "Base game",
                "folderPickerError" => "Folder picker not available", "folderSelected" => "Folder selected",
                "manifestCount" => "files configured", "emptyManifest" => "The manifest is empty.",
                _ => key
            },
            "pt" => key switch
            {
                "path" => "Caminho do Plutonium", "linux" => "Linux: escolha a pasta manualmente.", "windows" => "Windows: caminho predefinido aplicado automaticamente.", "pathRule" => "Obrigatório: Plutonium/storage/t6",
                "language" => "Idioma", "theme" => "Tema", "browse" => "Procurar",
                "themeToggleTooltip" => "Alternar tema claro ou escuro", "languageToggleTooltip" => "Mudar idioma",
                "install" => "Instalar", "repair" => "Reparar", "uninstall" => "Desinstalar",
                "cancel" => "Cancelar", "ready" => "Pronto - 0 ficheiros", "selectPath" => "Selecione um caminho válido do Plutonium",
                "invalidPath" => "O caminho tem de conter Plutonium/storage/t6.",
                "starting" => "A iniciar", "canceling" => "A cancelar a operação...",
                "cancelled" => "Operação cancelada. Os ficheiros transferidos foram removidos.",
                "folderTitle" => "Selecione a pasta do Plutonium", "downloading" => "A transferir",
                "removed" => "Removido", "error" => "Erro", "completed" => "concluído",
                "manifest" => "Manifesto", "loadingManifest" => "A carregar o manifesto...", "close" => "Fechar",
                "beta1Manifest" => "Manifesto BETA1", "loadingBeta1Manifest" => "A carregar o manifesto BETA1...",
                "beta1InstallTitle" => "Instalar BETA2", "beta1Question" => "Instalou a BETA1 anteriormente? Se sim, pode removê-la antes de instalar a BETA2.",
                "beta1GameLabel" => "Necessária apenas para desfazer a BETA1: pasta do jogo base", "beta1GamePlaceholder" => "Pasta raiz do jogo base",
                "beta1GameFolderTitle" => "Selecione a pasta do jogo base", "beta1RemoveMissing" => "Selecione a pasta do jogo base para desfazer a BETA1.",
                "beta1NotFound" => "Não foi encontrada uma instalação da BETA1 nas pastas selecionadas.", "beta1RemoveInstall" => "Desfazer BETA1 e instalar",
                "plutoniumManifestPrefix" => "Plutonium/storage/t6", "gameManifestPrefix" => "Jogo base",
                "folderPickerError" => "O seletor de pastas não está disponível", "folderSelected" => "Pasta selecionada",
                "manifestCount" => "ficheiros configurados", "emptyManifest" => "O manifesto está vazio.",
                _ => key
            },
            _ => key switch
            {
                "path" => "Ruta de Plutonium", "linux" => "Linux: elige la carpeta manualmente.", "windows" => "Windows: ruta predeterminada aplicada automáticamente.", "pathRule" => "Obligatorio: Plutonium/storage/t6",
                "language" => "Idioma", "theme" => "Tema", "browse" => "Examinar",
                "themeToggleTooltip" => "Cambiar entre tema claro y oscuro", "languageToggleTooltip" => "Cambiar idioma",
                "install" => "Instalar", "repair" => "Reparar", "uninstall" => "Desinstalar",
                "cancel" => "Cancelar", "ready" => "Listo - 0 archivos", "selectPath" => "Selecciona una ruta válida de Plutonium",
                "invalidPath" => "La ruta debe contener Plutonium/storage/t6.",
                "starting" => "Iniciando", "canceling" => "Cancelando operación...",
                "cancelled" => "Operación cancelada. Se eliminaron los archivos descargados.",
                "folderTitle" => "Selecciona la carpeta de Plutonium", "downloading" => "Descargando",
                "removed" => "Eliminado", "error" => "Error", "completed" => "completado",
                "manifest" => "Manifiesto", "loadingManifest" => "Cargando manifiesto...", "close" => "Cerrar",
                "beta1Manifest" => "Manifiesto BETA1", "loadingBeta1Manifest" => "Cargando manifiesto BETA1...",
                "beta1InstallTitle" => "Instalar BETA2", "beta1Question" => "¿Instalaste BETA1 anteriormente? Si es así, puedes eliminarla antes de instalar BETA2.",
                "beta1GameLabel" => "Solo necesaria para deshacer BETA1: carpeta del juego base", "beta1GamePlaceholder" => "Carpeta raíz del juego base",
                "beta1GameFolderTitle" => "Selecciona la carpeta base del juego", "beta1RemoveMissing" => "Selecciona la carpeta del juego base para deshacer BETA1.",
                "beta1NotFound" => "No se detectó ninguna instalación de BETA1 en las carpetas seleccionadas.", "beta1RemoveInstall" => "Deshacer beta1 e instalar",
                "plutoniumManifestPrefix" => "Plutonium/storage/t6", "gameManifestPrefix" => "Juego base",
                "folderPickerError" => "El selector de carpetas no está disponible", "folderSelected" => "Carpeta seleccionada",
                "manifestCount" => "archivos configurados", "emptyManifest" => "El manifiesto está vacío.",
                _ => key
            }
        };
    }

    private void UpdateLocalizedText()
    {
        _updatingLocalizedText = true;
        var selectedTheme = Math.Max(_themeCombo.SelectedIndex, 0);

        try
        {
        _titleText.Text = "Littlegods DLC 5 Installer";
        _pathLabel.Text = Text("path");
        _linuxNote.Text = $"{(OperatingSystem.IsLinux() ? Text("linux") : Text("windows"))} {Text("pathRule")}";
        _pathBox.PlaceholderText = _currentLanguage == "en"
            ? "Example: /home/user/.local/share/Steam/.../Plutonium/storage/t6"
            : _currentLanguage == "pt"
                ? "Exemplo: /home/utilizador/.local/share/Steam/.../Plutonium/storage/t6"
                : "Ejemplo: /home/usuario/.local/share/Steam/.../Plutonium/storage/t6";
        _languageTitle.Text = Text("language");
        _themeTitle.Text = Text("theme");
        _themeCombo.ItemsSource = _currentLanguage == "en"
            ? new List<string> { "Light", "Dark", "System" }
            : _currentLanguage == "pt"
                ? new List<string> { "Claro", "Escuro", "Sistema" }
                : new List<string> { "Claro", "Oscuro", "Sistema" };
        _themeCombo.SelectedIndex = selectedTheme;
        _browseButton.Content = Text("browse");
        _installButton.Content = Text("install");
        _repairButton.Content = Text("repair");
        _uninstallButton.Content = Text("uninstall");
        _manifestButton.Content = Text("manifest");
        _beta1ManifestButton.Content = Text("beta1Manifest");
        _cancelButton.Content = Text("cancel");
        if (_operationCancellation is null)
        {
            _progressText.Text = Text("ready");
        }
        }
        finally
        {
            _updatingLocalizedText = false;
            ApplyThemeAppearance();
        }
    }

    private async Task RunOperationAsync(string operation)
    {
        if (_operationCancellation is not null)
        {
            return;
        }

        var target = _pathBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            AppendLog(Text("selectPath"));
            return;
        }

        if (!ContainsRequiredPlutoniumPath(target))
        {
            AppendLog(Text("invalidPath"));
            return;
        }

        Directory.CreateDirectory(target);

        Beta1InstallChoice? beta1Choice = null;
        if (operation == "install")
        {
            beta1Choice = await ShowBeta1InstallDialogAsync();
            if (beta1Choice is null)
            {
                return;
            }

            if (beta1Choice.Value.RemoveBeta1)
            {
                if (string.IsNullOrWhiteSpace(beta1Choice.Value.GamePath))
                {
                    AppendLog(Text("beta1RemoveMissing"));
                    return;
                }

                var status = InstallerService.CheckBeta1Installation(target, beta1Choice.Value.GamePath);
                if (!status.IsInstalled)
                {
                    AppendLog(Text("beta1NotFound"));
                    return;
                }
            }
        }

        _operationCancellation = new CancellationTokenSource();
        SetOperationState(true);

        try
        {
            var operationText = operation switch
            {
                "install" => Text("install").ToLowerInvariant(),
                "repair" => Text("repair").ToLowerInvariant(),
                _ => Text("uninstall").ToLowerInvariant()
            };
            AppendLog($"{Text("starting")} {operationText}...");
            var cancellationToken = _operationCancellation.Token;
            switch (operation)
            {
                case "install":
                    var installed = beta1Choice!.Value.RemoveBeta1
                        ? await _service.UndoBeta1Async(target, beta1Choice.Value.GamePath, AppendLog, cancellationToken)
                        : await _service.InstallAsync(target, AppendLog, UpdateProgress, cancellationToken);
                    AppendLog($"{Text("install")} {Text("completed")}: {installed}");
                    break;
                case "repair":
                    var repaired = await _service.RepairAsync(target, AppendLog, UpdateProgress, cancellationToken);
                    AppendLog($"{Text("repair")} {Text("completed")}: {repaired}");
                    break;
                case "uninstall":
                    var removed = await _service.UninstallAsync(target, AppendLog, cancellationToken: cancellationToken);
                    AppendLog($"{Text("uninstall")} {Text("completed")}: {removed}");
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            AppendLog(Text("cancelled"));
        }
        catch (Exception ex)
        {
            AppendLog(Text("error") + ": " + ex.Message);
        }
        finally
        {
            ResetProgress();
            _operationCancellation.Dispose();
            _operationCancellation = null;
            SetOperationState(false);
        }
    }

    private readonly record struct Beta1InstallChoice(bool RemoveBeta1, string? GamePath);

    private async Task<Beta1InstallChoice?> ShowBeta1InstallDialogAsync()
    {
        var dialog = new Window
        {
            Title = Text("beta1InstallTitle"),
            Width = 680,
            MinWidth = 560,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Background,
            CanResize = false
        };

        var gameBox = new TextBox
        {
            Height = 38,
            PlaceholderText = Text("beta1GamePlaceholder"),
            Background = new SolidColorBrush(Color.FromRgb(18, 20, 28)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(88, 94, 111)),
            CornerRadius = new CornerRadius(8)
        };
        var errorText = new TextBlock
        {
            Foreground = Brushes.OrangeRed,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var gameBrowse = new Button();
        ConfigureButton(gameBrowse, "Examinar", Color.FromRgb(78, 136, 255), 120);
        gameBrowse.Click += async (_, _) =>
        {
            var path = await PickFolderAsync(Text("beta1GameFolderTitle"));
            if (path is not null)
            {
                gameBox.Text = path;
            }
        };

        var gameRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 10
        };
        gameRow.Children.Add(gameBox);
        gameRow.Children.Add(gameBrowse);
        Grid.SetColumn(gameBrowse, 1);

        var cancelButton = new Button();
        ConfigureButton(cancelButton, Text("cancel"), Color.FromRgb(90, 95, 108), 120);
        cancelButton.Click += (_, _) => dialog.Close();

        var installButton = new Button();
        ConfigureButton(installButton, Text("install"), Color.FromRgb(27, 130, 92), 120);
        var removeButton = new Button();
        ConfigureButton(removeButton, Text("beta1RemoveInstall"), Color.FromRgb(245, 147, 30), 190);
        Beta1InstallChoice? result = null;
        installButton.Click += (_, _) =>
        {
            result = new Beta1InstallChoice(false, null);
            dialog.Close();
        };
        removeButton.Click += (_, _) =>
        {
            var gamePath = gameBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gamePath))
            {
                errorText.Text = Text("beta1RemoveMissing");
                return;
            }

            result = new Beta1InstallChoice(true, gamePath);
            dialog.Close();
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(0, 18, 0, 0)
        };
        buttons.Children.Add(cancelButton);
        buttons.Children.Add(installButton);
        buttons.Children.Add(removeButton);

        var content = new StackPanel
        {
            Margin = new Thickness(22),
            Spacing = 8
        };
        content.Children.Add(new TextBlock
        {
            Text = Text("beta1Question"),
            Foreground = Foreground,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });
        content.Children.Add(new TextBlock { Text = Text("beta1GameLabel"), Foreground = Foreground, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 8, 0, 0) });
        content.Children.Add(gameRow);
        content.Children.Add(errorText);
        content.Children.Add(buttons);
        dialog.Content = content;

        await dialog.ShowDialog(this);
        return result;
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        try
        {
            var folder = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });

            return folder.FirstOrDefault()?.Path.LocalPath;
        }
        catch (Exception ex)
        {
            AppendLog(Text("folderPickerError") + ": " + ex.Message);
            return null;
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_operationCancellation is not null)
        {
            _cancelButton.IsEnabled = false;
            AppendLog(Text("canceling"));
            ResetProgress();
            _operationCancellation.Cancel();
        }
    }

    private void SetOperationState(bool running)
    {
        if (running)
        {
            foreach (var button in _operationButtons)
            {
                button.IsEnabled = false;
            }
        }
        else
        {
            ApplyActionAvailability();
        }

        _cancelButton.IsVisible = running;
        _cancelButton.IsEnabled = running;
        _manifestButton.IsEnabled = !running;
        _beta1ManifestButton.IsEnabled = !running;
    }

    private async void ManifestButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _manifestButton.IsEnabled = false;
            AppendLog(Text("loadingManifest"));
            var files = await _service.GetManifestAsync();
            await ShowManifestDialogAsync(files);
        }
        catch (Exception ex)
        {
            AppendLog(Text("error") + ": " + ex.Message);
        }
        finally
        {
            _manifestButton.IsEnabled = _operationCancellation is null;
        }
    }

    private async void Beta1ManifestButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _beta1ManifestButton.IsEnabled = false;
            AppendLog(Text("loadingBeta1Manifest"));
            var files = Beta1Manifest.StorageFiles
                .Select(file => $"{Text("plutoniumManifestPrefix")}/{file}")
                .Concat(Beta1Manifest.GameFiles.Select(file => $"{Text("gameManifestPrefix")}/{file}"))
                .ToList();
            await ShowManifestDialogAsync(files, Text("beta1Manifest"));
        }
        catch (Exception ex)
        {
            AppendLog(Text("error") + ": " + ex.Message);
        }
        finally
        {
            _beta1ManifestButton.IsEnabled = _operationCancellation is null;
        }
    }

    private Task ShowManifestDialogAsync(IReadOnlyList<string> files)
    {
        return ShowManifestDialogAsync(files, Text("manifest"));
    }

    private async Task ShowManifestDialogAsync(IReadOnlyList<string> files, string titleText)
    {
        var dialog = new Window
        {
            Title = titleText,
            Width = 720,
            Height = 600,
            MinWidth = 560,
            MinHeight = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Background
        };

        var title = new TextBlock
        {
            Text = $"{titleText} - {files.Count} {Text("manifestCount")}",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = Foreground,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var manifestText = files.Count == 0
            ? Text("emptyManifest")
            : string.Join(Environment.NewLine, files.Select((file, index) => $"{index + 1}. {file}"));

        var manifestViewer = new ScrollViewer
        {
            Content = new TextBlock
            {
                Text = manifestText,
                TextWrapping = TextWrapping.NoWrap,
                Foreground = Foreground
            },
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Background = new SolidColorBrush(Color.FromRgb(18, 20, 28)),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var closeButton = new Button
        {
            Content = Text("close"),
            Width = 120,
            Height = 38,
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = new SolidColorBrush(Color.FromRgb(78, 136, 255)),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(8)
        };
        closeButton.Click += (_, _) => dialog.Close();

        var content = new Grid
        {
            Margin = new Thickness(22),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(new GridLength(1, GridUnitType.Star)),
                new RowDefinition(GridLength.Auto)
            }
        };
        content.Children.Add(title);
        Grid.SetRow(title, 0);
        content.Children.Add(manifestViewer);
        Grid.SetRow(manifestViewer, 1);
        content.Children.Add(closeButton);
        Grid.SetRow(closeButton, 2);
        dialog.Content = content;
        await dialog.ShowDialog(this);
    }

    private void UpdateProgress(DownloadProgress progress)
    {
        var total = Math.Max(progress.TotalFiles, 1);
        _progressBar.Maximum = total;
        _progressBar.Value = Math.Min(progress.CompletedFiles + progress.CurrentFileFraction, total);
        var unit = _currentLanguage == "en" ? "files" : _currentLanguage == "pt" ? "ficheiros" : "archivos";
        _progressText.Text = $"{progress.CompletedFiles} / {progress.TotalFiles} {unit} - {progress.CurrentFile}";
    }

    private void ResetProgress()
    {
        _progressBar.Maximum = 1;
        _progressBar.Value = 0;
        _progressText.Text = Text("ready");
    }

    private static bool ContainsRequiredPlutoniumPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

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

    private void AppendLog(string message)
    {
        if (message.StartsWith("Downloading: ", StringComparison.Ordinal))
        {
            message = $"{Text("downloading")}: {message[13..]}";
        }
        else if (message.StartsWith("Removed: ", StringComparison.Ordinal))
        {
            message = $"{Text("removed")}: {message[9..]}";
        }

        _logBox.Text = string.IsNullOrWhiteSpace(_logBox.Text)
            || _logBox.Text == "Ready"
            || _logBox.Text == "Listo - 0 archivos"
            || _logBox.Text == "Pronto - 0 ficheiros"
            ? message
            : _logBox.Text + Environment.NewLine + message;
        Dispatcher.UIThread.Post(() => _logScrollViewer.ScrollToEnd());
    }

    private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var folder = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = Text("folderTitle"),
                AllowMultiple = false
            });

            var selected = folder.FirstOrDefault();
            if (selected is not null)
            {
                _pathBox.Text = selected.Path.LocalPath;
                AppendLog($"{Text("folderSelected")}: {selected.Path.LocalPath}");
                _ = RefreshActionAvailabilityAsync();
            }
        }
        catch (Exception ex)
        {
            AppendLog(Text("folderPickerError") + ": " + ex.Message);
        }
    }
}
