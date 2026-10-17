using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using Material.Icons;

namespace PredatorSense;

public class LogoEffectDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaterialIconKind Icon { get; set; }
    public bool SupportsSpeed { get; set; } = true;
    public bool SupportsColor { get; set; } = true;
    public string DefaultColorHex { get; set; } = "00F0FF";
}

public class LogoEffectsEngine
{
    private readonly PredatorSenseClient _client;
    private readonly Action<string> _updateLogoPreviewCallback;
    private CancellationTokenSource? _animationCts;
    private bool _isRunning;
    private LogoEffectDefinition? _currentEffect;
    private int _speed = 5;
    private int _brightness = 100;
    private Color _baseCustomColor = Color.Parse("#00F0FF");
    private static string? _cachedCpuTempPath;

    public static readonly List<LogoEffectDefinition> AvailableLogoEffects = new()
    {
        new LogoEffectDefinition
        {
            Id = "logo_static",
            Name = "Static Emblem",
            Subtitle = "Solid Color",
            Description = "Maintains a steady solid illuminated color on the rear Predator lid emblem.",
            Icon = MaterialIconKind.ShieldStarOutline,
            SupportsSpeed = false,
            SupportsColor = true,
            DefaultColorHex = "00F0FF"
        },
        new LogoEffectDefinition
        {
            Id = "logo_breathing",
            Name = "Breathing Pulse",
            Subtitle = "Gentle Fade",
            Description = "Smoothly pulses the lid emblem brightness from deep shadow to full luminance.",
            Icon = MaterialIconKind.HeartPulse,
            SupportsSpeed = true,
            SupportsColor = true,
            DefaultColorHex = "00F0FF"
        },
        new LogoEffectDefinition
        {
            Id = "logo_rainbow",
            Name = "Chroma Loop",
            Subtitle = "Full Spectrum",
            Description = "Continuously cycles the rear emblem through a radiant 360-degree rainbow spectrum.",
            Icon = MaterialIconKind.PaletteOutline,
            SupportsSpeed = true,
            SupportsColor = false,
            DefaultColorHex = "00FF9D"
        },
        new LogoEffectDefinition
        {
            Id = "logo_police",
            Name = "Police Interceptor",
            Subtitle = "High Speed Flash",
            Description = "Aggressive alternating emergency strobe flashes between high-intensity blue and red.",
            Icon = MaterialIconKind.ShieldAlertOutline,
            SupportsSpeed = true,
            SupportsColor = false,
            DefaultColorHex = "0055FF"
        },
        new LogoEffectDefinition
        {
            Id = "logo_neon",
            Name = "Cyber Neon Shift",
            Subtitle = "Night City",
            Description = "Morphs dynamically between electric cyan, neon magenta, and bright gold.",
            Icon = MaterialIconKind.WeatherSunny,
            SupportsSpeed = true,
            SupportsColor = false,
            DefaultColorHex = "FF007F"
        },
        new LogoEffectDefinition
        {
            Id = "logo_heartbeat",
            Name = "Predator Heartbeat",
            Subtitle = "Double Pulse",
            Description = "Simulates the aggressive double-beat predator pulse of a machine in combat mode.",
            Icon = MaterialIconKind.HeartFlash,
            SupportsSpeed = true,
            SupportsColor = true,
            DefaultColorHex = "FF2A55"
        },
        new LogoEffectDefinition
        {
            Id = "logo_strobe",
            Name = "Glitch Strobe",
            Subtitle = "Tactical Flicker",
            Description = "Rapid high-frequency shutter flash effect for high-energy gaming sessions.",
            Icon = MaterialIconKind.FlashOutline,
            SupportsSpeed = true,
            SupportsColor = true,
            DefaultColorHex = "FFFFFF"
        },
        new LogoEffectDefinition
        {
            Id = "logo_cpu",
            Name = "CPU Thermal Gauge",
            Subtitle = "Hardware Telemetry",
            Description = "Real-time reactive color: Cool Green under 50°C, Amber under 75°C, Blazing Red when hot.",
            Icon = MaterialIconKind.ThermometerAlert,
            SupportsSpeed = false,
            SupportsColor = false,
            DefaultColorHex = "00FF9D"
        },
        new LogoEffectDefinition
        {
            Id = "logo_stealth",
            Name = "Tactical Amber",
            Subtitle = "Stealth Hunter",
            Description = "Subtle warm amber tactical glow with gentle periodic military scanning pulse.",
            Icon = MaterialIconKind.EyeOutline,
            SupportsSpeed = true,
            SupportsColor = false,
            DefaultColorHex = "F59E0B"
        }
    };

    public LogoEffectsEngine(PredatorSenseClient client, Action<string> updateLogoPreviewCallback)
    {
        _client = client;
        _updateLogoPreviewCallback = updateLogoPreviewCallback;
    }

    public bool IsRunning => _isRunning;
    public LogoEffectDefinition? CurrentEffect => _currentEffect;

    public void SetParameters(int speed, int brightness, Color color)
    {
        _speed = Math.Clamp(speed, 1, 10);
        _brightness = Math.Clamp(brightness, 0, 100);
        if (color.R > 8 || color.G > 8 || color.B > 8)
            _baseCustomColor = color;
    }

    public void UpdateSpeed(int speed) => _speed = Math.Clamp(speed, 1, 10);
    public void UpdateBrightness(int brightness) => _brightness = Math.Clamp(brightness, 0, 100);
    public void UpdateColor(Color color)
    {
        if (color.R > 8 || color.G > 8 || color.B > 8)
            _baseCustomColor = color;
    }

    public async Task<bool> StartEffectAsync(LogoEffectDefinition effect)
    {
        await StopAsync();

        _currentEffect = effect;

        if (effect.Id == "logo_static")
        {
            _isRunning = false;
            string hex = ToHex(_baseCustomColor);
            _updateLogoPreviewCallback(hex);
            return await _client.SetBackLogoColorAsync(hex, _brightness, true);
        }

        _animationCts = new CancellationTokenSource();
        _isRunning = true;

        _ = Task.Run(async () =>
        {
            try
            {
                await RunAnimationLoopAsync(effect, _animationCts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[LogoEffectsEngine] Error in effect {effect.Id}: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
            }
        });

        return true;
    }

    public async Task StopAsync()
    {
        if (_animationCts != null)
        {
            _animationCts.Cancel();
            _animationCts.Dispose();
            _animationCts = null;
        }
        _isRunning = false;
        _currentEffect = null;
        await Task.CompletedTask;
    }

    private async Task RunAnimationLoopAsync(LogoEffectDefinition effect, CancellationToken token)
    {
        int step = 0;
        DateTime lastHardwareSend = DateTime.MinValue;
        const int hardwareThrottleMs = 90;

        while (!token.IsCancellationRequested)
        {
            int baseDelayMs = Math.Max(25, 120 - (_speed * 8));
            Color activeColor;
            int currentBrightness = _brightness;

            switch (effect.Id)
            {
                case "logo_breathing":
                {
                    double phase = (step % 60) / 60.0 * 2.0 * Math.PI;
                    double factor = (Math.Sin(phase) + 1.0) / 2.0; // 0.0 to 1.0
                    factor = Math.Max(0.08, factor);
                    activeColor = ScaleColor(_baseCustomColor, factor);
                    baseDelayMs = Math.Max(30, 95 - (_speed * 5));
                    break;
                }
                case "logo_rainbow":
                {
                    double hue = (step * 4) % 360;
                    activeColor = ColorFromHsv(hue, 1.0, 1.0);
                    baseDelayMs = Math.Max(20, 80 - (_speed * 5));
                    break;
                }
                case "logo_police":
                {
                    int cycle = (step / 4) % 2;
                    activeColor = cycle == 0 ? Color.Parse("#FF0033") : Color.Parse("#0066FF");
                    baseDelayMs = Math.Max(35, 140 - (_speed * 10));
                    break;
                }
                case "logo_neon":
                {
                    Color[] neonColors = { Color.Parse("#00F0FF"), Color.Parse("#FF007F"), Color.Parse("#FFE600"), Color.Parse("#7B2CBF") };
                    int colorIdx = (step / 15) % neonColors.Length;
                    int nextIdx = (colorIdx + 1) % neonColors.Length;
                    double blend = (step % 15) / 15.0;
                    activeColor = LerpColor(neonColors[colorIdx], neonColors[nextIdx], blend);
                    baseDelayMs = Math.Max(25, 80 - (_speed * 5));
                    break;
                }
                case "logo_heartbeat":
                {
                    int beatPhase = step % 35;
                    double factor = 0.08;
                    if (beatPhase is >= 0 and <= 4)
                        factor = 1.0;
                    else if (beatPhase is >= 8 and <= 12)
                        factor = 0.75;
                    activeColor = ScaleColor(_baseCustomColor, factor);
                    baseDelayMs = Math.Max(30, 80 - (_speed * 5));
                    break;
                }
                case "logo_strobe":
                {
                    int flash = step % 4;
                    activeColor = flash == 0 ? _baseCustomColor : Color.Parse("#080c14");
                    baseDelayMs = Math.Max(25, 90 - (_speed * 6));
                    break;
                }
                case "logo_cpu":
                {
                    int temp = ReadCpuTemp();
                    if (temp < 50)
                        activeColor = Color.Parse("#00FF9D"); // Cool green
                    else if (temp < 72)
                        activeColor = Color.Parse("#F59E0B"); // Warm amber
                    else
                        activeColor = Color.Parse("#FF2A55"); // Hot red

                    baseDelayMs = 1000;
                    break;
                }
                case "logo_stealth":
                {
                    double phase = (step % 80) / 80.0 * 2.0 * Math.PI;
                    double factor = 0.25 + 0.75 * ((Math.Sin(phase) + 1.0) / 2.0);
                    activeColor = ScaleColor(Color.Parse("#F59E0B"), factor);
                    baseDelayMs = Math.Max(30, 100 - (_speed * 6));
                    break;
                }
                default:
                    activeColor = _baseCustomColor;
                    break;
            }

            string hex = ToHex(activeColor);
            _updateLogoPreviewCallback(hex);

            if ((DateTime.UtcNow - lastHardwareSend).TotalMilliseconds >= hardwareThrottleMs)
            {
                lastHardwareSend = DateTime.UtcNow;
                _ = _client.SetBackLogoColorAsync(hex, currentBrightness, true);
            }

            step++;
            await Task.Delay(baseDelayMs, token);
        }
    }

    private static string ToHex(Color c) => $"{c.R:X2}{c.G:X2}{c.B:X2}";

    private static Color ScaleColor(Color c, double factor)
    {
        factor = Math.Clamp(factor, 0.0, 1.0);
        return Color.FromArgb(c.A, (byte)(c.R * factor), (byte)(c.G * factor), (byte)(c.B * factor));
    }

    private static Color LerpColor(Color c1, Color c2, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        byte r = (byte)(c1.R + (c2.R - c1.R) * t);
        byte g = (byte)(c1.G + (c2.G - c1.G) * t);
        byte b = (byte)(c1.B + (c2.B - c1.B) * t);
        return Color.FromArgb(255, r, g, b);
    }

    private static Color ColorFromHsv(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value *= 255;
        byte v = Convert.ToByte(Math.Clamp(value, 0, 255));
        byte p = Convert.ToByte(Math.Clamp(value * (1 - saturation), 0, 255));
        byte q = Convert.ToByte(Math.Clamp(value * (1 - f * saturation), 0, 255));
        byte t = Convert.ToByte(Math.Clamp(value * (1 - (1 - f) * saturation), 0, 255));

        return hi switch
        {
            0 => Color.FromArgb(255, v, t, p),
            1 => Color.FromArgb(255, q, v, p),
            2 => Color.FromArgb(255, p, v, t),
            3 => Color.FromArgb(255, p, q, v),
            4 => Color.FromArgb(255, t, p, v),
            _ => Color.FromArgb(255, v, p, q)
        };
    }

    private static int ReadCpuTemp()
    {
        try
        {
            if (_cachedCpuTempPath == null)
            {
                for (int i = 0; i < 15; i++)
                {
                    string p = $"/sys/class/hwmon/hwmon{i}/temp1_input";
                    if (File.Exists(p))
                    {
                        _cachedCpuTempPath = p;
                        break;
                    }
                }
            }

            if (_cachedCpuTempPath != null && File.Exists(_cachedCpuTempPath))
            {
                string text = File.ReadAllText(_cachedCpuTempPath).Trim();
                if (int.TryParse(text, out int millideg))
                    return millideg / 1000;
            }
        }
        catch { }
        return 55;
    }
}
