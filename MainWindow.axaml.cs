using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ImageMagick;

namespace FolderIcon;

public partial class MainWindow : Window
{
    private string? _imagePath;
    private string? _folderPath;
    private string? _icoPath;

    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnWindowKeyDown;

        var folderPathBox = this.FindControl<TextBox>("FolderPathBox");
        var imageDropBorder = this.FindControl<Border>("ImageDropBorder");

        DragDrop.SetAllowDrop(folderPathBox, true);
        DragDrop.SetAllowDrop(imageDropBorder, true);

        folderPathBox.AddHandler(DragDrop.DragOverEvent, OnFolderPathDragOver);
        folderPathBox.AddHandler(DragDrop.DropEvent, OnFolderPathDrop);

        imageDropBorder.AddHandler(DragDrop.DragOverEvent, OnImageDragOver);
        imageDropBorder.AddHandler(DragDrop.DropEvent, OnImageDrop);
    }

    private async void OnAddFolder(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select folder",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        if (folder is null) return;
        _folderPath = folder.Path.LocalPath;
        this.FindControl<TextBox>("FolderPathBox").Text = _folderPath;
    }

    private void OnClearFolder(object? sender, RoutedEventArgs e)
    {
        _folderPath = null;
        this.FindControl<TextBox>("FolderPathBox").Text = string.Empty;
    }

    private void OnFolderPathTextChanged(object? sender, TextChangedEventArgs e)
        => _folderPath = this.FindControl<TextBox>("FolderPathBox").Text;

    private void OnFolderPathDragOver(object? s, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
    private async void OnFolderPathDrop(object? s, DragEventArgs e)
    {
        var files = e.Data.GetFiles();
        var first = files?.FirstOrDefault();
        if (first is null) return;
        var path = first.Path.LocalPath;
        if (!Directory.Exists(path))
        {
            await ShowMessage("Warning", "Only folder path is allowed in this field.");
            return;
        }
        _folderPath = path;
        this.FindControl<TextBox>("FolderPathBox").Text = path;
        e.Handled = true;
    }

    private void OnImageDragOver(object? s, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
    private async void OnImageDrop(object? s, DragEventArgs e)
    {
        var files = e.Data.GetFiles();
        var all = files?.ToList() ?? [];
        if (all.Count != 1)
        {
            await ShowMessage("Tip", "Only 1 image allowed.");
            return;
        }

        var path = all[0].Path.LocalPath;
        if (!File.Exists(path) || !IsSupported(path))
        {
            await ShowMessage("Warning", "Unsupported format.");
            return;
        }

        _imagePath = path;
        await using var stream = File.OpenRead(path);
        this.FindControl<Avalonia.Controls.Image>("PreviewImage").Source = new Bitmap(stream);
        e.Handled = true;
    }

    private async void OnFinish(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_imagePath))
        {
            await ShowMessage("Tip", "Please add an image.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_folderPath) || !Directory.Exists(_folderPath))
        {
            await ShowMessage("Tip", "Please add a folder path.");
            return;
        }

        GenerateIco();
        ApplyLinuxFolderIcon();
        await ShowMessage("Info", "Folder icon changed successfully.");
    }

    private async void OnRestore(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            await ShowMessage("Warning", "Please select a folder path.");
            return;
        }

        RunProcess("gio", $"set -t string \"{_folderPath}\" metadata::custom-icon \"\"");
        await ShowMessage("Info", "Restore finished.");
    }

    private void GenerateIco()
    {
        var random = RandomString(6);
        _icoPath = Path.Combine(_folderPath!, $"icon-{random}.ico");

        using var image = new MagickImage(_imagePath!);
        image.BackgroundColor = new MagickColor("transparent");
        image.Resize(new MagickGeometry(256, 256));
        image.Extent(256, 256, Gravity.Center);
        image.Write(_icoPath);

        File.SetAttributes(_icoPath, File.GetAttributes(_icoPath) | FileAttributes.Hidden);
    }

    private void ApplyLinuxFolderIcon()
    {
        var iconUri = $"file://{_icoPath}";
        RunProcess("gio", $"set -t string \"{_folderPath}\" metadata::custom-icon \"{iconUri}\"");
    }

    private static void RunProcess(string fileName, string arguments)
    {
        using var p = new Process();
        p.StartInfo.FileName = fileName;
        p.StartInfo.Arguments = arguments;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} failed with code {p.ExitCode}");
        }
    }

    private static bool IsSupported(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".jpeg" or ".jpg" or ".png" or ".ico";
    }

    private static string RandomString(int digits)
    {
        var chars = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, digits).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private async System.Threading.Tasks.Task ShowMessage(string title, string message)
    {
        var window = new Window
        {
            Width = 380,
            Height = 160,
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
