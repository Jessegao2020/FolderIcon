using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".jfif", ".bmp", ".ico"
    };

    private string? _imagePath;
    private string? _folderPath;
    private string? _icoPath;
    private Bitmap? _previewBitmap;

    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnWindowKeyDown;

        var routing = RoutingStrategies.Tunnel | RoutingStrategies.Bubble;
        var rootGrid = this.FindControl<Grid>("RootGrid");
        var folderPathBox = this.FindControl<TextBox>("FolderPathBox");
        var imageDropBorder = this.FindControl<Border>("ImageDropBorder");
        var previewImage = this.FindControl<Avalonia.Controls.Image>("PreviewImage");

        AddHandler(DragDrop.DragOverEvent, OnAnyDragOver, routing, handledEventsToo: true);
        AddHandler(DragDrop.DropEvent, OnAnyDrop, routing, handledEventsToo: true);
        rootGrid.AddHandler(DragDrop.DragOverEvent, OnAnyDragOver, routing, handledEventsToo: true);
        rootGrid.AddHandler(DragDrop.DropEvent, OnAnyDrop, routing, handledEventsToo: true);
        folderPathBox.AddHandler(DragDrop.DragOverEvent, OnAnyDragOver, routing, handledEventsToo: true);
        folderPathBox.AddHandler(DragDrop.DropEvent, OnAnyDrop, routing, handledEventsToo: true);
        imageDropBorder.AddHandler(DragDrop.DragOverEvent, OnAnyDragOver, routing, handledEventsToo: true);
        imageDropBorder.AddHandler(DragDrop.DropEvent, OnAnyDrop, routing, handledEventsToo: true);
        previewImage.AddHandler(DragDrop.DragOverEvent, OnAnyDragOver, routing, handledEventsToo: true);
        previewImage.AddHandler(DragDrop.DropEvent, OnAnyDrop, routing, handledEventsToo: true);
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
        this.FindControl<TextBox>("FolderPathBox").Text = string.Empty;
    }

    private void OnFolderPathTextChanged(object? sender, TextChangedEventArgs e)
        => _folderPath = this.FindControl<TextBox>("FolderPathBox").Text;

    private void OnAnyDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnAnyDrop(object? sender, DragEventArgs e)
    {
        try
        {
            var files = e.Data.GetFiles();
            var first = files?.FirstOrDefault();
            if (first is null)
            {
                await ShowMessage("Warning", "未检测到可用文件。", false);
                return;
            }

            var path = first.Path.LocalPath;
            if (Directory.Exists(path))
            {
                SetFolderPath(path);
            }
            else if (File.Exists(path))
            {
                if (!IsSupportedImageFile(path))
                {
                    await ShowMessage("Warning", "Unsupported image format.", false);
                    return;
                }

                _imagePath = path;
                await SetPreviewImageAsync(path);
            }
            else
            {
                await ShowMessage("Warning", "Dropped item is not a valid local file/folder.", false);
            }

            e.Handled = true;
        }
        catch (Exception ex)
        {
            await ShowMessage("Drop Error", ex.Message, false);
        }
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

    private bool IsSupportedImageFile(string path)
        => SupportedImageExtensions.Contains(Path.GetExtension(path));

    private void SetFolderPath(string path)
    {
        _folderPath = path;
        this.FindControl<TextBox>("FolderPathBox").Text = path;
    }

    private async Task SetPreviewImageAsync(string imagePath)
    {
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
            magickImage.Format = MagickFormat.Png;
            magickImage.Write(output);
            output.Position = 0;
            _previewBitmap = new Bitmap(output);
        }

        this.FindControl<Avalonia.Controls.Image>("PreviewImage").Source = _previewBitmap;
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
