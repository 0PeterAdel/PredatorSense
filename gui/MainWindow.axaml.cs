using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Material.Icons;
using Material.Icons.Avalonia;
using MsBox.Avalonia;

namespace PredatorSense;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const string ProjectVersion = "1.0.2";
    private const string DefaultEffectColor = "#0078D7";
    private const string DefaultZone1Color = "#4287f5";
    private const string DefaultZone2Color = "#ff5733";
    private const string DefaultZone3Color = "#33ff57";
    private const string DefaultZone4Color = "#ffff01";
    private const string DefaultBackLogoColor = "#FFFFFF";
    private const int DirectionLeftToRight = 1;
    private const int DirectionRightToLeft = 2;
    private const string AppDataFolderName = "PredatorSense";
    private const string KeyboardZonePresetFileName = "keyboard-zone-colors.conf";
    private const string KeyboardLightingEffectPresetFileName = "keyboard-lighting-effect.conf";

    private static readonly string AppDataFolderPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDataFolderName
        );

    private static readonly string KeyboardZonePresetPath =
        Path.Combine(AppDataFolderPath, KeyboardZonePresetFileName);

    private static readonly string KeyboardLightingEffectPresetPath =
        Path.Combine(AppDataFolderPath, KeyboardLightingEffectPresetFileName);

    // UI Controls (will be bound via NameScope)
    private Button? _applyBackLogoButton;
    private Button _applyKeyboardColorsButton;
    private RadioButton _autoFanSpeedRadioButton;
    private CheckBox _backlightTimeoutCheckBox;
    private int _backLogoBrightness = 100;
    private Slider? _backLogoBrightnessSlider;
    private TextBlock? _backLogoBrightnessText;
    private ColorPicker? _backLogoColorPicker;
    private CheckBox? _backLogoEnabledCheckBox;
    private TextBlock? _backLogoEffectiveText;
    private RadioButton _balancedProfileButton;
    private CheckBox _batteryLimitCheckBox;
    private CheckBox _bootAnimAndSoundCheckBox;
    private TextBlock _calibrationStatusTextBlock;
    public PredatorSenseClient _client;
    private Slider _cpuFanSlider;
    private int _cpuFanSpeed = 50;
    private TextBlock _cpuFanTextBlock;
    private Grid _daemonErrorGrid;
    private TextBlock _daemonVersionText;
    private TextBlock _driverVersionText;
    private Slider _gpuFanSlider;
    private int _gpuFanSpeed = 70;
    private TextBlock _gpuFanTextBlock;
    private TextBlock _guiVersionTextBlock;
    private bool _isCalibrating;
    private bool _isConnected;
    private bool _isManualFanControl;
    private bool _isSettingFanSpeed;
    private bool _isLoadingSettings;
    private bool _isGlobalStaticActive = true;
    private Button? _applyGlobalColorButton;
    private int _keyboardBrightness = 100;
    private Slider _keyBrightnessSlider;
    private TextBlock _keyBrightnessText;
    private TextBlock _laptopTypeText;
    private CheckBox _lcdOverrideCheckBox;
    private RadioButton _leftToRightRadioButton;
    private ColorPicker _lightEffectColorPicker;
    private Button _lightingEffectsApplyButton;
    private ComboBox _lightingModeComboBox;
    private int _lightingSpeed = 5;
    private Slider _lightingSpeedSlider;
    private TextBlock _lightSpeedTextBlock;
    private RadioButton _lowPowerProfileButton;
    private RadioButton _manualFanSpeedRadioButton;
    private RadioButton _maxFanSpeedRadioButton;
    private TextBlock _modelNameText;
    private RadioButton _performanceProfileButton;
    private PowerSourceDetection _powerDetection;
    private ToggleSwitch _powerToggleSwitch;
    private RadioButton _quietProfileButton;
    private RadioButton _rightToLeftRadioButton;
    private Button _setManualSpeedButton;
    public PredatorSenseSettings _settings;
    private Button _startCalibrationButton;
    private Button _stopCalibrationButton;
    private TextBlock _supportedFeaturesTextBlock;
    private TextBlock _thermalProfileInfoText;
    private RadioButton _turboProfileButton;
    private ComboBox _usbChargingComboBox;
    private ColorPicker _zone1ColorPicker;
    private ColorPicker _zone2ColorPicker;
    private ColorPicker _zone3ColorPicker;
    private ColorPicker _zone4ColorPicker;

    // Realistic Keyboard Visualizer Controls
    private Border? _keyboardChassis;
    private ComboBox? _selectZoneComboBox;
    private Button? _zoneInfoBtn;
    private Border? _spaceKey;
    private Border? _keyNum1;
    private Border? _zone1Card;
    private Border? _zone2Card;
    private Border? _zone3Card;
    private Border? _zone4Card;
    private Border? _zone1Indicator;
    private Border? _zone2Indicator;
    private Border? _zone3Indicator;
    private Border? _zone4Indicator;
    private TextBlock? _activeZoneHintText;

    private readonly List<Border> _allKeyCaps = new();
    private readonly List<Border> _zone1Keys = new();
    private readonly List<Border> _zone2Keys = new();
    private readonly List<Border> _zone3Keys = new();
    private readonly List<Border> _zone4Keys = new();
    private Border? _currentlySelectedKey;
    private bool _suppressZoneComboUpdate;

    // JMA Studio Merged Features
    private Border? _zone1PreviewBox;
    private Border? _zone2PreviewBox;
    private Border? _zone3PreviewBox;
    private Border? _zone4PreviewBox;
    private TextBlock? _zone1PreviewText;
    private TextBlock? _zone2PreviewText;
    private TextBlock? _zone3PreviewText;
    private TextBlock? _zone4PreviewText;
    private ComboBox? _jmaPresetComboBox;
    private Button? _applyJmaPresetButton;
    private Button? _generateGradientButton;
    private Button? _syncAllToZone1Button;
    private Button? _syncBackLogoButton;
    private CheckBox? _instantApplyCheckBox;
    private ScrollViewer? _keyboardLightingScrollViewer;
    private TabControl? _mainTabControl;
    private TabItem? _keyboardLightingTab;

    // Dynamic Effects & Telemetry Controls
    private DynamicEffectsEngine? _dynamicEffectsEngine;
    private ComboBox? _dynamicEffectsSelector;
    private Button? _playDynamicEffectButton;
    private Button? _stopDynamicEffectButton;
    private Slider? _dynamicSpeedSlider;
    private TextBlock? _dynamicSpeedText;
    private ColorPicker? _dynamicEffectColorPicker;
    private RadioButton? _dirLeftToRightButton;
    private RadioButton? _dirRightToLeftButton;
    private MaterialIcon? _dynamicEffectIcon;
    private TextBlock? _dynamicEffectTitleText;
    private TextBlock? _dynamicEffectDescText;
    private TextBlock? _activeModeStatusText;
    private Border? _activeModeDot;
    private TextBlock? _dynamicColorLabel;
    private TextBlock? _dynamicDirectionLabel;
    private StackPanel? _dynamicDirectionPanel;

    // Logo Effects & Controls
    private LogoEffectsEngine? _logoEffectsEngine;
    private LogoEffectDefinition? _selectedLogoEffect;
    private Border? _logoActiveModeDot;
    private TextBlock? _logoActiveModeStatusText;
    private MaterialIcon? _logoEffectIcon;
    private TextBlock? _logoEffectTitleText;
    private TextBlock? _logoEffectDescText;
    private TextBlock? _logoColorLabel;
    private TextBlock? _logoSpeedLabel;
    private Slider? _logoSpeedSlider;
    private TextBlock? _logoSpeedText;
    private Button? _playLogoEffectButton;
    private Button? _stopLogoEffectButton;
    private Border? _logoEmblemGlow;
    private Image? _logoEmblemPreviewImage;
    private Border? _logoEmblemMask;
    private TextBlock? _logoEmblemLiveBadge;
    private Border? _keyboardChassisGlobal;
    private Border? _spaceKeyGlobal;
    private DispatcherTimer? _keyboardBrightnessDebounceTimer;
    private DispatcherTimer? _backLogoBrightnessDebounceTimer;
    private DispatcherTimer? _zoneColorDebounceTimer;
    private bool _isLogoStaticActive = true;

    // Header Quick Telemetry
    private TextBlock? _headerCpuTempText;
    private TextBlock? _headerGpuTempText;
    private TextBlock? _headerBatteryText;
    private MaterialIcon? _headerPowerIcon;
    private Border? _headerDaemonBadge;
    private DispatcherTimer? _telemetryTimer;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _client = new PredatorSenseClient();
        Loaded += MainWindow_Loaded;
    }

    public bool IsCalibrating
    {
        get => _isCalibrating;
        set => SetField(ref _isCalibrating, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        BindControls();
        AttachEventHandlers();
        InitializeWallpaperPalette();
        ResetKeyboardLightingScroll();
        if (_mainTabControl != null)
            _mainTabControl.SelectedIndex = 0;

        var args = Environment.GetCommandLineArgs();
        foreach (var arg in args)
        {
            if (arg == "--tab=rgb" && _mainTabControl != null)
            {
                _mainTabControl.SelectedIndex = 3;
            }
            if (arg == "--subtab=area")
            {
                var rArea = this.FindNameScope().Find<RadioButton>("RgbTabAreaBtn");
                if (rArea != null) rArea.IsChecked = true;
            }
            else if (arg == "--subtab=logo")
            {
                var rLogo = this.FindNameScope().Find<RadioButton>("RgbTabLogoBtn");
                if (rLogo != null) rLogo.IsChecked = true;
            }
            else if (arg == "--subtab=global")
            {
                var rGlob = this.FindNameScope().Find<RadioButton>("RgbTabGlobalBtn");
                if (rGlob != null) rGlob.IsChecked = true;
            }
            else if (arg.StartsWith("--theme="))
            {
                var themeTag = arg.Substring("--theme=".Length);
                Dispatcher.UIThread.Post(() =>
                {
                    if (_curatedPresets.TryGetValue(themeTag, out var col))
                    {
                        SetColorPicker(_zone1ColorPicker, col.z1);
                        SetColorPicker(_zone2ColorPicker, col.z2);
                        SetColorPicker(_zone3ColorPicker, col.z3);
                        SetColorPicker(_zone4ColorPicker, col.z4);
                        UpdateZonePreviews();
                    }
                }, DispatcherPriority.Loaded);
            }
        }

        InitializeAsync();
    }

    private void BindControls()
    {
        var nameScope = this.FindNameScope();

        // Thermal Profile controls
        _lowPowerProfileButton = nameScope.Find<RadioButton>("LowPowerProfileButton");
        _quietProfileButton = nameScope.Find<RadioButton>("QuietProfileButton");
        _balancedProfileButton = nameScope.Find<RadioButton>("BalancedProfileButton");
        _performanceProfileButton = nameScope.Find<RadioButton>("PerformanceProfileButton");
        _turboProfileButton = nameScope.Find<RadioButton>("TurboProfileButton");
        _powerToggleSwitch = nameScope.Find<ToggleSwitch>("PluggedInToggleSwitch");

        // Fan control controls
        _manualFanSpeedRadioButton = nameScope.Find<RadioButton>("ManualFanSpeedRadioButton");
        _maxFanSpeedRadioButton = nameScope.Find<RadioButton>("MaxFanSpeedRadioButton");
        _cpuFanSlider = nameScope.Find<Slider>("CpuFanSlider");
        _gpuFanSlider = nameScope.Find<Slider>("GpuFanSlider");
        _cpuFanTextBlock = nameScope.Find<TextBlock>("CpuFanTextBlock");
        _gpuFanTextBlock = nameScope.Find<TextBlock>("GpuFanTextBlock");
        _setManualSpeedButton = nameScope.Find<Button>("SetManualSpeedButton");
        _autoFanSpeedRadioButton = nameScope.Find<RadioButton>("AutoFanSpeedRadioButton");

        // Battery calibration controls
        _startCalibrationButton = nameScope.Find<Button>("StartCalibrationButton");
        _stopCalibrationButton = nameScope.Find<Button>("StopCalibrationButton");
        _calibrationStatusTextBlock = nameScope.Find<TextBlock>("CalibrationStatusTextBlock");
        _batteryLimitCheckBox = nameScope.Find<CheckBox>("BatteryLimitCheckBox");

        // USB charging controls
        _usbChargingComboBox = nameScope.Find<ComboBox>("UsbChargingComboBox");

        // Keyboard lighting zone controls
        _zone1ColorPicker = nameScope.Find<ColorPicker>("Zone1ColorPicker");
        _zone2ColorPicker = nameScope.Find<ColorPicker>("Zone2ColorPicker");
        _zone3ColorPicker = nameScope.Find<ColorPicker>("Zone3ColorPicker");
        _zone4ColorPicker = nameScope.Find<ColorPicker>("Zone4ColorPicker");
        _keyBrightnessSlider = nameScope.Find<Slider>("KeyBrightnessSlider");
        _keyBrightnessText = nameScope.Find<TextBlock>("KeyBrightnessText");
        _applyKeyboardColorsButton = nameScope.Find<Button>("ApplyKeyboardColorsButton");

        // Realistic Keyboard Visualizer Controls
        _keyboardChassis = nameScope.Find<Border>("KeyboardChassis");
        _keyboardChassisGlobal = nameScope.Find<Border>("KeyboardChassisGlobal");
        _selectZoneComboBox = nameScope.Find<ComboBox>("SelectZoneComboBox");
        _zoneInfoBtn = nameScope.Find<Button>("ZoneInfoBtn");
        _spaceKey = nameScope.Find<Border>("SpaceKey");
        _spaceKeyGlobal = nameScope.Find<Border>("SpaceKeyGlobal");
        _keyNum1 = nameScope.Find<Border>("KeyNum1");
        _zone1Card = nameScope.Find<Border>("Zone1Card");
        _zone2Card = nameScope.Find<Border>("Zone2Card");
        _zone3Card = nameScope.Find<Border>("Zone3Card");
        _zone4Card = nameScope.Find<Border>("Zone4Card");
        _zone1Indicator = nameScope.Find<Border>("Zone1Indicator");
        _zone2Indicator = nameScope.Find<Border>("Zone2Indicator");
        _zone3Indicator = nameScope.Find<Border>("Zone3Indicator");
        _zone4Indicator = nameScope.Find<Border>("Zone4Indicator");
        _activeZoneHintText = nameScope.Find<TextBlock>("ActiveZoneHintText");

        // JMA Studio Merged Controls
        _zone1PreviewBox = nameScope.Find<Border>("Zone1PreviewBox");
        _zone2PreviewBox = nameScope.Find<Border>("Zone2PreviewBox");
        _zone3PreviewBox = nameScope.Find<Border>("Zone3PreviewBox");
        _zone4PreviewBox = nameScope.Find<Border>("Zone4PreviewBox");
        _zone1PreviewText = nameScope.Find<TextBlock>("Zone1PreviewText");
        _zone2PreviewText = nameScope.Find<TextBlock>("Zone2PreviewText");
        _zone3PreviewText = nameScope.Find<TextBlock>("Zone3PreviewText");
        _zone4PreviewText = nameScope.Find<TextBlock>("Zone4PreviewText");
        _jmaPresetComboBox = nameScope.Find<ComboBox>("JmaPresetComboBox");
        _applyJmaPresetButton = nameScope.Find<Button>("ApplyJmaPresetButton");
        _generateGradientButton = nameScope.Find<Button>("GenerateGradientButton");
        _syncAllToZone1Button = nameScope.Find<Button>("SyncAllToZone1Button");
        _syncBackLogoButton = nameScope.Find<Button>("SyncBackLogoButton");
        _instantApplyCheckBox = nameScope.Find<CheckBox>("InstantApplyCheckBox");

        InitializeKeyboardVisualizer();

        // Back logo / lightbar controls
        _backLogoColorPicker = nameScope.Find<ColorPicker>("BackLogoColorPicker");
        _backLogoBrightnessSlider = nameScope.Find<Slider>("BackLogoBrightnessSlider");
        _backLogoBrightnessText = nameScope.Find<TextBlock>("BackLogoBrightnessText");
        _backLogoEnabledCheckBox = nameScope.Find<CheckBox>("BackLogoEnabledCheckBox");
        _backLogoEffectiveText = nameScope.Find<TextBlock>("BackLogoEffectiveText");
        _applyBackLogoButton = nameScope.Find<Button>("ApplyBackLogoButton");

        // Logo Effects Controls
        _logoActiveModeDot = nameScope.Find<Border>("LogoActiveModeDot");
        _logoActiveModeStatusText = nameScope.Find<TextBlock>("LogoActiveModeStatusText");
        _logoEffectIcon = nameScope.Find<MaterialIcon>("LogoEffectIcon");
        _logoEffectTitleText = nameScope.Find<TextBlock>("LogoEffectTitleText");
        _logoEffectDescText = nameScope.Find<TextBlock>("LogoEffectDescText");
        _logoColorLabel = nameScope.Find<TextBlock>("LogoColorLabel");
        _logoSpeedLabel = nameScope.Find<TextBlock>("LogoSpeedLabel");
        _logoSpeedSlider = nameScope.Find<Slider>("LogoSpeedSlider");
        _logoSpeedText = nameScope.Find<TextBlock>("LogoSpeedText");
        _playLogoEffectButton = nameScope.Find<Button>("PlayLogoEffectButton");
        _stopLogoEffectButton = nameScope.Find<Button>("StopLogoEffectButton");
        _logoEmblemGlow = nameScope.Find<Border>("LogoEmblemGlow");
        _logoEmblemPreviewImage = nameScope.Find<Image>("LogoEmblemPreviewImage");
        _logoEmblemMask = nameScope.Find<Border>("LogoEmblemMask");
        _logoEmblemLiveBadge = nameScope.Find<TextBlock>("LogoEmblemLiveBadge");

        // Lighting effects controls (legacy + dynamic)
        _lightingModeComboBox = nameScope.Find<ComboBox>("LightingModeComboBox");
        _lightingSpeedSlider = nameScope.Find<Slider>("LightingSpeedSlider");
        _lightSpeedTextBlock = nameScope.Find<TextBlock>("LightSpeedTextBlock");
        _lightEffectColorPicker = nameScope.Find<ColorPicker>("LightEffectColorPicker");
        _leftToRightRadioButton = nameScope.Find<RadioButton>("LeftToRightRadioButton");
        _rightToLeftRadioButton = nameScope.Find<RadioButton>("RightToLeftRadioButton");
        _lightingEffectsApplyButton = nameScope.Find<Button>("LightingEffectsApplyButton");

        // Dynamic Lighting Effects Controls
        _dynamicEffectsSelector = nameScope.Find<ComboBox>("DynamicEffectsSelector");
        _playDynamicEffectButton = nameScope.Find<Button>("PlayDynamicEffectButton");
        _stopDynamicEffectButton = nameScope.Find<Button>("StopDynamicEffectButton");
        _dynamicSpeedSlider = nameScope.Find<Slider>("DynamicSpeedSlider");
        _dynamicSpeedText = nameScope.Find<TextBlock>("DynamicSpeedText");
        _dynamicEffectColorPicker = nameScope.Find<ColorPicker>("DynamicEffectColorPicker");
        _dirLeftToRightButton = nameScope.Find<RadioButton>("DirLeftToRightButton");
        _dirRightToLeftButton = nameScope.Find<RadioButton>("DirRightToLeftButton");
        _dynamicEffectIcon = nameScope.Find<MaterialIcon>("DynamicEffectIcon");
        _dynamicEffectTitleText = nameScope.Find<TextBlock>("DynamicEffectTitleText");
        _dynamicEffectDescText = nameScope.Find<TextBlock>("DynamicEffectDescText");
        _activeModeStatusText = nameScope.Find<TextBlock>("ActiveModeStatusText");
        _activeModeDot = nameScope.Find<Border>("ActiveModeDot");
        _dynamicColorLabel = nameScope.Find<TextBlock>("DynamicColorLabel");
        _dynamicDirectionLabel = nameScope.Find<TextBlock>("DynamicDirectionLabel");
        _dynamicDirectionPanel = nameScope.Find<StackPanel>("DynamicDirectionPanel");
        _applyGlobalColorButton = nameScope.Find<Button>("ApplyGlobalColorButton");

        // Header Quick Telemetry
        _headerCpuTempText = nameScope.Find<TextBlock>("HeaderCpuTempText");
        _headerGpuTempText = nameScope.Find<TextBlock>("HeaderGpuTempText");
        _headerBatteryText = nameScope.Find<TextBlock>("HeaderBatteryText");
        _headerPowerIcon = nameScope.Find<MaterialIcon>("HeaderPowerIcon");
        _headerDaemonBadge = nameScope.Find<Border>("HeaderDaemonBadge");

        // System settings controls
        _backlightTimeoutCheckBox = nameScope.Find<CheckBox>("BacklightTimeoutCheckBox");
        _lcdOverrideCheckBox = nameScope.Find<CheckBox>("LcdOverrideCheckBox");
        _bootAnimAndSoundCheckBox = nameScope.Find<CheckBox>("BootAnimAndSoundCheckBox");

        // Info Texts
        _thermalProfileInfoText = nameScope.Find<TextBlock>("ThermalProfileInfoText");
        _modelNameText = nameScope.Find<TextBlock>("ModelNameText");
        _laptopTypeText = nameScope.Find<TextBlock>("LaptopTypeText");
        _supportedFeaturesTextBlock = nameScope.Find<TextBlock>("SupportedFeaturesTextBlock");
        _daemonVersionText = nameScope.Find<TextBlock>("DaemonVersionText");
        _driverVersionText = nameScope.Find<TextBlock>("DriverVersionText");
        _guiVersionTextBlock = nameScope.Find<TextBlock>("ProjectVersionText");
        _daemonErrorGrid = nameScope.Find<Grid>("DaemonErrorGrid");
        _keyboardLightingScrollViewer = nameScope.Find<ScrollViewer>("KeyboardLightingScrollViewer");
        _mainTabControl = nameScope.Find<TabControl>("MainTabControl");
        _keyboardLightingTab = nameScope.Find<TabItem>("KeyboardLightingPanel");

        // Set initial GUI version
        if (_guiVersionTextBlock != null)
            _guiVersionTextBlock.Text = $"v{ProjectVersion}";
    }

    private void AttachEventHandlers()
    {
        // Thermal Profile handlers
        if (_lowPowerProfileButton != null) _lowPowerProfileButton.IsCheckedChanged += ProfileButton_Checked;
        if (_quietProfileButton != null) _quietProfileButton.IsCheckedChanged += ProfileButton_Checked;
        if (_balancedProfileButton != null) _balancedProfileButton.IsCheckedChanged += ProfileButton_Checked;
        if (_performanceProfileButton != null) _performanceProfileButton.IsCheckedChanged += ProfileButton_Checked;
        if (_turboProfileButton != null) _turboProfileButton.IsCheckedChanged += ProfileButton_Checked;

        // Power toggle switch
        if (_powerToggleSwitch != null)
        {
            _powerDetection = new PowerSourceDetection(_powerToggleSwitch);
            _powerToggleSwitch.PropertyChanged += (s, args) =>
            {
                if (args.Property.Name == "IsChecked") UpdateUIBasedOnPowerSource();
            };
            UpdateUIBasedOnPowerSource();
        }

        // Fan control handlers
        if (_manualFanSpeedRadioButton != null) _manualFanSpeedRadioButton.Click += ManualFanControlRadioBox_Click;
        if (_cpuFanSlider != null) _cpuFanSlider.PropertyChanged += CpuFanSlider_ValueChanged;
        if (_gpuFanSlider != null) _gpuFanSlider.PropertyChanged += GpuFanSlider_ValueChanged;
        if (_autoFanSpeedRadioButton != null) _autoFanSpeedRadioButton.Click += AutoFanSpeedRadioButtonClick;
        if (_setManualSpeedButton != null) _setManualSpeedButton.Click += SetManualSpeedButton_OnClick;

        // Battery calibration handlers
        if (_startCalibrationButton != null) _startCalibrationButton.Click += StartCalibrationButton_Click;
        if (_stopCalibrationButton != null) _stopCalibrationButton.Click += StopCalibrationButton_Click;
        if (_batteryLimitCheckBox != null) _batteryLimitCheckBox.Click += BatteryLimitCheckBox_Click;


        // Keyboard lighting handlers
        if (_keyBrightnessSlider != null) _keyBrightnessSlider.PropertyChanged += KeyboardBrightnessSlider_ValueChanged;
        if (_applyKeyboardColorsButton != null) _applyKeyboardColorsButton.Click += ApplyKeyboardColorsButton_Click;

        // JMA Studio Merged Handlers
        if (_zone1ColorPicker != null) _zone1ColorPicker.PropertyChanged += ZoneColorPicker_PropertyChanged;
        if (_zone2ColorPicker != null) _zone2ColorPicker.PropertyChanged += ZoneColorPicker_PropertyChanged;
        if (_zone3ColorPicker != null) _zone3ColorPicker.PropertyChanged += ZoneColorPicker_PropertyChanged;
        if (_zone4ColorPicker != null) _zone4ColorPicker.PropertyChanged += ZoneColorPicker_PropertyChanged;

        if (_applyJmaPresetButton != null) _applyJmaPresetButton.Click += ApplyJmaPresetButton_Click;
        if (_jmaPresetComboBox != null) _jmaPresetComboBox.SelectionChanged += JmaPresetComboBox_SelectionChanged;
        if (_generateGradientButton != null) _generateGradientButton.Click += GenerateGradientButton_Click;
        if (_syncAllToZone1Button != null) _syncAllToZone1Button.Click += SyncAllToZone1Button_Click;
        if (_syncBackLogoButton != null) _syncBackLogoButton.Click += SyncBackLogoButton_Click;

        // Back logo / lightbar handlers
        if (_backLogoBrightnessSlider != null) _backLogoBrightnessSlider.PropertyChanged += BackLogoBrightnessSlider_ValueChanged;
        if (_backLogoEnabledCheckBox != null) _backLogoEnabledCheckBox.Click += BackLogoEnabledCheckBox_Click;
        if (_applyBackLogoButton != null) _applyBackLogoButton.Click += ApplyBackLogoButton_Click;
        if (_backLogoColorPicker != null) _backLogoColorPicker.PropertyChanged += BackLogoColorPicker_PropertyChanged;
        if (_logoSpeedSlider != null) _logoSpeedSlider.PropertyChanged += LogoSpeedSlider_PropertyChanged;

        // Initialize Logo Effects Engine
        _logoEffectsEngine = new LogoEffectsEngine(_client, OnLogoEffectsPreviewUpdate);

        // Lighting effects handlers (legacy)
        if (_lightingSpeedSlider != null) _lightingSpeedSlider.PropertyChanged += LightingSpeedSlider_ValueChanged;
        if (_lightingEffectsApplyButton != null) _lightingEffectsApplyButton.Click += LightingEffectsApplyButton_Click;

        // Dynamic Lighting Effects Handlers
        if (_dynamicEffectsSelector != null)
            _dynamicEffectsSelector.SelectionChanged += DynamicEffectsSelector_SelectionChanged;

        if (_playDynamicEffectButton != null)
            _playDynamicEffectButton.Click += PlayDynamicEffectButton_Click;

        if (_stopDynamicEffectButton != null)
            _stopDynamicEffectButton.Click += StopDynamicEffectButton_Click;

        if (_dynamicSpeedSlider != null)
            _dynamicSpeedSlider.PropertyChanged += DynamicSpeedSlider_PropertyChanged;

        if (_dynamicEffectColorPicker != null)
            _dynamicEffectColorPicker.PropertyChanged += DynamicEffectColorPicker_PropertyChanged;

        if (_dirLeftToRightButton != null)
            _dirLeftToRightButton.IsCheckedChanged += (s, e) => UpdateDynamicParameters();

        if (_dirRightToLeftButton != null)
            _dirRightToLeftButton.IsCheckedChanged += (s, e) => UpdateDynamicParameters();

        if (_applyGlobalColorButton != null)
            _applyGlobalColorButton.Click += ApplyGlobalColor_Click;

        // Initialize Dynamic Effects Engine with Live UI Preview
        _dynamicEffectsEngine = new DynamicEffectsEngine(_client, OnDynamicEffectsPreviewUpdate);

        // Initial Dynamic Effect Selection UI
        UpdateDynamicEffectSelectionUI();

        // Start Real-Time Quick Telemetry Timer
        StartTelemetryTimer();

        // System settings handlers
        if (_backlightTimeoutCheckBox != null) _backlightTimeoutCheckBox.Click += BacklightTimeoutCheckBox_Click;
        if (_lcdOverrideCheckBox != null) _lcdOverrideCheckBox.Click += LcdOverrideCheckBox_Click;
        if (_bootAnimAndSoundCheckBox != null) _bootAnimAndSoundCheckBox.Click += BootSoundCheckBox_Click;

        if (_mainTabControl != null)
        {
            _mainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
        }
    }

    private void MainTabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.Source != _mainTabControl) return;
        if (_mainTabControl?.SelectedItem == _keyboardLightingTab)
        {
            ResetKeyboardLightingScroll();
        }
    }

    private void ResetKeyboardLightingScroll()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_keyboardLightingScrollViewer != null)
            {
                _keyboardLightingScrollViewer.Offset = new Avalonia.Vector(0, 0);
            }
        }, DispatcherPriority.Loaded);

        Dispatcher.UIThread.Post(() =>
        {
            if (_keyboardLightingScrollViewer != null)
            {
                _keyboardLightingScrollViewer.Offset = new Avalonia.Vector(0, 0);
            }
        }, DispatcherPriority.Render);

        Dispatcher.UIThread.Post(() =>
        {
            if (_keyboardLightingScrollViewer != null)
            {
                _keyboardLightingScrollViewer.Offset = new Avalonia.Vector(0, 0);
            }
        }, DispatcherPriority.SystemIdle);
    }

    private void UpdateUIElementVisibility()
    {
        if (_settings == null) return;

        UpdateWallpaperPaletteAvailability();

        var nameScope = this.FindNameScope();
        var thermalProfilePanel = nameScope.Find<Border>("ThermalProfilePanel");
        var fanControlPanel = nameScope.Find<Border>("FanControlPanel");
        var batteryTab = nameScope.Find<TabItem>("BatteryPanel");
        var usbChargingPanel = nameScope.Find<Border>("UsbChargingPanel");
        var keyboardLightingTab = nameScope.Find<TabItem>("KeyboardLightingPanel");
        var zoneColorControlPanel = nameScope.Find<Border>("ZoneColorControlPanel");
        var keyboardEffectsPanel = nameScope.Find<Border>("KeyboardEffectsPanel");
        var systemSettingsTab = nameScope.Find<TabItem>("SystemSettingsPanel");

        if (thermalProfilePanel != null)
            thermalProfilePanel.IsVisible = _client.IsFeatureAvailable("thermal_profile") || AppState.DevMode;

        if (fanControlPanel != null)
            fanControlPanel.IsVisible = _client.IsFeatureAvailable("fan_speed") || AppState.DevMode;

        if (batteryTab != null)
        {
            var hasBatteryFeatures = _client.IsFeatureAvailable("battery_calibration") ||
                                     _client.IsFeatureAvailable("battery_limiter");
            batteryTab.IsVisible = hasBatteryFeatures;

            var calibrationControls = nameScope.Find<Border>("CalibrationControls");
            var limiterControls = nameScope.Find<Border>("LimiterControls");

            if (calibrationControls != null)
                calibrationControls.IsVisible = _client.IsFeatureAvailable("battery_calibration") || AppState.DevMode;

            if (limiterControls != null)
                limiterControls.IsVisible = _client.IsFeatureAvailable("battery_limiter") || AppState.DevMode;
        }

        var hasKeyboardFeatures = _client.IsFeatureAvailable("backlight_timeout") ||
                                  _client.IsFeatureAvailable("per_zone_mode") ||
                                  _client.IsFeatureAvailable("four_zone_mode") ||
                                  _client.IsFeatureAvailable("back_logo");

        if (keyboardLightingTab != null)
            keyboardLightingTab.IsVisible = hasKeyboardFeatures;

        if (zoneColorControlPanel != null)
            zoneColorControlPanel.IsVisible = _client.IsFeatureAvailable("per_zone_mode") || AppState.DevMode;

        if (keyboardEffectsPanel != null)
            keyboardEffectsPanel.IsVisible = _client.IsFeatureAvailable("four_zone_mode") || AppState.DevMode;

        var dynamicEffectsPanel = nameScope.Find<Border>("DynamicEffectsPanel");
        if (dynamicEffectsPanel != null)
            dynamicEffectsPanel.IsVisible = _client.IsFeatureAvailable("four_zone_mode") || _client.IsFeatureAvailable("per_zone_mode") || AppState.DevMode;

        var backLogoControls = nameScope.Find<Border>("BackLogoControls");
        if (backLogoControls != null)
            backLogoControls.IsVisible = _client.IsFeatureAvailable("back_logo") || AppState.DevMode;

        if (usbChargingPanel != null)
            usbChargingPanel.IsVisible = _client.IsFeatureAvailable("usb_charging") || AppState.DevMode;

        if (systemSettingsTab != null)
        {
            var hasSystemSettings = _client.IsFeatureAvailable("lcd_override") ||
                                    _client.IsFeatureAvailable("boot_animation_sound");

            var backlightControls = nameScope.Find<Border>("BacklightTimeoutControls");
            var lcdControls = nameScope.Find<Border>("LcdOverrideControls");
            var bootSoundControls = nameScope.Find<Border>("BootSoundControls");

            if (backlightControls != null)
                backlightControls.IsVisible = _client.IsFeatureAvailable("backlight_timeout") || AppState.DevMode;

            if (lcdControls != null)
                lcdControls.IsVisible = _client.IsFeatureAvailable("lcd_override") || AppState.DevMode;

            if (bootSoundControls != null)
                bootSoundControls.IsVisible = _client.IsFeatureAvailable("boot_animation_sound") || AppState.DevMode;
        }
    }

    private void UpdateUIBasedOnPowerSource()
    {
        var isPluggedIn = _powerToggleSwitch?.IsChecked ?? false;

        if (_lowPowerProfileButton != null)
            _lowPowerProfileButton.IsVisible = _lowPowerProfileButton.IsEnabled && !isPluggedIn;

        if (_quietProfileButton != null)
            _quietProfileButton.IsVisible = _quietProfileButton.IsEnabled && isPluggedIn;

        if (_balancedProfileButton != null)
            _balancedProfileButton.IsVisible = _balancedProfileButton.IsEnabled;

        if (_performanceProfileButton != null)
            _performanceProfileButton.IsVisible = _performanceProfileButton.IsEnabled && isPluggedIn;

        if (_turboProfileButton != null)
            _turboProfileButton.IsVisible = _turboProfileButton.IsEnabled && isPluggedIn;

        if (_balancedProfileButton != null &&
            ((_lowPowerProfileButton?.IsChecked == true && !_lowPowerProfileButton.IsVisible) ||
             (_quietProfileButton?.IsChecked == true && !_quietProfileButton.IsVisible) ||
             (_performanceProfileButton?.IsChecked == true && !_performanceProfileButton.IsVisible) ||
             (_turboProfileButton?.IsChecked == true && !_turboProfileButton.IsVisible)))
            _balancedProfileButton.IsChecked = true;
    }

    public async void InitializeAsync()
    {
        try
        {
            _isConnected = await _client.ConnectAsync();
            if (_isConnected)
            {
                _daemonErrorGrid.IsVisible = false;
                await LoadSettingsAsync();
            }
            else
            {
                await ShowMessageBox(
                    "Error Connecting to Daemon",
                    "Failed to connect to PredatorSense daemon. The Daemon may be initializing please wait.");
                _daemonErrorGrid.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await ShowMessageBox("Error while initializing", $"Error initializing: {ex.Message}");
            _daemonErrorGrid.IsVisible = true;
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            _isLoadingSettings = true;
            _settings = await _client.GetAllSettingsAsync() ?? new PredatorSenseSettings();
            ApplySettingsToUI();
            ResetKeyboardLightingScroll();
        }
        catch (Exception ex)
        {
            await ShowMessageBox("Error while loading settings", $"Error loading settings: {ex.Message}");
            _settings = new PredatorSenseSettings();
            ApplySettingsToUI();
            ResetKeyboardLightingScroll();
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void UpdateProfileButtons()
    {
        if (_settings?.ThermalProfile == null) return;

        var isPluggedIn = _powerToggleSwitch?.IsChecked ?? false;
        var profileConfigs =
            new Dictionary<string, (RadioButton button, string description, bool showOnBattery, bool showOnAC)>
            {
                {
                    "low-power",
                    (_lowPowerProfileButton,
                        "Prioritizes energy efficiency, reduces performance to extend battery life.", true, false)
                },
                { "quiet", (_quietProfileButton, "Minimizes noise, prioritizes low power and cooling.", false, true) },
                {
                    "balanced",
                    (_balancedProfileButton, "Optimal mix of performance and noise for everyday tasks.", true, true)
                },
                {
                    "balanced-performance",
                    (_performanceProfileButton, "Maximizes speed for demanding workloads, higher fan noise", false,
                        true)
                },
                {
                    "performance",
                    (_turboProfileButton, "Unleashes peak power for extreme tasks, loudest fans.", false, true)
                }
            };

        foreach (var config in profileConfigs.Values)
            if (config.button != null)
            {
                config.button.IsVisible = false;
                config.button.IsEnabled = false;
            }

        foreach (var profile in _settings.ThermalProfile.Available)
        {
            var profileKey = profile.ToLower();
            if (profileConfigs.TryGetValue(profileKey, out var config) && config.button != null)
            {
                var shouldShow = isPluggedIn ? config.showOnAC : config.showOnBattery;
                config.button.IsEnabled = true;
                config.button.IsVisible = shouldShow || AppState.DevMode;
            }
        }

        if (!string.IsNullOrEmpty(_settings.ThermalProfile.Current))
        {
            var currentProfileKey = _settings.ThermalProfile.Current.ToLower();
            if (profileConfigs.TryGetValue(currentProfileKey, out var config) && config.button?.IsEnabled == true)
            {
                config.button.IsChecked = true;
                if (_thermalProfileInfoText != null)
                    _thermalProfileInfoText.Text = config.description;
            }
        }
    }

    private void ApplySettingsToUI()
    {
        UpdateProfileButtons();

        SetCheckBox(_backlightTimeoutCheckBox, IsEnabledSetting(_settings.BacklightTimeout));
        SetCheckBox(_batteryLimitCheckBox, IsEnabledSetting(_settings.BatteryLimiter));

        var isCalibrating = IsEnabledSetting(_settings.BatteryCalibration);
        IsCalibrating = isCalibrating;
        SetEnabled(_startCalibrationButton, !isCalibrating);
        SetEnabled(_stopCalibrationButton, isCalibrating);
        SetText(_calibrationStatusTextBlock, isCalibrating ? "Status: Calibrating" : "Status: Not calibrating");

        SetCheckBox(_bootAnimAndSoundCheckBox, IsEnabledSetting(_settings.BootAnimationSound));
        SetCheckBox(_lcdOverrideCheckBox, IsEnabledSetting(_settings.LcdOverride));

        if (_usbChargingComboBox != null)
            _usbChargingComboBox.SelectedIndex = GetUsbChargingIndex(_settings.UsbCharging);

        var cpuSpeed = ApplyFanSpeed(_settings.FanSpeed?.Cpu, ref _cpuFanSpeed, _cpuFanSlider, _cpuFanTextBlock);
        var gpuSpeed = ApplyFanSpeed(_settings.FanSpeed?.Gpu, ref _gpuFanSpeed, _gpuFanSlider, _gpuFanTextBlock);

        var isAutoMode = cpuSpeed == 0 && gpuSpeed == 0;
        var isMaxMode = cpuSpeed == 100 && gpuSpeed == 100;
        var isManualMode = !isAutoMode && !isMaxMode;

        _isManualFanControl = isManualMode;

        if (_autoFanSpeedRadioButton != null)
            _autoFanSpeedRadioButton.IsChecked = isAutoMode;

        if (_maxFanSpeedRadioButton != null)
            _maxFanSpeedRadioButton.IsChecked = isMaxMode;

        if (_manualFanSpeedRadioButton != null)
            _manualFanSpeedRadioButton.IsChecked = isManualMode;

        ApplyKeyboardSettings();
        ApplyBackLogoSettingsToUI();

        SetText(_keyBrightnessText, $"{_keyboardBrightness}%");
        SetText(_lightSpeedTextBlock, _lightingSpeed.ToString());
        SetText(_daemonVersionText, $"v{_settings.Version}");
        SetText(_driverVersionText, $"v{_settings.DriverVersion}");
        SetText(_laptopTypeText, _settings.LaptopType);
        SetText(_supportedFeaturesTextBlock, string.Join(", ", _settings.AvailableFeatures));
        SetText(_modelNameText, GetLinuxLaptopModel());

        UpdateUIElementVisibility();
    }

    private static bool IsEnabledSetting(string? value)
    {
        return (value ?? "0").Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetUsbChargingIndex(string? value)
    {
        return value switch
        {
            "10" => 1,
            "20" => 2,
            "30" => 3,
            _ => 0
        };
    }

    private static int ApplyFanSpeed(
        string? value,
        ref int backingField,
        Slider? slider,
        TextBlock? textBlock)
    {
        if (!int.TryParse(value ?? "0", out var speed))
            return 0;

        backingField = speed;

        if (slider != null)
            slider.Value = speed;

        SetText(textBlock, FormatFanSpeed(speed));

        return speed;
    }

    private static string FormatFanSpeed(int speed)
    {
        return speed == 0 ? "Auto" : $"{speed}%";
    }

    private static void SetCheckBox(CheckBox? checkBox, bool value)
    {
        if (checkBox != null)
            checkBox.IsChecked = value;
    }

    private static void SetRadioButton(RadioButton? radioButton, bool value)
    {
        if (radioButton != null)
            radioButton.IsChecked = value;
    }

    private static void SetEnabled(Control? control, bool value)
    {
        if (control != null)
            control.IsEnabled = value;
    }

    private static void SetText(TextBlock? textBlock, string? value)
    {
        if (textBlock != null)
            textBlock.Text = value ?? string.Empty;
    }

    private string GetLinuxLaptopModel()
    {
        try
        {
            if (File.Exists("/sys/class/dmi/id/product_name"))
                return File.ReadAllText("/sys/class/dmi/id/product_name").Trim();

            var startInfo = new ProcessStartInfo
            {
                FileName = "dmidecode",
                Arguments = "-s system-product-name",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.StandardOutput.ReadToEnd().Trim() ?? "Unknown";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting laptop model: {ex.Message}");
            return "Unknown";
        }
    }

    private void ApplyKeyboardSettings()
    {
        if (!_settings.HasFourZoneKb)
            return;

        ApplySavedZonePresetToUI();

        if (!HasSavedZonePreset())
            ApplyPerZoneSettingsToUI();

        ApplyFourZoneSettingsToUI();
        ApplySavedLightingEffectPresetToUI();
    }

    private void ApplyPerZoneSettingsToUI()
    {
        if (!TryParsePerZoneMode(
                _settings.PerZoneMode,
                out var zone1,
                out var zone2,
                out var zone3,
                out var zone4,
                out var brightness))
            return;

        SetColorPicker(_zone1ColorPicker, zone1);
        SetColorPicker(_zone2ColorPicker, zone2);
        SetColorPicker(_zone3ColorPicker, zone3);
        SetColorPicker(_zone4ColorPicker, zone4);

        SetKeyboardBrightness(brightness);
        UpdateZonePreviews();
    }

    private void ApplyFourZoneSettingsToUI()
    {
        if (!TryParseFourZoneMode(
                _settings.FourZoneMode,
                out var mode,
                out var speed,
                out var brightness,
                out var direction,
                out var red,
                out var green,
                out var blue))
            return;

        if (_lightingModeComboBox != null)
            _lightingModeComboBox.SelectedIndex = mode;

        SetLightingSpeed(speed);
        SetKeyboardBrightness(brightness);

        SetDirectionRadioButtons(direction);

        // Static colors live in the zone registers, not the global effect RGB.
        if (mode == 0)
        {
            if (TryParsePerZoneMode(_settings.PerZoneMode,
                    out var zone1, out var zone2, out var zone3, out var zone4, out _) &&
                zone1 == zone2 && zone1 == zone3 && zone1 == zone4)
                SetColorPicker(_lightEffectColorPicker, zone1);
        }
        // Neon often reports 0,0,0; preserve the user's selected color.
        else if (mode != 2 || red != 0 || green != 0 || blue != 0)
            SetColorPicker(_lightEffectColorPicker, $"{red:X2}{green:X2}{blue:X2}");
    }


    private void ApplyBackLogoSettingsToUI()
    {
        if (string.IsNullOrWhiteSpace(_settings.BackLogoColor))
            return;

        if (!TryParseBackLogoColor(
                _settings.BackLogoColor,
                out var color,
                out var brightness,
                out var enabled))
            return;

        SetColorPicker(_backLogoColorPicker, color);
        // The driver normalizes a disabled state to RRGGBB,0,0. Preserve the
        // useful slider value so enabling the logo does not immediately send 0%.
        if (enabled || brightness > 0)
            SetBackLogoBrightness(brightness);
        SetCheckBox(_backLogoEnabledCheckBox, enabled);
        UpdateBackLogoEffectiveText();
    }

    private async Task ShowMessageBox(string title, string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message);
        await box.ShowWindowDialogAsync(this);
    }

    public void DeveloperMode_OnClick(object? sender, RoutedEventArgs e)
    {
        EnableDevMode(true);
    }

    public void EnableDevMode(bool toEnable)
    {
        AppState.DevMode = toEnable;
        if (_powerToggleSwitch != null)
            _powerToggleSwitch.IsHitTestVisible = toEnable;
        ApplySettingsToUI();
    }

    private void RetryConnectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        InitializeAsync();
    }

    private void UpdatesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("xdg-open", "https://github.com/0PeterAdel/PredatorSense")
            { UseShellExecute = true });
    }

    private void StarProject_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("xdg-open", "https://github.com/0PeterAdel/PredatorSense")
            { UseShellExecute = true });
    }

    private void IssuePageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("xdg-open", "https://github.com/0PeterAdel/PredatorSense/issues")
            { UseShellExecute = true });
    }

    private void InternalsMangerWindow_OnClick(object? sender, RoutedEventArgs e)
    {
        var internalsManagerWindow = new InternalsManager(this);
        internalsManagerWindow.ShowDialog(this);
    }


    private async void ProfileButton_Checked(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings || !_isConnected || sender is not RadioButton button || button.IsChecked != true) return;

        var profile = button.Name switch
        {
            "LowPowerProfileButton" => "low-power",
            "QuietProfileButton" => "quiet",
            "BalancedProfileButton" => "balanced",
            "PerformanceProfileButton" => "balanced-performance",
            "TurboProfileButton" => "performance",
            _ => "balanced"
        };

        var success = await _client.SetThermalProfileAsync(profile);
        if (!success)
        {
            await ShowMessageBox("Performance Mode", $"Could not activate '{profile}' mode.\n\nHardware requirement:\n• Acer Predator requires the AC power adapter connected and battery charged above 30% for Performance and Turbo modes.");
            await LoadSettingsAsync();
            return;
        }

        if (_settings?.ThermalProfile != null)
            _settings.ThermalProfile.Current = profile;

        if (profile == "quiet")
        {
            await _client.SetFanSpeedAsync(0, 0);
            _isManualFanControl = false;
            if (!AppState.DevMode)
            {
                if (_manualFanSpeedRadioButton != null) _manualFanSpeedRadioButton.IsChecked = false;
                if (_autoFanSpeedRadioButton != null) _autoFanSpeedRadioButton.IsChecked = true;
                if (_manualFanSpeedRadioButton != null) _manualFanSpeedRadioButton.IsEnabled = false;
                if (_maxFanSpeedRadioButton != null) _maxFanSpeedRadioButton.IsEnabled = false;
            }

            if (_thermalProfileInfoText != null)
                _thermalProfileInfoText.Text = "Minimizes noise, prioritizes low power and cooling.";
        }
        else
        {
            if (_maxFanSpeedRadioButton != null) _maxFanSpeedRadioButton.IsEnabled = true;
            if (_manualFanSpeedRadioButton != null) _manualFanSpeedRadioButton.IsEnabled = true;
            if (_thermalProfileInfoText != null)
                _thermalProfileInfoText.Text = profile switch
                {
                    "low-power" => "Prioritizes energy efficiency, reduces performance to extend battery life.",
                    "balanced" => "Optimal mix of performance and noise for everyday tasks.",
                    "balanced-performance" => "Maximizes speed for demanding workloads, higher fan noise",
                    "performance" => "Unleashes peak power for extreme tasks, loudest fans.",
                    _ => _thermalProfileInfoText.Text
                };
        }

        await Task.Delay(300);
        await LoadSettingsAsync();
    }

    private void ManualFanControlRadioBox_Click(object sender, RoutedEventArgs e)
    {
        _isManualFanControl = true;
        if (_manualFanSpeedRadioButton != null) _manualFanSpeedRadioButton.IsChecked = true;
        if (_autoFanSpeedRadioButton != null) _autoFanSpeedRadioButton.IsChecked = false;
    }

    private void CpuFanSlider_ValueChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
        {
            _cpuFanSpeed = Convert.ToInt32(e.NewValue);
            if (_cpuFanTextBlock != null)
                _cpuFanTextBlock.Text = _cpuFanSpeed == 0 ? "Auto" : $"{_cpuFanSpeed}%";
        }
    }

    private void GpuFanSlider_ValueChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
        {
            _gpuFanSpeed = Convert.ToInt32(e.NewValue);
            if (_gpuFanTextBlock != null)
                _gpuFanTextBlock.Text = _gpuFanSpeed == 0 ? "Auto" : $"{_gpuFanSpeed}%";
        }
    }

    private async void SetManualSpeedButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_isConnected || _isSettingFanSpeed)
            return;

        _isSettingFanSpeed = true;

        if (_setManualSpeedButton != null)
            _setManualSpeedButton.IsEnabled = false;

        try
        {
            await _client.SetFanSpeedAsync(_cpuFanSpeed, _gpuFanSpeed);
        }
        catch (Exception ex)
        {
            await ShowMessageBox(
                "Fan Speed Error",
                $"Failed to set fan speed: {ex.Message}"
            );
        }
        finally
        {
            _isSettingFanSpeed = false;

            if (_setManualSpeedButton != null)
                _setManualSpeedButton.IsEnabled = true;
        }
    }

    private async void AutoFanSpeedRadioButtonClick(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            await _client.SetFanSpeedAsync(0, 0);
            _isManualFanControl = false;
            if (_manualFanSpeedRadioButton != null) _manualFanSpeedRadioButton.IsChecked = false;
            await LoadSettingsAsync();
        }
    }

    private async void MaxFanSpeedRadioButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_isConnected)
            return;

        _cpuFanSpeed = 100;
        _gpuFanSpeed = 100;
        _isManualFanControl = false;

        if (_cpuFanSlider != null)
            _cpuFanSlider.Value = 100;

        if (_gpuFanSlider != null)
            _gpuFanSlider.Value = 100;

        if (_cpuFanTextBlock != null)
            _cpuFanTextBlock.Text = "100%";

        if (_gpuFanTextBlock != null)
            _gpuFanTextBlock.Text = "100%";

        await _client.SetFanSpeedAsync(100, 100);
    }

    private async void StartCalibrationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            await _client.SetBatteryCalibrationAsync(true);
            if (_startCalibrationButton != null) _startCalibrationButton.IsEnabled = false;
            if (_stopCalibrationButton != null) _stopCalibrationButton.IsEnabled = true;
            if (_calibrationStatusTextBlock != null) _calibrationStatusTextBlock.Text = "Status: Calibrating";
        }
    }

    private async void StopCalibrationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            await _client.SetBatteryCalibrationAsync(false);
            if (_startCalibrationButton != null) _startCalibrationButton.IsEnabled = true;
            if (_stopCalibrationButton != null) _stopCalibrationButton.IsEnabled = false;
            if (_calibrationStatusTextBlock != null) _calibrationStatusTextBlock.Text = "Status: Not calibrating";
        }
    }

    private async void BatteryLimitCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected && sender is CheckBox checkBox)
            await _client.SetBatteryLimiterAsync(checkBox.IsChecked ?? false);
    }

    private void ApplyKeyboardBrightnessDebounced()
    {
        if (_keyboardBrightnessDebounceTimer == null)
        {
            _keyboardBrightnessDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(70)
            };
            _keyboardBrightnessDebounceTimer.Tick += async (s, e) =>
            {
                _keyboardBrightnessDebounceTimer?.Stop();
                await ApplyStaticZoneColorsAsync();
            };
        }
        _keyboardBrightnessDebounceTimer.Stop();
        _keyboardBrightnessDebounceTimer.Start();
    }

    private void ApplyBackLogoBrightnessDebounced()
    {
        if (_backLogoBrightnessDebounceTimer == null)
        {
            _backLogoBrightnessDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(70)
            };
            _backLogoBrightnessDebounceTimer.Tick += async (s, e) =>
            {
                _backLogoBrightnessDebounceTimer?.Stop();
                await ApplyBackLogoStaticAsync();
            };
        }
        _backLogoBrightnessDebounceTimer.Stop();
        _backLogoBrightnessDebounceTimer.Start();
    }

    private void KeyboardBrightnessSlider_ValueChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
        {
            int val = Convert.ToInt32(e.NewValue);
            SetKeyboardBrightness(val, false);

            if (_dynamicEffectsEngine != null && _dynamicEffectsEngine.IsRunning)
            {
                _dynamicEffectsEngine.UpdateBrightness(val);
            }
            else
            {
                ApplyKeyboardBrightnessDebounced();
            }
        }
    }

    private async void ApplyKeyboardColorsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_dynamicEffectsEngine != null && _dynamicEffectsEngine.IsRunning)
        {
            await _dynamicEffectsEngine.StopAsync();
        }

        if (_activeModeStatusText != null)
            _activeModeStatusText.Text = "Static 4-Zone Mode";

        if (_activeModeDot != null)
            _activeModeDot.Background = Brush.Parse("#00ff88");

        await ApplyStaticZoneColorsAsync();
    }

    // ==========================================
    // JMA Studio Merged Features & Event Handlers
    // ==========================================

    private bool _isSyncingAllZones;

    private void ZoneColorPicker_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ColorPicker.ColorProperty && !_isSyncingAllZones)
        {
            if (_selectZoneComboBox?.SelectedIndex == 0 && sender is ColorPicker cp)
            {
                _isSyncingAllZones = true;
                try
                {
                    var newCol = cp.Color;
                    if (_zone1ColorPicker != null) _zone1ColorPicker.Color = newCol;
                    if (_zone2ColorPicker != null) _zone2ColorPicker.Color = newCol;
                    if (_zone3ColorPicker != null) _zone3ColorPicker.Color = newCol;
                    if (_zone4ColorPicker != null) _zone4ColorPicker.Color = newCol;
                }
                finally
                {
                    _isSyncingAllZones = false;
                }
            }

            UpdateZonePreviews();
            if (_instantApplyCheckBox?.IsChecked == true)
            {
                ApplyCurrentZoneColorsDebounced();
            }
        }
    }

    private void InitializeKeyboardVisualizer()
    {
        _allKeyCaps.Clear();
        _zone1Keys.Clear();
        _zone2Keys.Clear();
        _zone3Keys.Clear();
        _zone4Keys.Clear();

        if (_keyboardChassis != null)
        {
            ScanLogicalKeys(_keyboardChassis);
        }
        if (_keyboardChassisGlobal != null)
        {
            ScanLogicalKeys(_keyboardChassisGlobal);
        }

        if (_keyNum1 != null)
        {
            _currentlySelectedKey = _keyNum1;
        }

        HighlightActiveZoneCard(4);
        UpdateZonePreviews();
    }

    private void ScanLogicalKeys(ILogical parent)
    {
        foreach (var child in parent.LogicalChildren)
        {
            if (child is Border b && b.Classes.Contains("PSKey"))
            {
                _allKeyCaps.Add(b);
                var tag = b.Tag?.ToString() ?? "z1";
                switch (tag)
                {
                    case "z1": _zone1Keys.Add(b); break;
                    case "z2": _zone2Keys.Add(b); break;
                    case "z3": _zone3Keys.Add(b); break;
                    case "z4": _zone4Keys.Add(b); break;
                }
            }
            if (child is ILogical logicalChild)
            {
                ScanLogicalKeys(logicalChild);
            }
        }
    }

    private void KeyCap_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border keyBorder) return;

        // Tactile press animation
        keyBorder.RenderTransform = new ScaleTransform(0.91, 0.91);
        var bounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(75) };
        bounceTimer.Tick += (s, ev) =>
        {
            keyBorder.RenderTransform = new ScaleTransform(1.0, 1.0);
            bounceTimer.Stop();
        };
        bounceTimer.Start();

        // Select keycap (white border + checkmark badge)
        SelectVisualKey(keyBorder);

        // Map tag to zone
        var tag = keyBorder.Tag?.ToString() ?? "z1";
        int zoneIndex = tag switch
        {
            "z1" => 1,
            "z2" => 2,
            "z3" => 3,
            "z4" => 4,
            _ => 1
        };

        if (_selectZoneComboBox != null && _selectZoneComboBox.SelectedIndex != zoneIndex)
        {
            _suppressZoneComboUpdate = true;
            _selectZoneComboBox.SelectedIndex = zoneIndex;
            _suppressZoneComboUpdate = false;
        }

        HighlightActiveZoneCard(zoneIndex);
    }

    private void SelectVisualKey(Border keyBorder)
    {
        if (_currentlySelectedKey != null && _currentlySelectedKey != keyBorder)
        {
            _currentlySelectedKey.Classes.Remove("SelectedKey");
            if (_currentlySelectedKey.Child is Panel prevPanel)
            {
                var prevBadge = prevPanel.Children.OfType<Border>().FirstOrDefault(b => b.Classes.Contains("PSKeyBadge"));
                if (prevBadge != null) prevBadge.IsVisible = false;
            }
        }

        _currentlySelectedKey = keyBorder;
        if (!keyBorder.Classes.Contains("SelectedKey"))
        {
            keyBorder.Classes.Add("SelectedKey");
        }

        if (keyBorder.Child is Panel panel)
        {
            var badge = panel.Children.OfType<Border>().FirstOrDefault(b => b.Classes.Contains("PSKeyBadge"));
            if (badge != null) badge.IsVisible = true;
        }
    }

    private void SelectZoneComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressZoneComboUpdate) return;
        if (_selectZoneComboBox == null) return;

        int selectedIdx = _selectZoneComboBox.SelectedIndex;
        HighlightActiveZoneCard(selectedIdx);

        Border? targetKey = selectedIdx switch
        {
            1 => _zone1Keys.FirstOrDefault(k => k.Classes.Contains("KeyWasd")) ?? _zone1Keys.FirstOrDefault(),
            2 => _zone2Keys.FirstOrDefault(),
            3 => _zone3Keys.FirstOrDefault(k => k.Tag?.ToString() == "z3"),
            4 => _keyNum1 ?? _zone4Keys.FirstOrDefault(),
            _ => null
        };

        if (targetKey != null)
        {
            SelectVisualKey(targetKey);
        }
    }

    private void HighlightActiveZoneCard(int zoneIndex)
    {
        var activeBrush = new SolidColorBrush(Color.Parse("#00f0ff"));
        var inactiveBrush = new SolidColorBrush(Color.Parse("#1e293b"));

        if (_zone1Card != null) _zone1Card.BorderBrush = (zoneIndex == 1 || zoneIndex == 0) ? activeBrush : inactiveBrush;
        if (_zone2Card != null) _zone2Card.BorderBrush = (zoneIndex == 2 || zoneIndex == 0) ? activeBrush : inactiveBrush;
        if (_zone3Card != null) _zone3Card.BorderBrush = (zoneIndex == 3 || zoneIndex == 0) ? activeBrush : inactiveBrush;
        if (_zone4Card != null) _zone4Card.BorderBrush = (zoneIndex == 4 || zoneIndex == 0) ? activeBrush : inactiveBrush;

        if (_activeZoneHintText != null)
        {
            _activeZoneHintText.Text = zoneIndex switch
            {
                0 => "Active: All Zones (Unified)",
                1 => "Active: Zone 1 (WASD)",
                2 => "Active: Zone 2 (Center)",
                3 => "Active: Zone 3 (Nav & Arrows)",
                4 => "Active: Zone 4 (Numpad)",
                _ => "Active: 4-Zone Matrix"
            };
        }
    }

    private async void ZoneInfoBtn_Click(object? sender, RoutedEventArgs e)
    {
        await ShowMessageBox("Predator RGB Zones", "• Click any key on the matrix to select its zone and preview lighting.\n• Zone 1: WASD & Left Gaming cluster\n• Zone 2: Center Typing matrix\n• Zone 3: Navigation, Right Shift & Arrow cluster\n• Zone 4: Numpad, Media & Predator hotkeys");
    }

    private void UpdateZonePreviews()
    {
        var c1 = _zone1ColorPicker?.Color ?? Color.Parse(DefaultZone1Color);
        var c2 = _zone2ColorPicker?.Color ?? Color.Parse(DefaultZone2Color);
        var c3 = _zone3ColorPicker?.Color ?? Color.Parse(DefaultZone3Color);
        var c4 = _zone4ColorPicker?.Color ?? Color.Parse(DefaultZone4Color);

        if (_zone1Indicator != null) _zone1Indicator.Background = new SolidColorBrush(c1);
        if (_zone2Indicator != null) _zone2Indicator.Background = new SolidColorBrush(c2);
        if (_zone3Indicator != null) _zone3Indicator.Background = new SolidColorBrush(c3);
        if (_zone4Indicator != null) _zone4Indicator.Background = new SolidColorBrush(c4);

        if (_allKeyCaps.Count == 0)
        {
            InitializeKeyboardVisualizer();
        }

        ApplyColorToZoneKeys(_zone1Keys, c1);
        ApplyColorToZoneKeys(_zone2Keys, c2);
        ApplyColorToZoneKeys(_zone3Keys, c3);
        ApplyColorToZoneKeys(_zone4Keys, c4);

        var spaceBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(c2, 0.0),
                new GradientStop(c3, 1.0)
            }
        };
        if (_spaceKey != null) _spaceKey.Background = spaceBrush;
        if (_spaceKeyGlobal != null) _spaceKeyGlobal.Background = spaceBrush;

        UpdateBoxPreview(_zone1PreviewBox, _zone1PreviewText, c1);
        UpdateBoxPreview(_zone2PreviewBox, _zone2PreviewText, c2);
        UpdateBoxPreview(_zone3PreviewBox, _zone3PreviewText, c3);
        UpdateBoxPreview(_zone4PreviewBox, _zone4PreviewText, c4);
    }

    private static void ApplyColorToZoneKeys(List<Border> keys, Color color)
    {
        if (keys.Count == 0) return;

        var bgBrush = new SolidColorBrush(color);

        double lum = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
        var fgBrush = lum > 135
            ? new SolidColorBrush(Color.Parse("#08151f"))
            : new SolidColorBrush(Color.Parse("#f8fafc"));

        foreach (var key in keys)
        {
            key.Background = bgBrush;

            if (key.Child is Panel panel)
            {
                var text = panel.Children.OfType<TextBlock>().FirstOrDefault();
                if (text != null) text.Foreground = fgBrush;

                var icon = panel.Children.OfType<MaterialIcon>().FirstOrDefault();
                if (icon != null) icon.Foreground = fgBrush;
            }
        }
    }

    private static void UpdateBoxPreview(Border? box, TextBlock? text, Color color)
    {
        if (box != null)
        {
            box.Background = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B));
            box.BorderBrush = new SolidColorBrush(Color.FromArgb(180, color.R, color.G, color.B));
        }

        if (text != null)
        {
            text.Foreground = new SolidColorBrush(color);
        }
    }

    private void AreaThemeCard_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || rb.Tag == null) return;
        var tag = rb.Tag.ToString();
        if (string.IsNullOrEmpty(tag)) return;

        if (tag == "custom") return;

        if (tag == "gradient_flow")
        {
            GenerateGradientButton_Click(sender, e);
            return;
        }

        if (tag == "mirror_sym")
        {
            var c1 = _zone1ColorPicker?.Color ?? Color.Parse(DefaultZone1Color);
            var c2 = _zone2ColorPicker?.Color ?? Color.Parse(DefaultZone2Color);
            SetColorPicker(_zone3ColorPicker, ToRgbHex(c2));
            SetColorPicker(_zone4ColorPicker, ToRgbHex(c1));
            UpdateZonePreviews();
            if (_instantApplyCheckBox?.IsChecked == true)
                ApplyCurrentZoneColorsDebounced();
            return;
        }

        if (_curatedPresets.TryGetValue(tag, out var colors))
        {
            SetColorPicker(_zone1ColorPicker, colors.z1);
            SetColorPicker(_zone2ColorPicker, colors.z2);
            SetColorPicker(_zone3ColorPicker, colors.z3);
            SetColorPicker(_zone4ColorPicker, colors.z4);
            UpdateZonePreviews();

            if (_instantApplyCheckBox?.IsChecked == true)
            {
                ApplyCurrentZoneColorsDebounced();
            }
        }
    }

    private readonly Dictionary<string, (string z1, string z2, string z3, string z4)> _curatedPresets = new()
    {
        ["cyberpunk"] = ("00F0FF", "FF003C", "FFE600", "7000FF"),
        ["matrix"] = ("00FF41", "008F11", "00FF41", "003B00"),
        ["vaporwave"] = ("FF71CE", "01CDFE", "05FFA1", "B967FF"),
        ["fire_ice"] = ("FF3300", "FF9900", "00CCFF", "0033FF"),
        ["sunset"] = ("FF4500", "FF8C00", "FFD700", "8A2BE2"),
        ["predator_teal"] = ("00AEC7", "0088AA", "00AAFF", "0055FF"),
        ["toxic_acid"] = ("76FF03", "00E676", "1DE9B6", "00B0FF"),
        ["blood_moon"] = ("FF0000", "AA0000", "550000", "220000"),
        ["arctic_frost"] = ("FFFFFF", "E0F7FA", "B2EBF2", "80DEEA"),
        ["rainbow"] = ("FF0000", "FFFF00", "00FF00", "0000FF"),
        ["deep_purple"] = ("9D00FF", "E100FF", "00E1FF", "0026FF"),
        ["golden_dragon"] = ("FFD700", "FF8C00", "FF4500", "FF0000")
    };

    private void ApplySelectedPreset()
    {
        if (_jmaPresetComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag && _curatedPresets.TryGetValue(tag, out var colors))
        {
            SetColorPicker(_zone1ColorPicker, colors.z1);
            SetColorPicker(_zone2ColorPicker, colors.z2);
            SetColorPicker(_zone3ColorPicker, colors.z3);
            SetColorPicker(_zone4ColorPicker, colors.z4);
            UpdateZonePreviews();

            if (_instantApplyCheckBox?.IsChecked == true)
            {
                ApplyKeyboardColorsButton_Click(this, new RoutedEventArgs());
            }
        }
    }

    private void JmaPresetComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplySelectedPreset();
    }

    private void ApplyJmaPresetButton_Click(object? sender, RoutedEventArgs e)
    {
        ApplySelectedPreset();
    }

    private void GenerateGradientButton_Click(object? sender, RoutedEventArgs e)
    {
        var c1 = _zone1ColorPicker?.Color ?? Color.Parse(DefaultZone1Color);
        var c4 = _zone4ColorPicker?.Color ?? Color.Parse(DefaultZone4Color);

        byte r2 = (byte)Math.Clamp((int)Math.Round(c1.R + (c4.R - c1.R) * 0.333), 0, 255);
        byte g2 = (byte)Math.Clamp((int)Math.Round(c1.G + (c4.G - c1.G) * 0.333), 0, 255);
        byte b2 = (byte)Math.Clamp((int)Math.Round(c1.B + (c4.B - c1.B) * 0.333), 0, 255);

        byte r3 = (byte)Math.Clamp((int)Math.Round(c1.R + (c4.R - c1.R) * 0.667), 0, 255);
        byte g3 = (byte)Math.Clamp((int)Math.Round(c1.G + (c4.G - c1.G) * 0.667), 0, 255);
        byte b3 = (byte)Math.Clamp((int)Math.Round(c1.B + (c4.B - c1.B) * 0.667), 0, 255);

        SetColorPicker(_zone2ColorPicker, $"{r2:X2}{g2:X2}{b2:X2}");
        SetColorPicker(_zone3ColorPicker, $"{r3:X2}{g3:X2}{b3:X2}");
        UpdateZonePreviews();

        if (_instantApplyCheckBox?.IsChecked == true)
        {
            ApplyKeyboardColorsButton_Click(this, new RoutedEventArgs());
        }
    }

    private void SyncAllToZone1Button_Click(object? sender, RoutedEventArgs e)
    {
        var c = _zone1ColorPicker?.Color ?? Color.Parse(DefaultZone1Color);
        string hex = ToRgbHex(c);
        SetColorPicker(_zone2ColorPicker, hex);
        SetColorPicker(_zone3ColorPicker, hex);
        SetColorPicker(_zone4ColorPicker, hex);
        UpdateZonePreviews();

        if (_instantApplyCheckBox?.IsChecked == true)
        {
            ApplyKeyboardColorsButton_Click(this, new RoutedEventArgs());
        }
    }

    private async void SyncBackLogoButton_Click(object? sender, RoutedEventArgs e)
    {
        var c = _zone1ColorPicker?.Color ?? Color.Parse(DefaultZone1Color);
        string hex = ToRgbHex(c);
        SetColorPicker(_backLogoColorPicker, hex);
        if (_backLogoEnabledCheckBox != null)
            _backLogoEnabledCheckBox.IsChecked = true;
        await ApplyBackLogoStaticAsync();
    }

    private void ApplyCurrentZoneColorsDebounced()
    {
        if (_dynamicEffectsEngine != null && _dynamicEffectsEngine.IsRunning)
        {
            _ = _dynamicEffectsEngine.StopAsync();
            if (_activeModeStatusText != null)
                _activeModeStatusText.Text = "Static 4-Zone Mode";
            if (_activeModeDot != null)
                _activeModeDot.Background = Brush.Parse("#00ff88");
        }

        if (_zoneColorDebounceTimer == null)
        {
            _zoneColorDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(75)
            };
            _zoneColorDebounceTimer.Tick += async (s, e) =>
            {
                _zoneColorDebounceTimer?.Stop();
                await ApplyStaticZoneColorsAsync();
            };
        }
        _zoneColorDebounceTimer.Stop();
        _zoneColorDebounceTimer.Start();
    }

    private void DynamicEffectsSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateDynamicEffectSelectionUI();
    }

    private void UpdateDynamicEffectSelectionUI()
    {
        var selectedItem = _dynamicEffectsSelector?.SelectedItem as ComboBoxItem;
        var tag = selectedItem?.Tag?.ToString();
        if (string.IsNullOrEmpty(tag)) return;

        var effect = DynamicEffectsEngine.AvailableEffects.Find(x => x.Id == tag);
        if (effect == null) return;

        if (_dynamicEffectTitleText != null)
            _dynamicEffectTitleText.Text = $"{effect.Name} • {(effect.IsHardware ? "Hardware Mode (0% CPU)" : "Smart Dynamic Animation")}";

        if (_dynamicEffectDescText != null)
            _dynamicEffectDescText.Text = effect.Description;

        if (_dynamicEffectIcon != null)
            _dynamicEffectIcon.Kind = effect.Icon;

        if (_dynamicSpeedSlider != null)
            _dynamicSpeedSlider.IsEnabled = effect.SupportsSpeed;

        if (_dynamicColorLabel != null)
            _dynamicColorLabel.IsVisible = effect.SupportsCustomColor;
        if (_dynamicEffectColorPicker != null)
        {
            _dynamicEffectColorPicker.IsVisible = effect.SupportsCustomColor;
            if (effect.SupportsCustomColor && Color.TryParse($"#{effect.DefaultColorHex}", out var defColor))
                _dynamicEffectColorPicker.Color = defColor;
        }

        if (_dynamicDirectionLabel != null)
            _dynamicDirectionLabel.IsVisible = effect.SupportsDirection;
        if (_dynamicDirectionPanel != null)
            _dynamicDirectionPanel.IsVisible = effect.SupportsDirection;
    }

    private void DynamicSpeedSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty && _dynamicSpeedSlider != null)
        {
            int val = (int)_dynamicSpeedSlider.Value;
            if (_dynamicSpeedText != null)
                _dynamicSpeedText.Text = val.ToString();

            UpdateDynamicParameters();

            if (_dynamicEffectsEngine != null && _dynamicEffectsEngine.IsRunning)
            {
                _dynamicEffectsEngine.UpdateSpeed(val);
            }
        }
    }

    private void DynamicEffectColorPicker_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ColorPicker.ColorProperty)
        {
            if (_isGlobalStaticActive)
            {
                var color = _dynamicEffectColorPicker?.Color ?? Color.Parse("#00f0ff");
                if (_zone1ColorPicker != null) _zone1ColorPicker.Color = color;
                if (_zone2ColorPicker != null) _zone2ColorPicker.Color = color;
                if (_zone3ColorPicker != null) _zone3ColorPicker.Color = color;
                if (_zone4ColorPicker != null) _zone4ColorPicker.Color = color;

                ApplyColorToZoneKeys(_zone1Keys, color);
                ApplyColorToZoneKeys(_zone2Keys, color);
                ApplyColorToZoneKeys(_zone3Keys, color);
                ApplyColorToZoneKeys(_zone4Keys, color);

                UpdateZonePreviews();
                ApplyCurrentZoneColorsDebounced();
            }
            else
            {
                UpdateDynamicParameters();
            }
        }
    }

    private void UpdateDynamicParameters()
    {
        if (_dynamicEffectsEngine == null) return;
        int speed = (int)(_dynamicSpeedSlider?.Value ?? 5);
        int dir = _dirRightToLeftButton?.IsChecked == true ? DirectionRightToLeft : DirectionLeftToRight;
        Color col = _dynamicEffectColorPicker?.Color ?? Color.Parse("#00D4FF");
        _dynamicEffectsEngine.SetParameters(speed, _keyboardBrightness, dir, col);
    }

    private async void PlayDynamicEffectButton_Click(object? sender, RoutedEventArgs e)
    {
        var selectedItem = _dynamicEffectsSelector?.SelectedItem as ComboBoxItem;
        var tag = selectedItem?.Tag?.ToString();
        if (string.IsNullOrEmpty(tag)) return;

        var effect = DynamicEffectsEngine.AvailableEffects.Find(x => x.Id == tag);
        if (effect == null || _dynamicEffectsEngine == null) return;

        UpdateDynamicParameters();

        if (_activeModeStatusText != null)
            _activeModeStatusText.Text = $"Running: {effect.Name} ({(effect.IsHardware ? "Hardware Mode" : "Smart Dynamic")})";

        if (_activeModeDot != null)
            _activeModeDot.Background = Brush.Parse("#00d4ff");

        var success = await _dynamicEffectsEngine.StartEffectAsync(effect);
        if (!success && effect.IsHardware)
        {
            if (_activeModeStatusText != null)
                _activeModeStatusText.Text = "Hardware effect write error";
        }
    }

    private async void StopDynamicEffectButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_dynamicEffectsEngine != null)
        {
            await _dynamicEffectsEngine.StopAsync();
        }

        if (_activeModeStatusText != null)
            _activeModeStatusText.Text = "Static 4-Zone Mode";

        if (_activeModeDot != null)
            _activeModeDot.Background = Brush.Parse("#00ff88");

        UpdateZonePreviews();
        await ApplyStaticZoneColorsAsync();
    }

    private void OnDynamicEffectsPreviewUpdate(string z1Hex, string z2Hex, string z3Hex, string z4Hex)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Color.TryParse(z1Hex.StartsWith("#") ? z1Hex : $"#{z1Hex}", out var c1))
            {
                ApplyColorToZoneKeys(_zone1Keys, c1);
                if (_zone1Indicator != null) _zone1Indicator.Background = new SolidColorBrush(c1);
                UpdateBoxPreview(_zone1PreviewBox, _zone1PreviewText, c1);
            }
            if (Color.TryParse(z2Hex.StartsWith("#") ? z2Hex : $"#{z2Hex}", out var c2))
            {
                ApplyColorToZoneKeys(_zone2Keys, c2);
                if (_zone2Indicator != null) _zone2Indicator.Background = new SolidColorBrush(c2);
                UpdateBoxPreview(_zone2PreviewBox, _zone2PreviewText, c2);
            }
            if (Color.TryParse(z3Hex.StartsWith("#") ? z3Hex : $"#{z3Hex}", out var c3))
            {
                ApplyColorToZoneKeys(_zone3Keys, c3);
                if (_zone3Indicator != null) _zone3Indicator.Background = new SolidColorBrush(c3);
                UpdateBoxPreview(_zone3PreviewBox, _zone3PreviewText, c3);
            }
            if (Color.TryParse(z4Hex.StartsWith("#") ? z4Hex : $"#{z4Hex}", out var c4))
            {
                ApplyColorToZoneKeys(_zone4Keys, c4);
                if (_zone4Indicator != null) _zone4Indicator.Background = new SolidColorBrush(c4);
                UpdateBoxPreview(_zone4PreviewBox, _zone4PreviewText, c4);
            }

            if (Color.TryParse(z2Hex.StartsWith("#") ? z2Hex : $"#{z2Hex}", out var sc2) &&
                Color.TryParse(z3Hex.StartsWith("#") ? z3Hex : $"#{z3Hex}", out var sc3))
            {
                var spaceBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop(sc2, 0.0),
                        new GradientStop(sc3, 1.0)
                    }
                };
                if (_spaceKey != null) _spaceKey.Background = spaceBrush;
                if (_spaceKeyGlobal != null) _spaceKeyGlobal.Background = spaceBrush;
            }
        });
    }

    private void RgbSubTab_Click(object? sender, RoutedEventArgs e)
    {
        // Smoothly handle switching between RGB subtabs
    }

    private async void EffectCard_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || rb.Tag == null) return;
        var tag = rb.Tag.ToString();
        if (string.IsNullOrEmpty(tag)) return;

        if (tag == "static")
        {
            _isGlobalStaticActive = true;
            if (_dynamicEffectsEngine != null)
                await _dynamicEffectsEngine.StopAsync();

            if (_dynamicEffectTitleText != null)
                _dynamicEffectTitleText.Text = "Static Global Mode • Hardware Color";
            if (_dynamicEffectDescText != null)
                _dynamicEffectDescText.Text = "Applies a single uniform solid color across all 4 keyboard zones simultaneously.";
            if (_dynamicEffectIcon != null)
                _dynamicEffectIcon.Kind = MaterialIconKind.CubeOutline;

            if (_dynamicSpeedSlider != null) _dynamicSpeedSlider.IsEnabled = false;
            if (_dynamicColorLabel != null) _dynamicColorLabel.IsVisible = true;
            if (_dynamicEffectColorPicker != null) _dynamicEffectColorPicker.IsVisible = true;
            if (_dynamicDirectionLabel != null) _dynamicDirectionLabel.IsVisible = false;
            if (_dynamicDirectionPanel != null) _dynamicDirectionPanel.IsVisible = false;

            if (_activeModeStatusText != null)
                _activeModeStatusText.Text = "Static Global Mode";
            if (_activeModeDot != null)
                _activeModeDot.Background = Brush.Parse("#00ff9d");

            if (_applyGlobalColorButton != null)
                _applyGlobalColorButton.IsVisible = true;

            return;
        }

        _isGlobalStaticActive = false;
        if (_applyGlobalColorButton != null)
            _applyGlobalColorButton.IsVisible = false;

        // Synchronize with hidden _dynamicEffectsSelector
        if (_dynamicEffectsSelector != null)
        {
            for (int i = 0; i < _dynamicEffectsSelector.Items.Count; i++)
            {
                if (_dynamicEffectsSelector.Items[i] is ComboBoxItem item && item.Tag?.ToString() == tag)
                {
                    _dynamicEffectsSelector.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private async void ApplyGlobalColor_Click(object? sender, RoutedEventArgs e)
    {
        if (_dynamicEffectsEngine != null)
            await _dynamicEffectsEngine.StopAsync();

        var color = _dynamicEffectColorPicker?.Color ?? Color.Parse("#00f0ff");
        if (_zone1ColorPicker != null) _zone1ColorPicker.Color = color;
        if (_zone2ColorPicker != null) _zone2ColorPicker.Color = color;
        if (_zone3ColorPicker != null) _zone3ColorPicker.Color = color;
        if (_zone4ColorPicker != null) _zone4ColorPicker.Color = color;

        if (_activeModeStatusText != null)
            _activeModeStatusText.Text = "Static Global Color";
        if (_activeModeDot != null)
            _activeModeDot.Background = Brush.Parse("#00ff9d");

        UpdateZonePreviews();
        await ApplyStaticZoneColorsAsync();
    }

    private void QuickSwatch_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null && Color.TryParse(btn.Tag.ToString(), out var color))
        {
            if (_dynamicEffectColorPicker != null)
                _dynamicEffectColorPicker.Color = color;

            if (_isGlobalStaticActive)
            {
                ApplyGlobalColor_Click(sender, e);
            }
            else
            {
                UpdateDynamicParameters();
            }
        }
    }

    private async void QuickLogoSwatch_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null && Color.TryParse(btn.Tag.ToString(), out var color))
        {
            if (_backLogoColorPicker != null)
                _backLogoColorPicker.Color = color;

            if (_logoEmblemMask != null)
                _logoEmblemMask.Background = new SolidColorBrush(color);
            if (_logoEmblemLiveBadge != null)
            {
                _logoEmblemLiveBadge.Text = $"PREDATOR EMBLEM • #{ToRgbHex(color)}";
                _logoEmblemLiveBadge.Foreground = new SolidColorBrush(color);
            }

            if (_isLogoStaticActive)
            {
                if (_logoEffectsEngine != null)
                    await _logoEffectsEngine.StopAsync();
                await ApplyBackLogoStaticAsync();
            }
            else if (_logoEffectsEngine != null)
            {
                _logoEffectsEngine.UpdateColor(color);
            }
        }
    }

    private void BackLogoColorPicker_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ColorPicker.ColorProperty)
        {
            var col = _backLogoColorPicker?.Color ?? Color.Parse("#00f0ff");
            if (_logoEmblemMask != null)
                _logoEmblemMask.Background = new SolidColorBrush(col);
            if (_logoEmblemLiveBadge != null)
            {
                _logoEmblemLiveBadge.Text = $"PREDATOR EMBLEM • #{ToRgbHex(col)}";
                _logoEmblemLiveBadge.Foreground = new SolidColorBrush(col);
            }

            if (_isLogoStaticActive)
            {
                ApplyBackLogoButton_Click(this, new RoutedEventArgs());
            }
            else if (_logoEffectsEngine != null)
            {
                _logoEffectsEngine.UpdateColor(col);
            }
        }
    }

    private void LogoSpeedSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty && _logoSpeedSlider != null)
        {
            int val = (int)_logoSpeedSlider.Value;
            if (_logoSpeedText != null)
                _logoSpeedText.Text = val.ToString();

            if (_logoEffectsEngine != null && _logoEffectsEngine.IsRunning)
            {
                _logoEffectsEngine.UpdateSpeed(val);
            }
        }
    }

    private void OnLogoEffectsPreviewUpdate(string hex)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Color.TryParse(hex.StartsWith("#") ? hex : $"#{hex}", out var col))
            {
                if (_logoEmblemMask != null)
                    _logoEmblemMask.Background = new SolidColorBrush(col);
                if (_logoEmblemLiveBadge != null)
                {
                    _logoEmblemLiveBadge.Text = $"PREDATOR EMBLEM • #{hex.ToUpper()}";
                    _logoEmblemLiveBadge.Foreground = new SolidColorBrush(col);
                }
                if (_logoEffectIcon != null)
                    _logoEffectIcon.Foreground = new SolidColorBrush(col);
                if (_logoActiveModeDot != null)
                    _logoActiveModeDot.Background = new SolidColorBrush(col);

                UpdateBackLogoEffectiveText();
            }
        });
    }

    private async void LogoEffectCard_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || rb.Tag == null) return;
        var tag = rb.Tag.ToString();
        if (string.IsNullOrEmpty(tag)) return;

        var effect = LogoEffectsEngine.AvailableLogoEffects.Find(x => x.Id == tag);
        if (effect == null) return;

        _selectedLogoEffect = effect;

        if (effect.Id == "logo_static")
        {
            _isLogoStaticActive = true;
            if (_logoEffectsEngine != null)
                await _logoEffectsEngine.StopAsync();

            if (_logoEffectTitleText != null)
                _logoEffectTitleText.Text = $"{effect.Name} • Hardware Color";
            if (_logoEffectDescText != null)
                _logoEffectDescText.Text = effect.Description;
            if (_logoEffectIcon != null)
            {
                _logoEffectIcon.Kind = effect.Icon;
                _logoEffectIcon.Foreground = Brush.Parse("#00f0ff");
            }

            if (_logoSpeedSlider != null) _logoSpeedSlider.IsEnabled = false;
            if (_logoColorLabel != null) _logoColorLabel.IsVisible = true;
            if (_backLogoColorPicker != null) _backLogoColorPicker.IsVisible = true;

            if (_logoActiveModeStatusText != null)
                _logoActiveModeStatusText.Text = "Static Emblem";
            if (_logoActiveModeDot != null)
                _logoActiveModeDot.Background = Brush.Parse("#00ff9d");

            await ApplyBackLogoStaticAsync();
            return;
        }

        _isLogoStaticActive = false;
        if (_logoEffectTitleText != null)
            _logoEffectTitleText.Text = $"{effect.Name} • {effect.Subtitle}";
        if (_logoEffectDescText != null)
            _logoEffectDescText.Text = effect.Description;
        if (_logoEffectIcon != null)
            _logoEffectIcon.Kind = effect.Icon;

        if (_logoSpeedSlider != null)
            _logoSpeedSlider.IsEnabled = effect.SupportsSpeed;
        if (_logoColorLabel != null)
            _logoColorLabel.IsVisible = effect.SupportsColor;
        if (_backLogoColorPicker != null)
            _backLogoColorPicker.IsVisible = effect.SupportsColor;

        if (_logoActiveModeStatusText != null)
            _logoActiveModeStatusText.Text = $"Running: {effect.Name}";
        if (_logoActiveModeDot != null)
            _logoActiveModeDot.Background = Brush.Parse("#00f0ff");

        // Automatically start the selected dynamic logo effect immediately!
        if (_logoEffectsEngine != null)
        {
            int speed = (int)(_logoSpeedSlider?.Value ?? 5);
            int brightness = Math.Clamp(_backLogoBrightness, 0, 100);
            var col = _backLogoColorPicker?.Color ?? Color.Parse("#00f0ff");
            _logoEffectsEngine.SetParameters(speed, brightness, col);
            await _logoEffectsEngine.StartEffectAsync(effect);
        }
    }

    private async void PlayLogoEffectButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_logoEffectsEngine == null) return;

        var effect = (_selectedLogoEffect != null && _selectedLogoEffect.Id != "logo_static")
            ? _selectedLogoEffect
            : LogoEffectsEngine.AvailableLogoEffects[1];

        _selectedLogoEffect = effect;
        _isLogoStaticActive = false;

        int speed = (int)(_logoSpeedSlider?.Value ?? 5);
        int brightness = Math.Clamp(_backLogoBrightness, 0, 100);
        var col = _backLogoColorPicker?.Color ?? Color.Parse("#00f0ff");

        _logoEffectsEngine.SetParameters(speed, brightness, col);

        if (_logoActiveModeStatusText != null)
            _logoActiveModeStatusText.Text = $"Running: {effect.Name}";
        if (_logoActiveModeDot != null)
            _logoActiveModeDot.Background = Brush.Parse("#00f0ff");

        await _logoEffectsEngine.StartEffectAsync(effect);
    }

    private async void StopLogoEffectButton_Click(object? sender, RoutedEventArgs e)
    {
        _isLogoStaticActive = true;
        if (_logoEffectsEngine != null)
            await _logoEffectsEngine.StopAsync();

        if (_logoActiveModeStatusText != null)
            _logoActiveModeStatusText.Text = "Static Emblem";
        if (_logoActiveModeDot != null)
            _logoActiveModeDot.Background = Brush.Parse("#00ff9d");

        await ApplyBackLogoStaticAsync();
    }

    private async Task ApplyStaticZoneColorsAsync()
    {
        if ((_isConnected && _settings.HasFourZoneKb) || AppState.DevMode)
        {
            await _client.SetPerZoneModeAsync(
                ToRgbHex(_zone1ColorPicker?.Color ?? Color.Parse(DefaultZone1Color)),
                ToRgbHex(_zone2ColorPicker?.Color ?? Color.Parse(DefaultZone2Color)),
                ToRgbHex(_zone3ColorPicker?.Color ?? Color.Parse(DefaultZone3Color)),
                ToRgbHex(_zone4ColorPicker?.Color ?? Color.Parse(DefaultZone4Color)),
                _keyboardBrightness
            );

            SaveZonePresetFromUI();
        }
    }

    private void StartTelemetryTimer()
    {
        _telemetryTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _telemetryTimer.Tick += (s, e) => UpdateHeaderTelemetry();
        _telemetryTimer.Start();
        UpdateHeaderTelemetry();
    }

    private void UpdateHeaderTelemetry()
    {
        try
        {
            // CPU Package Temp
            double cpuTemp = DynamicEffectsEngine.ReadCpuPackageTemperature();
            if (_headerCpuTempText != null)
                _headerCpuTempText.Text = $"CPU: {cpuTemp:F0}°C";

            // Battery
            int battPct = DynamicEffectsEngine.ReadBatteryPercentage();
            bool isAc = File.Exists("/sys/class/power_supply/ACAD/online") &&
                        File.ReadAllText("/sys/class/power_supply/ACAD/online").Trim() == "1";

            if (_headerBatteryText != null)
                _headerBatteryText.Text = isAc ? $"AC {battPct}%" : $"BAT {battPct}%";

            if (_headerPowerIcon != null)
                _headerPowerIcon.Kind = isAc ? MaterialIconKind.PowerPlugOutline : MaterialIconKind.BatteryOutline;

            // Daemon status
            if (_headerDaemonBadge != null)
            {
                _headerDaemonBadge.Background = _isConnected ? Brush.Parse("#0e2a1e") : Brush.Parse("#2a0e14");
                _headerDaemonBadge.BorderBrush = _isConnected ? Brush.Parse("#105b38") : Brush.Parse("#5b101e");
            }

            // GPU Temp via quick asynchronous query
            Task.Run(() =>
            {
                try
                {
                    if (File.Exists("/usr/bin/nvidia-smi"))
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "nvidia-smi",
                            Arguments = "--query-gpu=temperature.gpu --format=csv,noheader",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var proc = Process.Start(psi);
                        if (proc != null)
                        {
                            string outStr = proc.StandardOutput.ReadToEnd().Trim();
                            proc.WaitForExit(500);
                            if (double.TryParse(outStr, out var gpuTemp))
                            {
                                Dispatcher.UIThread.Post(() =>
                                {
                                    if (_headerGpuTempText != null)
                                        _headerGpuTempText.Text = $"GPU: {gpuTemp:F0}°C";
                                });
                            }
                        }
                    }
                }
                catch { }
            });
        }
        catch { }
    }

    private void LightingSpeedSlider_ValueChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
        {
            int val = Convert.ToInt32(e.NewValue);
            SetLightingSpeed(val, false);
            if (_dynamicEffectsEngine != null && _dynamicEffectsEngine.IsRunning)
            {
                _dynamicEffectsEngine.UpdateSpeed(val);
            }
        }
    }

    private void BackLogoBrightnessSlider_ValueChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
        {
            int val = Convert.ToInt32(e.NewValue);
            SetBackLogoBrightness(val, false);
            if (_logoEffectsEngine != null && _logoEffectsEngine.IsRunning)
            {
                _logoEffectsEngine.UpdateBrightness(val);
            }
            else
            {
                ApplyBackLogoBrightnessDebounced();
            }
        }
    }

    private async void ApplyBackLogoButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!_isConnected || (!_client.IsFeatureAvailable("back_logo") && !AppState.DevMode))
            return;

        await ApplyBackLogoStaticAsync();
    }

    private async void BackLogoEnabledCheckBox_Click(object? sender, RoutedEventArgs e)
    {
        if (!_isConnected || (!_client.IsFeatureAvailable("back_logo") && !AppState.DevMode))
            return;

        await ApplyBackLogoStaticAsync();
    }

    private async Task ApplyBackLogoStaticAsync()
    {
        var color = _backLogoColorPicker?.Color ?? Color.Parse(DefaultBackLogoColor);
        var enabled = _backLogoEnabledCheckBox?.IsChecked ?? true;
        var brightness = Math.Clamp(_backLogoBrightness, 0, 100);

        // Avoid the confusing "enabled but dark" state when the user enables
        // a logo whose previous disabled readback was normalized to 0%.
        if (enabled && brightness == 0)
        {
            brightness = 100;
            SetBackLogoBrightness(brightness);
        }

        var hex = ToRgbHex(color);
        if (_logoEmblemMask != null)
        {
            _logoEmblemMask.Background = enabled ? new SolidColorBrush(color) : Brush.Parse("#1e293b");
            _logoEmblemMask.Opacity = enabled ? Math.Max(0.2, brightness / 100.0) : 0.2;
        }
        if (_logoEmblemLiveBadge != null)
        {
            _logoEmblemLiveBadge.Text = enabled ? $"PREDATOR EMBLEM • #{hex}" : "PREDATOR EMBLEM • DISABLED";
            _logoEmblemLiveBadge.Foreground = enabled ? new SolidColorBrush(color) : Brush.Parse("#64748b");
        }

        var success = await _client.SetBackLogoColorAsync(
            hex,
            brightness,
            enabled
        );

        if (!success && !AppState.DevMode)
        {
            await ShowMessageBox("Back Logo Error", "Failed to apply the back logo/lightbar setting.");
            return;
        }

        UpdateBackLogoEffectiveText();
    }

    private async void LightingEffectsApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if ((_isConnected && _settings.HasFourZoneKb) || AppState.DevMode)
        {
            var mode = _lightingModeComboBox?.SelectedIndex ?? 0;

            var direction = GetSelectedDirection();

            var color = _lightEffectColorPicker?.Color ?? Color.Parse(DefaultEffectColor);

            if (mode == 0)
            {
                var rgb = ToRgbHex(color);
                var success = await _client.SetPerZoneModeAsync(
                    rgb,
                    rgb,
                    rgb,
                    rgb,
                    _keyboardBrightness
                );

                if (!success)
                {
                    await ShowMessageBox("Keyboard Lighting Error", "Failed to apply static keyboard lighting.");
                    return;
                }

                SaveLightingEffectPresetFromUI(mode, direction, color);

                return;
            }

            await _client.SetFourZoneModeAsync(
                mode,
                _lightingSpeed,
                _keyboardBrightness,
                direction,
                color.R,
                color.G,
                color.B
            );

            SaveLightingEffectPresetFromUI(mode, direction, color);
        }
    }

    private async void BacklightTimeoutCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected && sender is CheckBox checkBox)
            await _client.SetBacklightTimeoutAsync(checkBox.IsChecked ?? false);
    }

    private async void LcdOverrideCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected && sender is CheckBox checkBox)
            await _client.SetLcdOverrideAsync(checkBox.IsChecked ?? false);
    }

    private async void BootSoundCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected && sender is CheckBox checkBox)
            await _client.SetBootAnimationSoundAsync(checkBox.IsChecked ?? false);
    }

    private async void UsbChargingComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isConnected && _usbChargingComboBox != null)
        {
            var level = _usbChargingComboBox.SelectedIndex switch
            {
                1 => 10,
                2 => 20,
                3 => 30,
                _ => 0
            };
            await _client.SetUsbChargingAsync(level);
        }
    }

    private void SetKeyboardBrightness(int brightness, bool updateSlider = true)
    {
        _keyboardBrightness = brightness;

        if (updateSlider && _keyBrightnessSlider != null)
            _keyBrightnessSlider.Value = brightness;

        SetText(_keyBrightnessText, $"{brightness}%");

        if (_keyboardChassis != null)
        {
            _keyboardChassis.Opacity = Math.Max(0.35, brightness / 100.0);
        }
    }

    private void SetLightingSpeed(int speed, bool updateSlider = true)
    {
        _lightingSpeed = speed;

        if (updateSlider && _lightingSpeedSlider != null)
            _lightingSpeedSlider.Value = speed;

        if (updateSlider && _dynamicSpeedSlider != null)
            _dynamicSpeedSlider.Value = speed;

        SetText(_lightSpeedTextBlock, speed.ToString());
        SetText(_dynamicSpeedText, speed.ToString());
    }

    private void SetBackLogoBrightness(int brightness, bool updateSlider = true)
    {
        _backLogoBrightness = Math.Clamp(brightness, 0, 100);

        if (updateSlider && _backLogoBrightnessSlider != null)
            _backLogoBrightnessSlider.Value = _backLogoBrightness;

        SetText(_backLogoBrightnessText, $"{_backLogoBrightness}%");

        if (_logoEmblemMask != null)
        {
            _logoEmblemMask.Opacity = Math.Max(0.15, _backLogoBrightness / 100.0);
        }

        UpdateBackLogoEffectiveText();
    }

    private void UpdateBackLogoEffectiveText()
    {
        var color = _backLogoColorPicker?.Color ?? Color.Parse(DefaultBackLogoColor);
        var enabled = _backLogoEnabledCheckBox?.IsChecked ?? true;

        SetText(_backLogoEffectiveText, enabled
            ? $"Effective output: RGB #{ToRgbHex(color)}, brightness {_backLogoBrightness}%"
            : "Effective output: logo/lightbar disabled");
    }

    private static string ToRgbHex(Color color)
    {
        return $"{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static void SetColorPicker(ColorPicker? picker, string rgbHex)
    {
        if (picker == null)
            return;

        var normalized = NormalizeRgbHex(rgbHex);
        if (normalized == null)
            return;

        picker.Color = Color.Parse($"#{normalized}");
    }

    private static string? NormalizeRgbHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var hex = value.Trim();

        if (hex.StartsWith('#'))
            hex = hex[1..];

        if (hex.Length != 6)
            return null;

        foreach (var c in hex)
            if (!Uri.IsHexDigit(c))
                return null;

        return hex.ToUpperInvariant();
    }

    private static bool TryParsePerZoneMode(
        string? value,
        out string zone1,
        out string zone2,
        out string zone3,
        out string zone4,
        out int brightness)
    {
        zone1 = "";
        zone2 = "";
        zone3 = "";
        zone4 = "";
        brightness = 100;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Trim().Split(',', StringSplitOptions.TrimEntries);

        if (parts.Length != 5)
            return false;

        zone1 = NormalizeRgbHex(parts[0]) ?? "";
        zone2 = NormalizeRgbHex(parts[1]) ?? "";
        zone3 = NormalizeRgbHex(parts[2]) ?? "";
        zone4 = NormalizeRgbHex(parts[3]) ?? "";

        if (zone1.Length != 6 ||
            zone2.Length != 6 ||
            zone3.Length != 6 ||
            zone4.Length != 6)
            return false;

        if (!int.TryParse(parts[4], out brightness))
            return false;

        return brightness is >= 0 and <= 100;
    }


    private static bool TryParseBackLogoColor(
        string? value,
        out string color,
        out int brightness,
        out bool enabled)
    {
        color = "FFFFFF";
        brightness = 100;
        enabled = true;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Trim().Split(',', StringSplitOptions.TrimEntries);

        if (parts.Length < 2 || parts.Length > 3)
            return false;

        color = NormalizeRgbHex(parts[0]) ?? "";
        if (color.Length != 6)
            return false;

        if (!int.TryParse(parts[1], out brightness) || brightness is < 0 or > 100)
            return false;

        if (parts.Length == 3)
        {
            if (!int.TryParse(parts[2], out var enableValue) || enableValue is < 0 or > 1)
                return false;

            enabled = enableValue == 1;
        }
        else
        {
            enabled = brightness > 0;
        }

        return true;
    }

    private static bool TryParseFourZoneMode(
        string? value,
        out int mode,
        out int speed,
        out int brightness,
        out int direction,
        out int red,
        out int green,
        out int blue)
    {
        mode = 0;
        speed = 5;
        brightness = 100;
        direction = DirectionRightToLeft;
        red = 0;
        green = 0;
        blue = 0;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Trim().Split(',', StringSplitOptions.TrimEntries);

        if (parts.Length != 7)
            return false;

        return int.TryParse(parts[0], out mode) &&
               mode is >= 0 and <= 7 &&
               int.TryParse(parts[1], out speed) &&
               speed is >= 0 and <= 9 &&
               int.TryParse(parts[2], out brightness) &&
               brightness is >= 0 and <= 100 &&
               int.TryParse(parts[3], out direction) &&
               direction is >= DirectionLeftToRight and <= DirectionRightToLeft &&
               int.TryParse(parts[4], out red) &&
               red is >= 0 and <= 255 &&
               int.TryParse(parts[5], out green) &&
               green is >= 0 and <= 255 &&
               int.TryParse(parts[6], out blue) &&
               blue is >= 0 and <= 255;
    }

    private static bool HasSavedZonePreset()
    {
        return File.Exists(KeyboardZonePresetPath);
    }

    private void ApplySavedZonePresetToUI()
    {
        if (!File.Exists(KeyboardZonePresetPath))
            return;

        try
        {
            var value = File.ReadAllText(KeyboardZonePresetPath).Trim();

            if (!TryParsePerZoneMode(
                    value,
                    out var zone1,
                    out var zone2,
                    out var zone3,
                    out var zone4,
                    out var brightness))
                return;

            SetColorPicker(_zone1ColorPicker, zone1);
            SetColorPicker(_zone2ColorPicker, zone2);
            SetColorPicker(_zone3ColorPicker, zone3);
            SetColorPicker(_zone4ColorPicker, zone4);

            _keyboardBrightness = brightness;

            if (_keyBrightnessSlider != null)
                _keyBrightnessSlider.Value = brightness;

            if (_keyBrightnessText != null)
                _keyBrightnessText.Text = $"{brightness}%";

            UpdateZonePreviews();
        }
        catch
        {
            // Ignore broken preset file and let daemon settings win.
        }
    }

    private void SaveZonePresetFromUI()
    {
        try
        {
            WritePresetFile(KeyboardZonePresetPath, CreateZonePresetValueFromUI());
        }
        catch
        {
            // Failing to save a UI preset should not break keyboard control.
        }
    }

    private string CreateZonePresetValueFromUI()
    {
        var zone1 = ToRgbHex(_zone1ColorPicker?.Color ?? Color.Parse(DefaultZone1Color));
        var zone2 = ToRgbHex(_zone2ColorPicker?.Color ?? Color.Parse(DefaultZone2Color));
        var zone3 = ToRgbHex(_zone3ColorPicker?.Color ?? Color.Parse(DefaultZone3Color));
        var zone4 = ToRgbHex(_zone4ColorPicker?.Color ?? Color.Parse(DefaultZone4Color));

        return $"{zone1},{zone2},{zone3},{zone4},{_keyboardBrightness}";
    }

    private void ApplySavedLightingEffectPresetToUI()
    {
        if (!File.Exists(KeyboardLightingEffectPresetPath))
            return;

        try
        {
            var value = File.ReadAllText(KeyboardLightingEffectPresetPath).Trim();

            if (!TryParseFourZoneMode(
                    value,
                    out var mode,
                    out var speed,
                    out var brightness,
                    out var direction,
                    out var red,
                    out var green,
                    out var blue))
                return;

            if (_lightingModeComboBox != null)
                _lightingModeComboBox.SelectedIndex = mode;

            SetLightingSpeed(speed);
            SetKeyboardBrightness(brightness);
            SetDirectionRadioButtons(direction);

            SetColorPicker(_lightEffectColorPicker, $"{red:X2}{green:X2}{blue:X2}");
        }
        catch
        {
            // Ignore broken preset file and let daemon settings win.
        }
    }

    private void SaveLightingEffectPresetFromUI(int mode, int direction, Color color)
    {
        try
        {
            WritePresetFile(
                KeyboardLightingEffectPresetPath,
                $"{mode},{_lightingSpeed},{_keyboardBrightness},{direction},{color.R},{color.G},{color.B}"
            );
        }
        catch
        {
            // Failing to save a UI preset should not break keyboard control.
        }
    }

    private static void WritePresetFile(string path, string value)
    {
        Directory.CreateDirectory(AppDataFolderPath);
        File.WriteAllText(path, value);
    }

    private int GetSelectedDirection()
    {
        if (_leftToRightRadioButton?.IsChecked == true)
            return DirectionLeftToRight;

        if (_rightToLeftRadioButton?.IsChecked == true)
            return DirectionRightToLeft;

        return DirectionLeftToRight;
    }

    private void SetDirectionRadioButtons(int direction)
    {
        if (_leftToRightRadioButton != null)
            _leftToRightRadioButton.IsChecked = direction == DirectionLeftToRight;

        if (_rightToLeftRadioButton != null)
            _rightToLeftRadioButton.IsChecked = direction == DirectionRightToLeft;

        if (_dirLeftToRightButton != null)
            _dirLeftToRightButton.IsChecked = direction == DirectionLeftToRight;

        if (_dirRightToLeftButton != null)
            _dirRightToLeftButton.IsChecked = direction == DirectionRightToLeft;
    }

    public static class AppState
    {
        public static bool DevMode { get; set; }
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}
