using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ImageMagick;

namespace FolderIcon;

public partial class MainWindow : Window
{
    private static readonly HashSet<string> SupportedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".jfif",
            ".gif",
            ".bmp",
            ".tif",
            ".tiff",
            ".ico"
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

    private async void OnAddFolder(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select folder", AllowMultiple = false });
        var folder = folders.FirstOrDefault();
        if (folder is not null)
        {
            SetFolderPath(folder.Path.LocalPath);
        }
    }

    private void OnClearFolder(object? sender, RoutedEventArgs e)
    {
        _folderPath = null;
        var folderPathBox = this.FindControl<TextBox>("FolderPathBox");
        if (folderPathBox != null)
            folderPathBox.Text = string.Empty;
    }

    private void OnFolderPathTextChanged(object? sender, TextChangedEventArgs e)
    {
        var folderPathBox = this.FindControl<TextBox>("FolderPathBox");
        _folderPath = folderPathBox?.Text;
    }

    private void OnFolderDragOver(object? sender, DragEventArgs e)
    {
        Debug.WriteLine("Folder DragOver");
        if (e.Data.Contains(DataFormats.Files))
            e.DragEffects = DragDropEffects.Copy;
        else
            e.DragEffects = DragDropEffects.None;

        e.Handled = true;
    }

    private async void OnFolderDrop(object? sender, DragEventArgs e)
    {
        Debug.WriteLine("Folder Drop");

        try
        {
            e.Handled = true;

            var path = GetFirstDroppedPath(e);
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!Directory.Exists(path))
            {
                await ShowMessage("提示", "这里只能拖入文件夹。", false);
                return;
            }

            SetFolderPath(path);
        }
        catch (Exception ex)
        {
            await ShowMessage("拖拽错误", ex.ToString(), true);
        }
    }

    private void OnImageDragOver(object? sender, DragEventArgs e)
    {
        Debug.WriteLine("Image DragOver");
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private async void OnImageDrop(object? sender, DragEventArgs e)
    {
        Debug.WriteLine("Image Drop");

        try
        {
            e.Handled = true;

            var path = GetFirstDroppedPath(e);
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (Directory.Exists(path))
            {
                await ShowMessage("提示", "这里只能拖入图片文件。", false);
                return;
            }

            if (!File.Exists(path))
            {
                await ShowMessage("提示", "拖入的文件不存在。", false);
                return;
            }

            if (!IsSupportedImageFile(path))
            {
                await ShowMessage("提示", "不支持该图片格式。", false);
                return;
            }

            _imagePath = path;
            await SetPreviewImageAsync(path);
        }
        catch (Exception ex)
        {
            await ShowMessage("拖拽错误", ex.ToString(), true);
        }
    }

    private static string? GetFirstDroppedPath(DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files))
            return null;

        var files = e.Data.GetFiles();
        if (files == null)
            return null;

        foreach (var file in files)
        {
            return file.Path.LocalPath;
        }

        return null;
    }

    private async void OnFinish(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_imagePath))
            {
                await ShowMessage("Tip", "Please add an image.", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(_folderPath) || !Directory.Exists(_folderPath))
            {
                await ShowMessage("Tip", "Please add a folder path.", false);
                return;
            }

            if (OperatingSystem.IsLinux())
            {
                await ApplyLinuxFolderIconAsync(_folderPath!, _imagePath!);
            }
            else if (OperatingSystem.IsMacOS())
            {
                await ApplyMacFolderIconAsync(_folderPath!, _imagePath!);
            }
            else if (OperatingSystem.IsWindows())
            {
                GenerateWindowsIco();
                ApplyWindowsFolderIcon();
            }
            else
            {
                await ShowMessage("Warning", "当前操作系统暂不支持自动设置文件夹图标。", false);
                return;
            }

            await ShowMessage("Info", "Folder icon changed successfully.", false);
        }
        catch (Exception ex)
        {
            await ShowMessage("Error", ex.Message, false);
        }
    }

    private async void OnRestore(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_folderPath))
            {
                await ShowMessage("Warning", "Please select a folder path.", false);
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
            else
            {
                await ShowMessage("Warning", "当前操作系统暂不支持自动恢复文件夹图标。", false);
                return;
            }

            await ShowMessage("Info", "Restore finished.", false);
        }
        catch (Exception ex)
        {
            await ShowMessage("Error", ex.Message, false);
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
        if (folderPathBox == null)
            return;

        folderPathBox.Text = path;
    }

    private async Task SetPreviewImageAsync(string imagePath)
    {
        _imagePath = imagePath;

        var previewImage = this.FindControl<Avalonia.Controls.Image>("PreviewImage");
        if (previewImage == null)
        {
            await ShowMessage("错误", "PreviewImage not found.", true);
            return;
        }

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
    }

    private async Task ApplyLinuxFolderIconAsync(string folderPath, string imagePath)
    {
        var uri = new Uri(Path.GetFullPath(imagePath)).AbsoluteUri;
        var result = await RunProcessAsync("gio", ["set", "-t", "string", folderPath, "metadata::custom-icon", uri]);

        if (result.ExitCode != 0)
        {
            if (result.StdErr.Contains("metadata::custom-icon", StringComparison.OrdinalIgnoreCase) &&
                result.StdErr.Contains("not", StringComparison.OrdinalIgnoreCase) &&
                result.StdErr.Contains("support", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("当前 Linux 文件管理器不支持自动设置自定义文件夹图标。");
            }

            throw new InvalidOperationException($"gio set failed: {result.StdErr}");
        }
    }

    private async Task RestoreLinuxFolderIconAsync(string folderPath)
    {
        var result = await RunProcessAsync("gio", ["set", "-t", "unset", folderPath, "metadata::custom-icon"]);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"gio restore failed: {result.StdErr}");
        }
    }

    private async Task ApplyMacFolderIconAsync(string folderPath, string imagePath)
    {
        var check = await RunProcessAsync("which", ["fileicon"]);
        if (check.ExitCode != 0)
        {
            throw new InvalidOperationException("macOS 文件夹图标功能需要安装 fileicon，请先执行：brew install fileicon");
        }

        var result = await RunProcessAsync("fileicon", ["set", folderPath, imagePath]);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"fileicon set failed: {result.StdErr}");
        }
    }

    private async Task RestoreMacFolderIconAsync(string folderPath)
    {
        var check = await RunProcessAsync("which", ["fileicon"]);
        if (check.ExitCode != 0)
        {
            throw new InvalidOperationException("macOS 文件夹图标功能需要安装 fileicon，请先执行：brew install fileicon");
        }

        var result = await RunProcessAsync("fileicon", ["rm", folderPath]);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"fileicon rm failed: {result.StdErr}");
        }
    }

    private void GenerateWindowsIco()
    {
        var random = RandomString(6);
        _icoPath = Path.Combine(_folderPath!, $"icon-{random}.ico");

        using var image = new MagickImage(_imagePath!);
        image.BackgroundColor = new MagickColor("transparent");
        image.Resize(new MagickGeometry(256, 256));
        image.Extent(256, 256, Gravity.Center);
        image.Write(_icoPath);
    }

    private void ApplyWindowsFolderIcon()
    {
        if (_folderPath is null || _icoPath is null) return;
        var iniPath = Path.Combine(_folderPath, "desktop.ini");
        File.WriteAllText(iniPath, "[.ShellClassInfo]\nIconResource=" + Path.GetFileName(_icoPath) + ",0\n");
        if (OperatingSystem.IsWindows())
        {
            var attribPath = Environment.ExpandEnvironmentVariables("%SystemRoot%\\System32\\attrib.exe");
            _ = RunProcessAsync(attribPath, ["+h", _icoPath]).Result;
            _ = RunProcessAsync(attribPath, ["+s", "+r", _folderPath]).Result;
            _ = RunProcessAsync(attribPath, ["+h", "+s", iniPath]).Result;
        }
    }

    private void RestoreWindowsFolderIcon(string folderPath)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        if (File.Exists(iniPath)) File.Delete(iniPath);
    }

    private static string RandomString(int digits)
    {
        var chars = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, digits).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(string fileName, IReadOnlyList<string> arguments)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in arguments)
            {
                psi.ArgumentList.Add(arg);
            }

            using var process = new Process { StartInfo = psi };
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return (process.ExitCode, await stdoutTask, await stderrTask);
        }
        catch (Exception ex)
        {
            return (-1, string.Empty, ex.Message);
        }
    }

    private async Task ShowMessage(string title, string message, bool isError)
    {
        var window = new Window
        {
            Width = 420,
            Height = 190,
            Title = title,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Width = 80,
                    }
                }
            }
        };

        if (window.Content is StackPanel panel && panel.Children.Last() is Button button)
        {
            button.Click += (_, _) => window.Close();
        }

        await window.ShowDialog(this);
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.W)
        {
            Close();
        }
    }
}
