using System;
using System.Windows;
using WhisperPete.Core;
using Microsoft.Win32;
using System.IO;

namespace WhisperPete.Tray
{
    public partial class MainWindow : Window
    {
        private readonly AudioCapture _audioCapture = new AudioCapture();
        private readonly HotkeyManager _hotkeyManager = new HotkeyManager();
        private WhisperEngine? _engine;
        private AppSettings _currentSettings = new AppSettings();
        private bool _isRecording;
        private bool _isLoading = true;
        private RecordingOverlay? _overlay;

        public MainWindow()
        {
            // Load settings BEFORE InitializeComponent so defaults don't overwrite
            _currentSettings = SettingsManager.Load();
            
            InitializeComponent();
            
            SourceInitialized += (s, e) => 
            {
                RegisterHotkey();
                UpdateUIFromSettings();
                _isLoading = false; // Done loading
            };
            
            Closing += (s, e) =>
            {
                e.Cancel = true;
                Hide();
            };
        }

        private void UpdateUIFromSettings()
        {
            // Update UI without triggering saves
            ChkSaveDebug.IsChecked = _currentSettings.SaveDebugRecordings;
            ChkStartWithWindows.IsChecked = _currentSettings.StartWithWindows;

            if (!string.IsNullOrEmpty(_currentSettings.ModelPath))
            {
                InitializeModel(_currentSettings.ModelPath);
            }
        }


        public void RegisterHotkey()
        {
            try 
            {
                // Register Ctrl + Alt + W as global hotkey
                // Modifiers: Alt = 0x0001, Ctrl = 0x0002 => 0x0003. 'W' = 0x57
                _hotkeyManager.Register(this, 0x0003, 0x57); 
                _hotkeyManager.HotkeyPressed += ToggleRecording;
                TxtStatus.Text = "Status: Idle (Ctrl + Alt + W active)";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Status: Hotkey Error - {ex.Message}";
            }
        }

        public void ToggleRecording()
        {
            if (_engine == null)
            {
                System.Windows.MessageBox.Show("Please select an ONNX model first.");
                Show();
                WindowState = WindowState.Normal;
                Activate();
                return;
            }

            if (!_isRecording)
            {
                StartRecording();
            }
            else
            {
                _ = StopRecordingAsync();
            }
        }

        private void StartRecording()
        {
            _audioCapture.Start();
            BtnRecord.Content = "Stop Recording";
            TxtStatus.Text = "Status: Recording...";
            _isRecording = true;
            
            // Visual feedback
            _overlay = new RecordingOverlay();
            _overlay.Show();
            
            // Audio feedback
            System.Media.SystemSounds.Asterisk.Play();
        }

        private async System.Threading.Tasks.Task StopRecordingAsync()
        {
            try 
            {
                var audioData = await _audioCapture.StopAsync();
                BtnRecord.Content = "Start Recording";
                TxtStatus.Text = "Status: Transcribing...";
                _isRecording = false;
                
                // Visual feedback
                _overlay?.Close();
                _overlay = null;
                
                // Audio feedback
                System.Media.SystemSounds.Exclamation.Play();

                string resultText;
                if (_engine != null)
                {
                    resultText = await System.Threading.Tasks.Task.Run(() => _engine.Transcribe(audioData));
                }
                else
                {
                    resultText = $"Dictation: Recorded {audioData.Length} samples.";
                }

                TxtResult.Text = resultText;
                TxtStatus.Text = "Status: Idle";
                
                // Inject transcribed text into active window
                TextInjector.InjectText(resultText);
            }
            catch (Exception ex)
            {
                string errorMsg = $"Critical Recording Error: {ex.Message}";
                (System.Windows.Application.Current as App)?.Log($"{errorMsg}\n{ex.StackTrace}");
                TxtStatus.Text = "Status: Error";
                TxtResult.Text = errorMsg;
                
                // Ensure UI is reset even on error
                _isRecording = false;
                BtnRecord.Content = "Start Recording";
                _overlay?.Close();
                _overlay = null;
            }
        }

        private void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            ToggleRecording();
        }

        private void BtnSelectModel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ONNX Models (*.onnx)|*.onnx|All Files (*.*)|*.*",
                Title = "Select Whisper ONNX Model"
            };

            if (dialog.ShowDialog() == true)
            {
                InitializeModel(dialog.FileName);
            }
        }

        private void InitializeModel(string path)
        {
            try
            {
                if (_engine != null && _engine.IsInitialized && _engine.ModelPath == path)
                {
                    return; // Already initialized correctly
                }

                TxtModelPath.Text = path;
                TxtStatus.Text = "Status: Initializing engine...";
                
                // Initialize the engine
                _engine = new WhisperEngine(path);
                _engine.SaveDebugRecordings = _currentSettings.SaveDebugRecordings;
                _engine.Initialize();
                
                // Save settings
                _currentSettings.ModelPath = path;
                SettingsManager.Save(_currentSettings);

                TxtStatus.Text = "Status: Idle (Model loaded)";
                TxtAccelerator.Text = _engine.HardwareAccelerator;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading model: {ex.Message}");
                _engine = null;
                TxtModelPath.Text = "No model loaded.";
                TxtStatus.Text = "Status: Error loading model.";
            }
        }

        private void ChkSaveDebug_Changed(object sender, RoutedEventArgs e)
        {
            if (_currentSettings == null || _isLoading) return;
            _currentSettings.SaveDebugRecordings = ChkSaveDebug.IsChecked ?? true;
            if (_engine != null) _engine.SaveDebugRecordings = _currentSettings.SaveDebugRecordings;
            SettingsManager.Save(_currentSettings);
        }

        private void ChkStartWithWindows_Changed(object sender, RoutedEventArgs e)
        {
            if (_currentSettings == null || _isLoading || !IsLoaded) return;
            bool start = ChkStartWithWindows.IsChecked ?? false;
            _currentSettings.StartWithWindows = start;
            SettingsManager.Save(_currentSettings);

            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (start)
                        {
                            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                            if (!string.IsNullOrEmpty(exePath) && exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                key.SetValue("WhisperPete", $"\"{exePath}\"");
                            }
                        }
                        else
                        {
                            key.DeleteValue("WhisperPete", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                (System.Windows.Application.Current as App)?.Log($"Failed to update Startup registry: {ex.Message}");
            }
        }
    }
}