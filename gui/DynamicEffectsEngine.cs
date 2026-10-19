using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using Material.Icons;

namespace PredatorSense;

public enum DynamicEffectCategory
{
    Hardware,
    InteractiveSoftware
}

public class DynamicEffectDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ArabicName { get => Subtitle; set => Subtitle = value; } // backward-compat
    public string Description { get; set; } = string.Empty;
    public DynamicEffectCategory Category { get; set; }
    public MaterialIconKind Icon { get; set; }
    public bool IsHardware { get; set; }
    public int HardwareMode { get; set; }
    public bool SupportsSpeed { get; set; } = true;
    public bool SupportsDirection { get; set; } = false;
    public bool SupportsCustomColor { get; set; } = false;
    public string DefaultColorHex { get; set; } = "00D4FF";

    public string DisplayTitle => string.IsNullOrEmpty(Subtitle) ? Name : $"{Name} ({Subtitle})";
}

public class DynamicEffectsEngine
{
    private readonly PredatorSenseClient _client;
    private readonly Action<string, string, string, string> _updatePreviewCallback;
    private CancellationTokenSource? _animationCts;
    private bool _isRunning;
    private DynamicEffectDefinition? _currentEffect;
    private int _speed = 5;
    private int _brightness = 100;
    private int _direction = 1; // 1 = Left to Right, 2 = Right to Left
    private Color _customColor = Color.Parse("#00D4FF");

    // Cached hardware paths for telemetry
    private static string? _cachedCpuTempPath;
    private static readonly Random _random = new();

    public static readonly List<DynamicEffectDefinition> AvailableEffects = new()
    {
        // 1. Hardware Firmware Effects (Zero CPU, Maximum Smoothness)
        new DynamicEffectDefinition
        {
            Id = "hw_wave",
            Name = "Wave Mode",
            Subtitle = "Spectrum Wave",
            Description = "Smooth continuous RGB wave flowing across the 4 zones.",
            Category = DynamicEffectCategory.Hardware,
            Icon = MaterialIconKind.Waves,
            IsHardware = true,
            HardwareMode = 3,
            SupportsSpeed = true,
            SupportsDirection = true,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "hw_neon",
            Name = "Neon Mode",
            Subtitle = "Cycling Neon",
            Description = "Vibrant cycling neon glow shifting dynamically across all keys.",
            Category = DynamicEffectCategory.Hardware,
            Icon = MaterialIconKind.Flare,
            IsHardware = true,
            HardwareMode = 2,
            SupportsSpeed = true,
            SupportsDirection = false,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "hw_breathing",
            Name = "Breathing Mode",
            Subtitle = "Color Respiration",
            Description = "Smooth pulsating respiration effect in your custom chosen color.",
            Category = DynamicEffectCategory.Hardware,
            Icon = MaterialIconKind.HeartPulse,
            IsHardware = true,
            HardwareMode = 1,
            SupportsSpeed = true,
            SupportsDirection = false,
            SupportsCustomColor = true,
            DefaultColorHex = "00D4FF"
        },
        new DynamicEffectDefinition
        {
            Id = "hw_shifting",
            Name = "Shifting Mode",
            Subtitle = "Gradient Shift",
            Description = "Horizontal color transitions shifting seamlessly between zones.",
            Category = DynamicEffectCategory.Hardware,
            Icon = MaterialIconKind.ArrowLeftRight,
            IsHardware = true,
            HardwareMode = 4,
            SupportsSpeed = true,
            SupportsDirection = true,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "hw_zoom",
            Name = "Zoom Mode",
            Subtitle = "Concentric Zoom",
            Description = "Concentric lighting pulsations zooming outwards from the keyboard center.",
            Category = DynamicEffectCategory.Hardware,
            Icon = MaterialIconKind.MagnifyPlusOutline,
            IsHardware = true,
            HardwareMode = 5,
            SupportsSpeed = true,
            SupportsDirection = false,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "hw_meteor",
            Name = "Meteor Storm",
            Subtitle = "Sweeping Comets",
            Description = "Falling celestial comets sweeping dynamically across keyboard zones.",
            Category = DynamicEffectCategory.Hardware,
            Icon = MaterialIconKind.WeatherSunny,
            IsHardware = true,
            HardwareMode = 6,
            SupportsSpeed = true,
            SupportsDirection = true,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "hw_twinkling",
            Name = "Starry Twinkle",
            Subtitle = "Starlight Shimmer",
            Description = "Random shimmering starlight sparkles across all 4 zones.",
            Category = DynamicEffectCategory.Hardware,
            Icon = MaterialIconKind.Creation,
            IsHardware = true,
            HardwareMode = 7,
            SupportsSpeed = true,
            SupportsDirection = false,
            SupportsCustomColor = true,
            DefaultColorHex = "FFD700"
        },

        // 2. Interactive & Software Animated Effects (Smart Reactive)
        new DynamicEffectDefinition
        {
            Id = "sw_cpu_heatmap",
            Name = "CPU Temp Reactive",
            Subtitle = "Live Thermal Heatmap",
            Description = "Smart reactive heatmap! Colors shift in real time: Cyan (<50°C), Amber (50-70°C), Crimson Red (>70°C).",
            Category = DynamicEffectCategory.InteractiveSoftware,
            Icon = MaterialIconKind.ThermometerAlert,
            IsHardware = false,
            SupportsSpeed = false,
            SupportsDirection = false,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "sw_fire_ember",
            Name = "Fire & Lava Ember",
            Subtitle = "Organic Hearth Glow",
            Description = "Realistic organic fireplace flicker oscillating between deep red, flame orange, and golden embers.",
            Category = DynamicEffectCategory.InteractiveSoftware,
            Icon = MaterialIconKind.Fire,
            IsHardware = false,
            SupportsSpeed = true,
            SupportsDirection = false,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "sw_police_strobe",
            Name = "Police Interceptor",
            Subtitle = "High-Intensity Strobe",
            Description = "High-energy alternating strobe! Zones 1-2 flash Electric Blue, Zones 3-4 flash Siren Red.",
            Category = DynamicEffectCategory.InteractiveSoftware,
            Icon = MaterialIconKind.PoliceBadge,
            IsHardware = false,
            SupportsSpeed = true,
            SupportsDirection = false,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "sw_matrix_rain",
            Name = "Matrix Cyber Cascade",
            Subtitle = "Digital Code Rain",
            Description = "Digital neon green data pulses sweeping rapidly across zones 1 -> 2 -> 3 -> 4.",
            Category = DynamicEffectCategory.InteractiveSoftware,
            Icon = MaterialIconKind.Matrix,
            IsHardware = false,
            SupportsSpeed = true,
            SupportsDirection = true,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "sw_cylon_scanner",
            Name = "Knight Rider Scanner",
            Subtitle = "Bi-Directional Radar Beam",
            Description = "A glowing beam sweeping back and forth continuously: Zone 1 ↔ Zone 4.",
            Category = DynamicEffectCategory.InteractiveSoftware,
            Icon = MaterialIconKind.EyeOutline,
            IsHardware = false,
            SupportsSpeed = true,
            SupportsDirection = false,
            SupportsCustomColor = true,
            DefaultColorHex = "FF0033"
        },
        new DynamicEffectDefinition
        {
            Id = "sw_cyberpunk_pulse",
            Name = "Cyberpunk Night City",
            Subtitle = "Dual Neon Pulse",
            Description = "Dynamic alternating pulses between Neon Cyan (#00F0FF) and Hot Magenta Pink (#FF007F).",
            Category = DynamicEffectCategory.InteractiveSoftware,
            Icon = MaterialIconKind.LightningBolt,
            IsHardware = false,
            SupportsSpeed = true,
            SupportsDirection = false,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "sw_rainbow_flow",
            Name = "Rainbow Chroma Flow",
            Subtitle = "360° Hue Rotation",
            Description = "Continuous smooth 360-degree color cycle flowing gracefully through the keyboard zones.",
            Category = DynamicEffectCategory.InteractiveSoftware,
            Icon = MaterialIconKind.PaletteAdvanced,
            IsHardware = false,
            SupportsSpeed = true,
            SupportsDirection = true,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "sw_aurora",
            Name = "Aurora Borealis",
            Subtitle = "Polar Ambient Drift",
            Description = "Ethereal mystical wave undulating smoothly between Polar Teal, Emerald, and Deep Violet.",
            Category = DynamicEffectCategory.InteractiveSoftware,
            Icon = MaterialIconKind.WeatherDust,
            IsHardware = false,
            SupportsSpeed = true,
            SupportsDirection = false,
            SupportsCustomColor = false
        },
        new DynamicEffectDefinition
        {
            Id = "sw_battery_fuel",
            Name = "Battery Fuel Gauge",
            Subtitle = "Charge Level Meter",
            Description = "Real-time battery level gauge: Zones light up proportionally from 25% (Zone 1) up to 100% (Zone 4).",
            Category = DynamicEffectCategory.InteractiveSoftware,
            Icon = MaterialIconKind.BatteryChargingHigh,
            IsHardware = false,
            SupportsSpeed = false,
            SupportsDirection = false,
            SupportsCustomColor = false
        }
    };

    public DynamicEffectsEngine(PredatorSenseClient client, Action<string, string, string, string> updatePreviewCallback)
    {
        _client = client;
        _updatePreviewCallback = updatePreviewCallback;
    }

    public DynamicEffectDefinition? CurrentEffect => _currentEffect;
    public bool IsRunning => _isRunning;

    public void SetParameters(int speed, int brightness, int direction, Color customColor)
    {
        _speed = Math.Clamp(speed, 0, 9);
        _brightness = Math.Clamp(brightness, 0, 100);
        _direction = direction;
        _customColor = customColor;
    }

    public void UpdateSpeed(int speed)
    {
        _speed = Math.Clamp(speed, 0, 9);
        if (_isRunning && _currentEffect != null)
        {
            var hexColor = _currentEffect.SupportsCustomColor ? ToHex(_customColor) : _currentEffect.DefaultColorHex;
            if (_currentEffect.IsHardware)
            {
                var color = _currentEffect.SupportsCustomColor ? _customColor : Color.Parse($"#{_currentEffect.DefaultColorHex}");
                _ = _client.SetFourZoneModeAsync(
                    _currentEffect.HardwareMode,
                    _speed,
                    _brightness,
                    _direction,
                    color.R,
                    color.G,
                    color.B
                );
            }
            else
            {
                _ = _client.SetDynamicEffectAsync(_currentEffect.Id, _speed, _brightness, _direction, hexColor);
            }
        }
    }

    public void UpdateBrightness(int brightness)
    {
        _brightness = Math.Clamp(brightness, 0, 100);
        if (_isRunning && _currentEffect != null)
        {
            var hexColor = _currentEffect.SupportsCustomColor ? ToHex(_customColor) : _currentEffect.DefaultColorHex;
            if (_currentEffect.IsHardware)
            {
                var color = _currentEffect.SupportsCustomColor ? _customColor : Color.Parse($"#{_currentEffect.DefaultColorHex}");
                _ = _client.SetFourZoneModeAsync(
                    _currentEffect.HardwareMode,
                    _speed,
                    _brightness,
                    _direction,
                    color.R,
                    color.G,
                    color.B
                );
            }
            else
            {
                _ = _client.SetDynamicEffectAsync(_currentEffect.Id, _speed, _brightness, _direction, hexColor);
            }
        }
    }

    public async Task StopAsync()
    {
        _isRunning = false;
        if (_animationCts != null)
        {
            _animationCts.Cancel();
            _animationCts.Dispose();
            _animationCts = null;
        }
        await _client.StopDynamicEffectAsync();
    }

    public async Task<bool> StartEffectAsync(DynamicEffectDefinition effect, bool syncOnly = false)
    {
        await StopAsync();

        _currentEffect = effect;
        _isRunning = true;
        _animationCts = new CancellationTokenSource();

        if (effect.IsHardware)
        {
            // Apply hardware mode directly to firmware via daemon
            var color = effect.SupportsCustomColor ? _customColor : Color.Parse($"#{effect.DefaultColorHex}");
            var success = true;
            if (!syncOnly)
            {
                success = await _client.SetFourZoneModeAsync(
                    effect.HardwareMode,
                    _speed,
                    _brightness,
                    _direction,
                    color.R,
                    color.G,
                    color.B
                );
            }

            // Start smooth local GUI preview simulation
            _ = RunHardwarePreviewSimulationAsync(effect, _animationCts.Token);
            return success;
        }
        else
        {
            var hexColor = effect.SupportsCustomColor ? ToHex(_customColor) : effect.DefaultColorHex;
            bool daemonStarted = false;
            if (!syncOnly)
            {
                daemonStarted = await _client.SetDynamicEffectAsync(effect.Id, _speed, _brightness, _direction, hexColor);
            }

            // Start software animation loop that safely drives GUI (and hardware only if daemon didn't take over)
            _ = RunSoftwareEffectLoopAsync(effect, _animationCts.Token, dispatchToHardware: !daemonStarted && !syncOnly);
            return true;
        }
    }

    private async Task RunHardwarePreviewSimulationAsync(DynamicEffectDefinition effect, CancellationToken ct)
    {
        double step = 0;
        const int frameIntervalMs = 25; // 40 FPS silky smooth UI rendering

        while (!ct.IsCancellationRequested && _isRunning)
        {
            try
            {
                // Dynamic speed progression: Speed 0 is gentle hypnotic drift, Speed 9 is high-velocity gaming pulse
                double speedStep = 0.0035 + (_speed * 0.0055);
                step += speedStep;

                string z1, z2, z3, z4;

                switch (effect.HardwareMode)
                {
                    case 3: // Wave (Smooth RGB rainbow spectrum flowing across zones)
                        double waveDir = _direction == 1 ? -step : step;
                        double p = ((waveDir % 1.0) + 1.0) % 1.0;
                        z1 = ColorFromHue(p);
                        z2 = ColorFromHue((p + 0.22) % 1.0);
                        z3 = ColorFromHue((p + 0.44) % 1.0);
                        z4 = ColorFromHue((p + 0.66) % 1.0);
                        break;

                    case 2: // Neon (Continuous unified hue cycling synchronously across all keys)
                        double neonHue = ((step * 0.6 % 1.0) + 1.0) % 1.0;
                        var neonColor = ColorFromHue(neonHue);
                        z1 = z2 = z3 = z4 = neonColor;
                        break;

                    case 1: // Breathing (Realistic respiratory curve pulsing base color)
                        var baseColor = effect.SupportsCustomColor ? _customColor : Color.Parse($"#{effect.DefaultColorHex}");
                        double breathPhase = (Math.Sin(step * Math.PI * 2.0 * 0.45) + 1.0) / 2.0; // 0.0 to 1.0
                        double intensity = Math.Pow(breathPhase, 2.0); // Natural quadratic eye-perception curve
                        double brightnessScale = Math.Max(0.04, intensity * (_brightness / 100.0));
                        var pulsed = ScaleColor(baseColor, brightnessScale);
                        z1 = z2 = z3 = z4 = ToHex(pulsed);
                        break;

                    case 4: // Shifting (Sequential color shift sweeping across zones)
                        double shiftPhase = ((step * 1.6) % 4.0 + 4.0) % 4.0;
                        int activeZone = _direction == 1 ? (int)shiftPhase : 3 - (int)shiftPhase;
                        var shiftCol = ColorFromHue(((step * 0.3 % 1.0) + 1.0) % 1.0);
                        z1 = activeZone == 0 ? shiftCol : "0c101a";
                        z2 = activeZone == 1 ? shiftCol : "0c101a";
                        z3 = activeZone == 2 ? shiftCol : "0c101a";
                        z4 = activeZone == 3 ? shiftCol : "0c101a";
                        break;

                    case 5: // Zoom Mode (Concentric ripples expanding outward from center Z2/Z3 to edges Z1/Z4)
                        double zoomPhase = ((step * 0.8 % 1.0) + 1.0) % 1.0;
                        var centerCol = ColorFromHue(zoomPhase);
                        var edgeCol = ColorFromHue((zoomPhase + 0.35) % 1.0);
                        z2 = z3 = centerCol;
                        z1 = z4 = edgeCol;
                        break;

                    case 6: // Meteor Storm (Celestial shooting comets sweeping across zones)
                        double mPos = ((step * 2.8) % 6.0 + 6.0) % 6.0;
                        int head = _direction == 1 ? (int)mPos : 5 - (int)mPos;
                        string MeteorColor(int zoneIdx)
                        {
                            int dist = Math.Abs(zoneIdx - head);
                            if (dist == 0) return "FFFFFF";
                            if (dist == 1) return "00F0FF";
                            if (dist == 2) return "005588";
                            return "070b14";
                        }
                        z1 = MeteorColor(0);
                        z2 = MeteorColor(1);
                        z3 = MeteorColor(2);
                        z4 = MeteorColor(3);
                        break;

                    case 7: // Twinkling (Smooth organic starlight sparkle shimmer)
                        var starCol = effect.SupportsCustomColor ? _customColor : Color.Parse($"#{effect.DefaultColorHex}");
                        double b1 = (Math.Sin(step * 3.6 + 0.0) + 1.0) * 0.45 + 0.1;
                        double b2 = (Math.Sin(step * 4.9 + 2.0) + 1.0) * 0.45 + 0.1;
                        double b3 = (Math.Sin(step * 4.1 + 4.1) + 1.0) * 0.45 + 0.1;
                        double b4 = (Math.Sin(step * 5.8 + 1.2) + 1.0) * 0.45 + 0.1;
                        z1 = ToHex(ScaleColor(starCol, b1 * (_brightness / 100.0)));
                        z2 = ToHex(ScaleColor(starCol, b2 * (_brightness / 100.0)));
                        z3 = ToHex(ScaleColor(starCol, b3 * (_brightness / 100.0)));
                        z4 = ToHex(ScaleColor(starCol, b4 * (_brightness / 100.0)));
                        break;

                    default:
                        z1 = z2 = z3 = z4 = effect.DefaultColorHex;
                        break;
                }

                Dispatcher.UIThread.Post(() => _updatePreviewCallback(z1, z2, z3, z4));
                await Task.Delay(frameIntervalMs, ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(50, ct);
            }
        }
    }

    private async Task RunSoftwareEffectLoopAsync(DynamicEffectDefinition effect, CancellationToken ct, bool dispatchToHardware = false)
    {
        double step = 0;
        int frameCount = 0;
        DateTime lastHwDispatch = DateTime.MinValue;

        while (!ct.IsCancellationRequested && _isRunning)
        {
            try
            {
                const int uiIntervalMs = 30; // ~33 FPS fluid local GUI rendering
                double speedFactor = 0.04 + (_speed * 0.04);
                step += speedFactor;
                frameCount++;

                string z1, z2, z3, z4;
                int hwThrottleMs = Math.Max(80, 240 - (_speed * 16));

                switch (effect.Id)
                {
                    case "sw_cpu_heatmap":
                        var cpuTemp = ReadCpuPackageTemperature();
                        var heatmapColor = GetHeatmapColor(cpuTemp);
                        z1 = z2 = z3 = z4 = heatmapColor;
                        hwThrottleMs = 1200;
                        break;

                    case "sw_police_strobe":
                        var isBlue = ((frameCount / 5) % 2) == 0;
                        z1 = z2 = isBlue ? "0044FF" : "110000";
                        z3 = z4 = isBlue ? "000022" : "FF0022";
                        hwThrottleMs = Math.Max(120, 260 - (_speed * 15));
                        break;

                    case "sw_fire_ember":
                        z1 = EmberColor(0.85 + Math.Sin(step * 0.6) * 0.15);
                        z2 = EmberColor(0.95 + Math.Sin(step * 0.6 + 1.2) * 0.2);
                        z3 = EmberColor(0.90 + Math.Sin(step * 0.6 + 2.4) * 0.18);
                        z4 = EmberColor(0.80 + Math.Sin(step * 0.6 + 3.6) * 0.22);
                        break;

                    case "sw_matrix_rain":
                        var matrixPos = _direction == 1 ? ((frameCount / 4) % 4) : 3 - ((frameCount / 4) % 4);
                        z1 = matrixPos == 0 ? "39FF14" : (matrixPos == 3 ? "003B00" : "001100");
                        z2 = matrixPos == 1 ? "39FF14" : (matrixPos == 0 ? "003B00" : "001100");
                        z3 = matrixPos == 2 ? "39FF14" : (matrixPos == 1 ? "003B00" : "001100");
                        z4 = matrixPos == 3 ? "39FF14" : (matrixPos == 2 ? "003B00" : "001100");
                        break;

                    case "sw_cylon_scanner":
                        var posSequence = new[] { 0, 1, 2, 3, 2, 1 };
                        var cylonIdx = posSequence[(frameCount / 4) % posSequence.Length];
                        var scanCol = effect.SupportsCustomColor ? ToHex(_customColor) : "FF0033";
                        var dimCol = "140205";
                        z1 = cylonIdx == 0 ? scanCol : dimCol;
                        z2 = cylonIdx == 1 ? scanCol : dimCol;
                        z3 = cylonIdx == 2 ? scanCol : dimCol;
                        z4 = cylonIdx == 3 ? scanCol : dimCol;
                        break;

                    case "sw_cyberpunk_pulse":
                        var cpCyan = "00F0FF";
                        var cpPink = "FF007F";
                        var cpSwap = ((frameCount / 8) % 2) == 0;
                        z1 = cpSwap ? cpCyan : cpPink;
                        z2 = cpSwap ? cpPink : cpCyan;
                        z3 = cpSwap ? cpCyan : cpPink;
                        z4 = cpSwap ? cpPink : cpCyan;
                        break;

                    case "sw_rainbow_flow":
                        double rHue = _direction == 1 ? (step * 0.05) % 1.0 : (1.0 - (step * 0.05) % 1.0);
                        double rp = ((rHue % 1.0) + 1.0) % 1.0;
                        z1 = ColorFromHue(rp);
                        z2 = ColorFromHue((rp + 0.25) % 1.0);
                        z3 = ColorFromHue((rp + 0.50) % 1.0);
                        z4 = ColorFromHue((rp + 0.75) % 1.0);
                        break;

                    case "sw_aurora":
                        var a1 = (Math.Sin(step * 0.35) + 1.0) / 2.0;
                        var a2 = (Math.Sin(step * 0.35 + 0.8) + 1.0) / 2.0;
                        var a3 = (Math.Sin(step * 0.35 + 1.6) + 1.0) / 2.0;
                        var a4 = (Math.Sin(step * 0.35 + 2.4) + 1.0) / 2.0;
                        z1 = LerpHex("00F5D4", "7B2CBF", a1);
                        z2 = LerpHex("00F5D4", "7B2CBF", a2);
                        z3 = LerpHex("00F5D4", "7B2CBF", a3);
                        z4 = LerpHex("00F5D4", "7B2CBF", a4);
                        break;

                    case "sw_battery_fuel":
                        var batteryLevel = ReadBatteryPercentage();
                        var fuelColor = batteryLevel > 50 ? "00E5FF" : (batteryLevel > 20 ? "FFB703" : "E63946");
                        z1 = batteryLevel > 5 ? fuelColor : "1A1D24";
                        z2 = batteryLevel >= 25 ? fuelColor : "1A1D24";
                        z3 = batteryLevel >= 50 ? fuelColor : "1A1D24";
                        z4 = batteryLevel >= 75 ? fuelColor : "1A1D24";
                        hwThrottleMs = 2500;
                        break;

                    default:
                        z1 = z2 = z3 = z4 = "00D4FF";
                        break;
                }

                // Smooth GUI preview at 33 FPS
                Dispatcher.UIThread.Post(() => _updatePreviewCallback(z1, z2, z3, z4));

                // Throttled hardware dispatch only if daemon is not running the effect natively
                if (dispatchToHardware && (DateTime.Now - lastHwDispatch).TotalMilliseconds >= hwThrottleMs)
                {
                    lastHwDispatch = DateTime.Now;
                    _ = _client.SetPerZoneModeAsync(z1, z2, z3, z4, _brightness);
                }

                await Task.Delay(uiIntervalMs, ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DynamicEffectsEngine] Loop error: {ex.Message}");
                await Task.Delay(200, ct);
            }
        }
    }

    #region Helper Math & Color Calculations

    public static double ReadCpuPackageTemperature()
    {
        try
        {
            if (_cachedCpuTempPath == null || !File.Exists(_cachedCpuTempPath))
            {
                // Scan hwmon for coretemp or cpu temp
                var hwmonRoot = "/sys/class/hwmon";
                if (Directory.Exists(hwmonRoot))
                {
                    foreach (var dir in Directory.GetDirectories(hwmonRoot))
                    {
                        var nameFile = Path.Combine(dir, "name");
                        if (File.Exists(nameFile))
                        {
                            var name = File.ReadAllText(nameFile).Trim();
                            if (name.Contains("coretemp", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("k10temp", StringComparison.OrdinalIgnoreCase))
                            {
                                var tempInput = Path.Combine(dir, "temp1_input");
                                if (File.Exists(tempInput))
                                {
                                    _cachedCpuTempPath = tempInput;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (_cachedCpuTempPath != null && File.Exists(_cachedCpuTempPath))
            {
                var content = File.ReadAllText(_cachedCpuTempPath).Trim();
                if (double.TryParse(content, out var milliDeg))
                {
                    return Math.Round(milliDeg / 1000.0, 1);
                }
            }
        }
        catch
        {
            // fallback
        }
        return 50.0;
    }

    public static int ReadBatteryPercentage()
    {
        try
        {
            var capacityFile = "/sys/class/power_supply/BAT1/capacity";
            if (!File.Exists(capacityFile))
                capacityFile = "/sys/class/power_supply/BAT0/capacity";

            if (File.Exists(capacityFile))
            {
                var val = File.ReadAllText(capacityFile).Trim();
                if (int.TryParse(val, out var cap))
                    return cap;
            }
        }
        catch
        {
            // fallback
        }
        return 80;
    }

    private static string GetHeatmapColor(double cpuTemp)
    {
        if (cpuTemp < 48.0)
            return "00E5FF"; // Chill Cyan
        if (cpuTemp < 60.0)
            return "00FF88"; // Cool Emerald Green
        if (cpuTemp < 72.0)
            return "FFB703"; // Warm Gold Amber
        if (cpuTemp < 82.0)
            return "FB8500"; // Hot Orange
        return "FF0033"; // Extreme Crimson Flame
    }

    private static string EmberColor(double intensity)
    {
        // Ember palette: Crimson (C00000) -> Orange (FF4500) -> Gold (FFAA00)
        var clamped = Math.Clamp(intensity, 0.4, 1.2);
        var r = (byte)Math.Clamp(230 * clamped, 120, 255);
        var g = (byte)Math.Clamp(65 * clamped * clamped, 15, 170);
        var b = (byte)Math.Clamp(10 * clamped, 0, 30);
        return $"{r:X2}{g:X2}{b:X2}";
    }

    private static string ColorFromHue(double hue01)
    {
        var h = (hue01 % 1.0) * 360.0;
        var (r, g, b) = HsvToRgb(h, 1.0, 1.0);
        return $"{r:X2}{g:X2}{b:X2}";
    }

    private static (byte r, byte g, byte b) HsvToRgb(double h, double s, double v)
    {
        while (h < 0) h += 360;
        while (h >= 360) h -= 360;

        var c = v * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = v - c;

        double r1 = 0, g1 = 0, b1 = 0;
        if (h < 60) { r1 = c; g1 = x; }
        else if (h < 120) { r1 = x; g1 = c; }
        else if (h < 180) { g1 = c; b1 = x; }
        else if (h < 240) { g1 = x; b1 = c; }
        else if (h < 300) { r1 = x; b1 = c; }
        else { r1 = c; b1 = x; }

        return ((byte)((r1 + m) * 255), (byte)((g1 + m) * 255), (byte)((b1 + m) * 255));
    }

    private static Color ScaleColor(Color col, double factor)
    {
        factor = Math.Clamp(factor, 0.0, 1.0);
        return Color.FromArgb(
            col.A,
            (byte)(col.R * factor),
            (byte)(col.G * factor),
            (byte)(col.B * factor)
        );
    }

    private static string LerpHex(string hexA, string hexB, double factor)
    {
        factor = Math.Clamp(factor, 0.0, 1.0);
        var a = Color.Parse($"#{hexA}");
        var b = Color.Parse($"#{hexB}");
        var r = (byte)(a.R + (b.R - a.R) * factor);
        var g = (byte)(a.G + (b.G - a.G) * factor);
        var bl = (byte)(a.B + (b.B - a.B) * factor);
        return $"{r:X2}{g:X2}{bl:X2}";
    }

    private static string ToHex(Color col) => $"{col.R:X2}{col.G:X2}{col.B:X2}";

    #endregion
}
