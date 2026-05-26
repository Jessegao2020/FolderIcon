using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ImageMagick;

namespace FolderIcon;

public partial class MainWindow : Window
{
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".jfif", ".gif", ".bmp", ".tif", ".tiff", ".ico"
    };

    private string? _imagePath;
    private string? _folderPath;
    private string? _icoPath;
    private Bitmap? _previewBitmap;

    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnWindowKeyDown;
    }

    private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Target Folder",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        if (folder is not null)
            SetFolderPath(folder.Path.LocalPath);
    }

    private async void OnBrowseImageClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Icon Image File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.webp", "*.jfif", "*.gif", "*.bmp", "*.tif", "*.tiff", "*.ico" }
                }
            }
        });

        var file = files.FirstOrDefault();
        if (file is not null)
        {
            await SetPreviewImageAsync(file.Path.LocalPath);
        }
    }

    private void OnClearFolder(object? sender, RoutedEventArgs e)
    {
        _folderPath = null;
        var folderPathBox = this.FindControl<TextBox>("FolderPathBox");
        if (folderPathBox != null)
            folderPathBox.Text = string.Empty;
    }

    private void OnClearImageClick(object? sender, RoutedEventArgs e)
    {
        _imagePath = null;
        var previewImage = this.FindControl<Image>("PreviewImage");
        if (previewImage != null)
            previewImage.Source = null;

        var dropPromptText = this.FindControl<TextBlock>("DropPromptText");
        if (dropPromptText != null)
            dropPromptText.IsVisible = true;
    }

    private void OnFolderPathTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
            _folderPath = textBox.Text;
    }

    private void OnFolderDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void OnFolderDrop(object? sender, DragEventArgs e)
    {
        try
        {
            e.Handled = true;
            var path = GetFirstDroppedPath(e);
            if (string.IsNullOrWhiteSpace(path)) return;

            if (!Directory.Exists(path))
            {
                await ShowMessage("Tip", "Only folders can be dropped here.");
                return;
            }
            SetFolderPath(path);
        }
        catch (Exception ex)
        {
            await ShowMessage("Drag Error", ex.ToString());
        }
    }

    private void OnImageDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void OnImageDrop(object? sender, DragEventArgs e)
    {
        try
        {
            e.Handled = true;
            var path = GetFirstDroppedPath(e);
            if (string.IsNullOrWhiteSpace(path)) return;

            if (Directory.Exists(path) || !File.Exists(path)) return;

            if (!IsSupportedImageFile(path))
            {
                await ShowMessage("Tip", "Unsupported image format.");
                return;
            }
            await SetPreviewImageAsync(path);
        }
        catch (Exception ex)
        {
            await ShowMessage("Drag Error", ex.ToString());
        }
    }

    private static string? GetFirstDroppedPath(DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                foreach (var file in files)
                {
                    var path = file.Path.LocalPath;
                    if (!string.IsNullOrWhiteSpace(path)) return path;
                }
            }
        }
        if (e.Data.Contains("text/uri-list"))
        {
            var uriList = e.Data.Get("text/uri-list") as string;
            if (!string.IsNullOrWhiteSpace(uriList))
            {
                var lines = uriList.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
                    if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && uri.IsFile)
                        return uri.LocalPath;
                }
            }
        }
        return null;
    }

    private async void OnFinish(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_imagePath))
            {
                await ShowMessage("Tip", "Please select an image first.");
                return;
            }
            if (string.IsNullOrWhiteSpace(_folderPath) || !Directory.Exists(_folderPath))
            {
                await ShowMessage("Tip", "Please select a valid folder path.");
                return;
            }

            if (OperatingSystem.IsLinux())
            {
                await ApplyLinuxFolderIconAsync(_folderPath, _imagePath);
            }
            else if (OperatingSystem.IsMacOS())
            {
                await ApplyMacFolderIconAsync(_folderPath, _imagePath);
            }
            else if (OperatingSystem.IsWindows())
            {
                GenerateWindowsIco();
                await ApplyWindowsFolderIconAsync();
            }

            // 成功提示弹窗已按要求移除，现在直接静默完成
        }
        catch (Exception ex)
        {
            await ShowMessage("Error", ex.Message);
        }
    }

    private async void OnRestore(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_folderPath))
            {
                await ShowMessage("Warning", "Please select a folder path.");
                return;
            }

            if (OperatingSystem.IsLinux())
            {
                await RestoreLinuxFolderIconAsync(_folderPath);
            }
            else if (OperatingSystem.IsMacOS())
            {
                await RestoreMacFolderIconAsync(_folderPath);
            }
            else if (OperatingSystem.IsWindows())
            {
                RestoreWindowsFolderIcon(_folderPath);
            }

            await ShowMessage("Info", "Restore finished.");
        }
        catch (Exception ex)
        {
            await ShowMessage("Error", ex.Message);
        }
    }

    private static bool IsSupportedImageFile(string path)
    {
        return SupportedImageExtensions.Contains(Path.GetExtension(path));
    }

    private void SetFolderPath(string path)
    {
        _folderPath = path;
        var folderPathBox = this.FindControl<TextBox>("FolderPathBox");
        if (folderPathBox != null)
            folderPathBox.Text = path;
    }

    private async Task SetPreviewImageAsync(string imagePath)
    {
        _imagePath = imagePath;
        var previewImage = this.FindControl<Image>("PreviewImage");
        if (previewImage == null) return;

        _previewBitmap?.Dispose();
        _previewBitmap = null;

        try
        {
            await using var sourceStream = File.OpenRead(imagePath);
            _previewBitmap = new Bitmap(sourceStream);
        }
        catch
        {
            await using var output = new MemoryStream();
            using var magickImage = new MagickImage(imagePath);
            magickImage.AutoOrient();
            magickImage.Format = MagickFormat.Png;
            await magickImage.WriteAsync(output);
            output.Position = 0;
            _previewBitmap = new Bitmap(output);
        }
        previewImage.Source = _previewBitmap;

        var dropPromptText = this.FindControl<TextBlock>("DropPromptText");
        if (dropPromptText != null)
            dropPromptText.IsVisible = false;
    }

    private async Task ApplyLinuxFolderIconAsync(string folderPath, string imagePath)
    {
        var uri = new Uri(Path.GetFullPath(imagePath)).AbsoluteUri;
        var result = await RunProcessAsync("gio", new[] { "set", "-t", "string", folderPath, "metadata::custom-icon", uri });

        if (result.ExitCode != 0)
        {
            if (result.StdErr.Contains("metadata::custom-icon", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("当前 Linux 桌面环境可能禁用了自定义文件夹图标属性。");
            throw new InvalidOperationException($"gio set failed: {result.StdErr}");
        }
    }

    private async Task RestoreLinuxFolderIconAsync(string folderPath)
    {
        var result = await RunProcessAsync("gio", new[] { "set", "-t", "unset", folderPath, "metadata::custom-icon" });
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"gio restore failed: {result.StdErr}");
    }

    private async Task ApplyMacFolderIconAsync(string folderPath, string imagePath)
    {
        var check = await RunProcessAsync("which", new[] { "fileicon" });
        if (check.ExitCode != 0)
            throw new InvalidOperationException("macOS 需要安装 fileicon 工具，请先执行: brew install fileicon");

        var result = await RunProcessAsync("fileicon", new[] { "set", folderPath, imagePath });
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"fileicon failed: {result.StdErr}");
    }

    private async Task RestoreMacFolderIconAsync(string folderPath)
    {
        var result = await RunProcessAsync("fileicon", new[] { "rm", folderPath });
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"fileicon rm failed: {result.StdErr}");
    }

    private void GenerateWindowsIco()
    {
        if (_folderPath == null || _imagePath == null) return;
        var random = RandomString(6);
        _icoPath = Path.Combine(_folderPath, $"icon-{random}.ico");

        using var image = new MagickImage(_imagePath);
        image.BackgroundColor = MagickColors.Transparent;
        image.Resize(new MagickGeometry(256, 256));
        image.Extent(256, 256, Gravity.Center);
        image.Write(_icoPath);
    }

    private async Task ApplyWindowsFolderIconAsync()
    {
        if (_folderPath is null || _icoPath is null) return;
        var iniPath = Path.Combine(_folderPath, "desktop.ini");
        var iniText = "[.ShellClassInfo]" + Environment.NewLine + "IconResource=" + Path.GetFileName(_icoPath) + ",0" + Environment.NewLine;
        File.WriteAllText(iniPath, iniText);

        if (!OperatingSystem.IsWindows()) return;
        var attribPath = Environment.ExpandEnvironmentVariables("%SystemRoot%\\System32\\attrib.exe");
        await RunProcessAsync(attribPath, new[] { "+h", _icoPath });
        await RunProcessAsync(attribPath, new[] { "+s", "+r", _folderPath });
        await RunProcessAsync(attribPath, new[] { "+h", "+s", iniPath });
    }

    private void RestoreWindowsFolderIcon(string folderPath)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        if (File.Exists(iniPath)) File.Delete(iniPath);
    }

    private static string RandomString(int length)
    {
        const string chars = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(string fileName, IEnumerable<string> arguments)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            foreach (var arg in arguments) psi.ArgumentList.Add(arg);
            using var process = new Process { StartInfo = psi };
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, await stdoutTask, await stderrTask);
        }
        catch (Exception ex) { return (-1, string.Empty, ex.Message); }
    }

    private async Task ShowMessage(string title, string message)
    {
        var okButton = new Button { Content = "OK", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
        var window = new Window
        {
            Width = 420, Height = 190, Title = title, CanResize = false,
            Content = new StackPanel { Margin = new Thickness(16), Spacing = 12, Children = { new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap }, okButton } }
        };
        okButton.Click += (_, _) => window.Close();
        await window.ShowDialog(this);
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.W) Close();
    }
}