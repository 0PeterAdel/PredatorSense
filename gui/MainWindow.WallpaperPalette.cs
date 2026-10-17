using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace PredatorSense;

public partial class MainWindow
{
    private enum WallpaperTarget { Keyboard, Logo, Both }
    private Button? _readWallpaperButton, _chooseWallpaperImageButton, _applyWallpaperColorButton;
    private TextBlock? _wallpaperSourceText, _wallpaperPaletteStatus;
    private WrapPanel? _wallpaperColorSwatches;
    private ComboBox? _wallpaperLightingTarget;
    private Image? _wallpaperPreview;
    private Bitmap? _wallpaperPreviewBitmap;
    private WallpaperColor? _selectedWallpaperColor;
    private readonly List<Button> _wallpaperSwatchButtons = new();
    private bool _wallpaperBusy, _wallpaperClosed;

    private void InitializeWallpaperPalette()
    {
        _readWallpaperButton = this.FindControl<Button>("ReadWallpaperButton");
        _chooseWallpaperImageButton = this.FindControl<Button>("ChooseWallpaperImageButton");
        _applyWallpaperColorButton = this.FindControl<Button>("ApplyWallpaperColorButton");
        _wallpaperSourceText = this.FindControl<TextBlock>("WallpaperSourceText");
        _wallpaperPaletteStatus = this.FindControl<TextBlock>("WallpaperPaletteStatus");
        _wallpaperColorSwatches = this.FindControl<WrapPanel>("WallpaperColorSwatches");
        _wallpaperLightingTarget = this.FindControl<ComboBox>("WallpaperLightingTarget");
        _wallpaperPreview = this.FindControl<Image>("WallpaperPreview");
        if (_readWallpaperButton != null)
            _readWallpaperButton.Click += async (_, _) => await ReadCurrentWallpaperAsync();
        if (_chooseWallpaperImageButton != null)
            _chooseWallpaperImageButton.Click += ChooseWallpaperImage_Click;
        if (_applyWallpaperColorButton != null)
            _applyWallpaperColorButton.Click += ApplyWallpaperColor_Click;
        Closed += (_, _) =>
        {
            _wallpaperClosed = true;
            if (_wallpaperPreview != null) _wallpaperPreview.Source = null;
            _wallpaperPreviewBitmap?.Dispose();
        };
        _ = ReadCurrentWallpaperAsync();
    }

    private void UpdateWallpaperPaletteAvailability()
    {
        if (_wallpaperLightingTarget == null) return;
        var previous = (_wallpaperLightingTarget.SelectedItem as ComboBoxItem)?.Tag as WallpaperTarget?;
        var keyboard = _client.IsFeatureAvailable("per_zone_mode") || AppState.DevMode;
        var logo = _client.IsFeatureAvailable("back_logo") || AppState.DevMode;
        _wallpaperLightingTarget.Items.Clear();
        if (keyboard)
            _wallpaperLightingTarget.Items.Add(new ComboBoxItem { Content = "Keyboard", Tag = WallpaperTarget.Keyboard });
        if (logo)
            _wallpaperLightingTarget.Items.Add(new ComboBoxItem { Content = "Back Logo / Lightbar", Tag = WallpaperTarget.Logo });
        if (keyboard && logo)
            _wallpaperLightingTarget.Items.Add(new ComboBoxItem { Content = "Keyboard + Back Logo", Tag = WallpaperTarget.Both });
        var items = _wallpaperLightingTarget.Items.OfType<ComboBoxItem>().ToList();
        _wallpaperLightingTarget.SelectedItem = items.FirstOrDefault(item => Equals(item.Tag, previous)) ?? items.FirstOrDefault();
        var panel = this.FindControl<Border>("WallpaperPalettePanel");
        if (panel != null) panel.IsVisible = keyboard || logo;
        UpdateWallpaperButtons();
    }

    private void UpdateWallpaperButtons()
    {
        if (_readWallpaperButton != null) _readWallpaperButton.IsEnabled = !_wallpaperBusy;
        if (_chooseWallpaperImageButton != null) _chooseWallpaperImageButton.IsEnabled = !_wallpaperBusy;
        if (_wallpaperColorSwatches != null) _wallpaperColorSwatches.IsEnabled = !_wallpaperBusy;
        if (_wallpaperLightingTarget != null) _wallpaperLightingTarget.IsEnabled = !_wallpaperBusy;
        if (_applyWallpaperColorButton != null)
            _applyWallpaperColorButton.IsEnabled = !_wallpaperBusy && _isConnected &&
                _selectedWallpaperColor != null && _wallpaperLightingTarget?.SelectedItem != null;
    }

    private async Task ReadCurrentWallpaperAsync()
    {
        if (_wallpaperBusy) return;
        try
        {
            var path = WallpaperPalette.FindCurrentWallpaper();
            if (path == null)
            {
                SetText(_wallpaperPaletteStatus, "Current wallpaper was not found. Choose an image to extract its colors.");
                return;
            }
            await LoadWallpaperPaletteAsync(path);
        }
        catch (Exception ex)
        {
            SetText(_wallpaperPaletteStatus, $"Could not read the current wallpaper: {ex.Message}");
        }
    }

    private async void ChooseWallpaperImage_Click(object? sender, RoutedEventArgs e)
    {
        if (_wallpaperBusy) return;
        _wallpaperBusy = true;
        UpdateWallpaperButtons();
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose an image for lighting colors",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
            });
            if (files.Count == 0 || _wallpaperClosed) return;
            var path = files[0].TryGetLocalPath();
            if (path == null)
                throw new IOException("Choose an image stored on this computer.");
            await LoadWallpaperPaletteAsync(path);
        }
        catch (Exception ex)
        {
            SetText(_wallpaperPaletteStatus, $"Could not open the image: {ex.Message}");
        }
        finally
        {
            _wallpaperBusy = false;
            UpdateWallpaperButtons();
        }
    }

    private async Task LoadWallpaperPaletteAsync(string path)
    {
        _wallpaperBusy = true;
        UpdateWallpaperButtons();
        SetText(_wallpaperPaletteStatus, "Finding prominent colors…");
        try
        {
            var result = await Task.Run(() =>
            {
                var colors = WallpaperPalette.Extract(path);
                using var stream = File.OpenRead(path);
                var preview = Bitmap.DecodeToWidth(stream, 240);
                return (colors, preview);
            });
            if (_wallpaperClosed)
            {
                result.preview.Dispose();
                return;
            }
            var oldPreview = _wallpaperPreviewBitmap;
            _wallpaperPreviewBitmap = result.preview;
            if (_wallpaperPreview != null)
            {
                _wallpaperPreview.Source = result.preview;
                _wallpaperPreview.IsVisible = true;
            }
            oldPreview?.Dispose();
            SetText(_wallpaperSourceText, Path.GetFileName(path));
            ShowWallpaperSwatches(result.colors);
        }
        catch (Exception ex)
        {
            _selectedWallpaperColor = null;
            _wallpaperColorSwatches?.Children.Clear();
            _wallpaperSwatchButtons.Clear();
            if (_wallpaperPreview != null)
            {
                _wallpaperPreview.Source = null;
                _wallpaperPreview.IsVisible = false;
            }
            _wallpaperPreviewBitmap?.Dispose();
            _wallpaperPreviewBitmap = null;
            SetText(_wallpaperSourceText, Path.GetFileName(path));
            SetText(_wallpaperPaletteStatus, $"Could not extract colors: {ex.Message}");
        }
        finally
        {
            _wallpaperBusy = false;
            UpdateWallpaperButtons();
        }
    }

    private void ShowWallpaperSwatches(IReadOnlyList<WallpaperColor> colors)
    {
        if (_wallpaperColorSwatches == null) return;
        _wallpaperColorSwatches.Children.Clear();
        _wallpaperSwatchButtons.Clear();
        foreach (var color in colors)
        {
            var bright = 0.2126 * color.Red + 0.7152 * color.Green + 0.0722 * color.Blue > 145;
            var button = new Button
            {
                Content = new TextBlock { Text = $"#{color.Hex}\n{color.Coverage:P0} of image", TextAlignment = TextAlignment.Center },
                Background = new SolidColorBrush(Color.FromRgb(color.Red, color.Green, color.Blue)),
                Foreground = bright ? Brushes.Black : Brushes.White,
                BorderThickness = new Thickness(3),
                Width = 125, Height = 66, Margin = new Thickness(0, 0, 8, 8),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Tag = color
            };
            ToolTip.SetTip(button, $"Use #{color.Hex}");
            button.Click += (_, _) => SelectWallpaperColor(color);
            _wallpaperSwatchButtons.Add(button);
            _wallpaperColorSwatches.Children.Add(button);
        }
        _selectedWallpaperColor = null;
        SetText(_wallpaperPaletteStatus, "Select one of the prominent colors, then choose where to apply it.");
        UpdateWallpaperButtons();
    }

    private void SelectWallpaperColor(WallpaperColor color)
    {
        _selectedWallpaperColor = color;
        foreach (var button in _wallpaperSwatchButtons)
            button.BorderBrush = Equals(button.Tag, color) ? button.Foreground : Brushes.Transparent;
        SetText(_wallpaperPaletteStatus, $"Selected #{color.Hex}. Choose where to apply this static color.");
        UpdateWallpaperButtons();
    }

    private async void ApplyWallpaperColor_Click(object? sender, RoutedEventArgs e)
    {
        if (_wallpaperBusy || !_isConnected || _selectedWallpaperColor == null ||
            (_wallpaperLightingTarget?.SelectedItem as ComboBoxItem)?.Tag is not WallpaperTarget target)
            return;
        var color = _selectedWallpaperColor;
        _wallpaperBusy = true;
        UpdateWallpaperButtons();
        var applied = new List<string>();
        try
        {
            if (target is WallpaperTarget.Keyboard or WallpaperTarget.Both)
            {
                if (!await _client.SetPerZoneModeAsync(color.Hex, color.Hex, color.Hex, color.Hex, _keyboardBrightness))
                    throw new IOException("Keyboard lighting could not be applied.");
                SetColorPicker(_lightEffectColorPicker, color.Hex);
                foreach (var picker in new[] { _zone1ColorPicker, _zone2ColorPicker, _zone3ColorPicker, _zone4ColorPicker })
                    SetColorPicker(picker, color.Hex);
                if (_lightingModeComboBox != null) _lightingModeComboBox.SelectedIndex = 0;
                SaveLightingEffectPresetFromUI(0, GetSelectedDirection(), Color.FromRgb(color.Red, color.Green, color.Blue));
                SaveZonePresetFromUI();
                applied.Add("keyboard");
            }
            if (target is WallpaperTarget.Logo or WallpaperTarget.Both)
            {
                var enabled = _backLogoEnabledCheckBox?.IsChecked ?? true;
                var brightness = enabled && _backLogoBrightness == 0 ? 100 : _backLogoBrightness;
                if (!await _client.SetBackLogoColorAsync(color.Hex, brightness, enabled))
                    throw new IOException("Back logo lighting could not be applied.");
                SetColorPicker(_backLogoColorPicker, color.Hex);
                SetBackLogoBrightness(brightness);
                UpdateBackLogoEffectiveText();
                applied.Add(enabled ? "back logo" : "back logo (disabled)");
            }
            SetText(_wallpaperPaletteStatus, $"Applied #{color.Hex} to {string.Join(" and ", applied)}.");
        }
        catch (Exception ex)
        {
            var prefix = applied.Count == 0 ? "" : $"Applied to {string.Join(" and ", applied)}. ";
            SetText(_wallpaperPaletteStatus, prefix + ex.Message);
        }
        finally
        {
            _wallpaperBusy = false;
            UpdateWallpaperButtons();
        }
    }
}
