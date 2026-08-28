using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NAudio.Wave;

namespace WhisperPete.Core
{
    public class WhisperEngine : IDisposable
    {
        private InferenceSession? _session;
        private readonly string _modelPath;
        private bool _isInitialized;

        public string ModelPath => _modelPath;
        public bool IsInitialized => _isInitialized;
        public bool SaveDebugRecordings { get; set; } = true;
        public string HardwareAccelerator { get; private set; } = "None";

        public WhisperEngine(string modelPath)
        {
            _modelPath = modelPath;
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            
            Logger.Log($"Engine: Starting initialization for model: {Path.GetFileName(_modelPath)}");
            
            if (!File.Exists(_modelPath))
            {
                string missingError = $"CRITICAL: Model file not found at path: {_modelPath}. Please check your settings.";
                Logger.Log($"Engine: {missingError}");
                HardwareAccelerator = "Error (File Missing)";
                throw new FileNotFoundException(missingError, _modelPath);
            }

            bool isGpuModel = _modelPath.Contains("gpu", StringComparison.OrdinalIgnoreCase);

            // Strategy: Try Device 0, then Device 1
            bool success = false;
            string lastGpuError = "";
            for (int i = 0; i <= 1; i++)
            {
                try
                {
                    Logger.Log($"Engine: Attempting GPU initialization on Device {i}...");
                    _session = CreateSession(true, i);
                    HardwareAccelerator = i == 0 ? "GPU (DirectML)" : $"GPU (DirectML - Device {i})";
                    Logger.Log($"Engine: Success! Hardware = {HardwareAccelerator}");
                    success = true;
                    break;
                }
                catch (Exception ex)
                {
                    lastGpuError = ex.Message;
                    if (ex.InnerException != null) lastGpuError += $" (Inner: {ex.InnerException.Message})";
                    Logger.Log($"Engine: GPU Device {i} failed. Reason: {lastGpuError}");
                    
                    // If Device 0 failed with a "Parameter is incorrect" (80070057) or similar driver error,
                    // trying Device 1 (which often doesn't exist) just adds noise/confusion with C0262002.
                    if (i == 0 && (lastGpuError.Contains("80070057") || lastGpuError.Contains("Fusion")))
                    {
                        Logger.Log("Engine: Device 0 failed with a likely driver/model fusion error. Skipping Device 1 probe.");
                        break; 
                    }
                }
            }

            if (!success)
            {
                if (isGpuModel)
                {
                    HardwareAccelerator = "Error (GPU Required)";
                    string errorMsg = $"CRITICAL: DirectML failed to initialize your GPU. This model REQUIRES a GPU to run safely. Reason: {lastGpuError}";
                    Logger.Log($"Engine: {errorMsg}");
                    throw new Exception(errorMsg);
                }

                try
                {
                    Logger.Log("Engine: Falling back to CPU session...");
                    _session = CreateSession(false);
                    HardwareAccelerator = "CPU (Fallback)";
                    Logger.Log("Engine: CPU Fallback initialized.");
                }
                catch (Exception cpuEx)
                {
                    HardwareAccelerator = "Error";
                    Logger.Log($"Engine: Fatal CPU Error: {cpuEx.Message}");
                    throw new Exception($"Fatal Error: Model failed on both GPU and CPU. GPU Error: {lastGpuError}", cpuEx);
                }
            }

            LogModelMetadata();
            _isInitialized = true;
        }

        private InferenceSession CreateSession(bool useGPU, int deviceId = 0)
        {
            var options = new SessionOptions();
            
            if (useGPU)
            {
                // Set DML options before registration
                options.EnableMemoryPattern = false; // MUST be false for DirectML
                options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL; // MUST be sequential for DirectML
                options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_BASIC; 
                options.AddSessionConfigEntry("session.disable_metacommands", "1"); // Bypasses unstable Conv kernels
                
                try 
                {
                    options.AppendExecutionProvider_DML(deviceId);
                    Logger.Log($"Engine: DirectML provider registered for Device {deviceId}");
                }
                catch (Exception ex)
                {
                    throw new Exception($"DirectML registration failed for device {deviceId}: {ex.Message}", ex);
                }
            }
            
            // Register ONNX Runtime Extensions
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string extensionsPath = Path.Combine(baseDir, "ortextensions.dll");
            
            if (!File.Exists(extensionsPath))
            {
                extensionsPath = Path.Combine(baseDir, "runtimes", "win-x64", "native", "ortextensions.dll");
            }

            if (File.Exists(extensionsPath))
            {
                options.RegisterCustomOpLibraryV2(extensionsPath, out _);
                Logger.Log("Engine: Custom Ops library registered.");
            }
            else
            {
                throw new FileNotFoundException("Custom Ops library 'ortextensions.dll' missing.");
            }

            return new InferenceSession(_modelPath, options);
        }

        private void LogModelMetadata()
        {
            try
            {
                Logger.Log("--- Model Metadata Loaded ---");
                Logger.Log($"Path: {_modelPath}");
                foreach (var input in _session!.InputMetadata)
                {
                    Logger.Log($"Input: {input.Key}, Type: {input.Value.ElementType}, Kind: {input.Value.OnnxValueType}");
                }
                Logger.Log("----------------------------------");
            }
            catch { }
        }

        public string Transcribe(float[] audioData)
        {
            if (!_isInitialized) Initialize();
            
            int totalSamples = audioData?.Length ?? 0;
            Logger.Log($"Transcription requested. Total sample count: {totalSamples}");

            if (SaveDebugRecordings)
            {
                SaveDebugAudio(audioData!);
            }

            if (totalSamples < 1600) // At least 100ms of audio
            {
                return "Error: Audio clip too short to transcribe.";
            }

            const int MaxSamplesPerChunk = 480000; // 30 seconds @ 16kHz
            var fullTranscript = new System.Text.StringBuilder();

            try
            {
                for (int i = 0; i < totalSamples; i += MaxSamplesPerChunk)
                {
                    int remaining = totalSamples - i;
                    int chunkSize = Math.Min(MaxSamplesPerChunk, remaining);
                    
                    // DirectML Conv nodes often fail with variable-length inputs.
                    // We pad every chunk to exactly 30 seconds (480,000 samples) with silence.
                    float[] paddedChunk = new float[MaxSamplesPerChunk];
                    Array.Copy(audioData!, i, paddedChunk, 0, chunkSize);

                    if (totalSamples > MaxSamplesPerChunk)
                    {
                        Logger.Log($"Processing chunk {i / MaxSamplesPerChunk + 1} ({chunkSize} samples, padded to {MaxSamplesPerChunk})...");
                    }
                    else
                    {
                        Logger.Log($"Processing audio clip ({chunkSize} samples, padded to {MaxSamplesPerChunk} for DML stability)...");
                    }

                    string chunkText = TranscribeChunk(paddedChunk);
                    
                    // If a chunk fails, we return what we have or the error
                    if (chunkText.StartsWith("Error:") || chunkText.Contains("ErrorCode:"))
                    {
                        if (fullTranscript.Length > 0) break; 
                        return chunkText;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(chunkText))
                    {
                        if (fullTranscript.Length > 0) fullTranscript.Append(" ");
                        fullTranscript.Append(chunkText);
                    }
                }

                var resultText = fullTranscript.ToString().Trim();
                Logger.Log($"Final transcription result: {resultText}");
                return resultText;
            }
            catch (Exception ex)
            {
                string msg = $"Transcription failed: {ex.Message}";
                Logger.Log(msg);
                return msg;
            }
        }

        private string TranscribeChunk(float[] audioData)
        {
            var container = new List<NamedOnnxValue>();
            
            // Complex models often have multiple inputs. We prioritize "audio_pcm" or the first float input.
            // Some models expect Int64 for parameters like 'min_length'.
            foreach (var input in _session!.InputMetadata)
            {
                Type type = input.Value.ElementType;
                string name = input.Key;

                if (type == typeof(byte))
                {
                    // Priority: If model wants bytes, it usually wants a WAV file buffer (with header) 
                    // so the AudioDecoder node can "detect" the format.
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var waveWriter = new WaveFileWriter(memoryStream, new WaveFormat(16000, 16, 1)))
                        {
                            foreach (var sample in audioData)
                            {
                                // Convert float [-1.0, 1.0] back to Int16
                                short pcm16 = (short)Math.Clamp(sample * 32767f, short.MinValue, short.MaxValue);
                                waveWriter.WriteSample(pcm16 / 32768f);
                            }
                            waveWriter.Flush();
                        }
                        byte[] wavData = memoryStream.ToArray();
                        var tensor = new DenseTensor<byte>(wavData, new[] { 1, wavData.Length });
                        container.Add(NamedOnnxValue.CreateFromTensor(name, tensor));
                    }
                }
                else if (type == typeof(float))
                {
                    if (name.Contains("audio") || name.Contains("features"))
                    {
                        var tensor = new DenseTensor<float>(audioData, new[] { 1, audioData.Length });
                        container.Add(NamedOnnxValue.CreateFromTensor(name, tensor));
                    }
                    else
                    {
                        // Scalar float parameter (e.g., length_penalty, repetition_penalty)
                        float val = 1.0f;
                        if (name.Contains("repetition_penalty")) val = 1.2f;
                        
                        var tensor = new DenseTensor<float>(new[] { val }, new[] { 1 }); // Rank 1 [1]
                        container.Add(NamedOnnxValue.CreateFromTensor(name, tensor));
                    }
                }
                else if (type == typeof(int))
                {
                    // Handle Int32 parameters
                    int[] val = { 1 };
                    // If it's a specific param like max_length, we could be more generous,
                    // but most all-in-one models have defaults inside. 
                    // Providing a safe generic "1" or matching model name.
                    if (name.Contains("max_length")) val[0] = 1024;
                    if (name.Contains("min_length")) val[0] = 0;
                    if (name.Contains("num_beams")) val[0] = 1;
                    if (name.Contains("num_return_sequences")) val[0] = 1;

                    // decoder_input_ids often expects Rank 2 [1, 1].
                    // Token 50258 is the standard Whisper <|startoftranscript|> token.
                    if (name.Contains("input_ids") || name.Contains("decoder_input_ids"))
                    {
                        val[0] = 50258; 
                        var tensor = new DenseTensor<int>(val, new[] { 1, 1 }); // Rank 2 [1, 1]
                        container.Add(NamedOnnxValue.CreateFromTensor(name, tensor));
                    }
                    else
                    {
                        var tensor = new DenseTensor<int>(val, new[] { 1 }); // Rank 1 [1]
                        container.Add(NamedOnnxValue.CreateFromTensor(name, tensor));
                    }
                }
                else if (type == typeof(long))
                {
                    long[] val = { 1 }; 
                    var tensor = new DenseTensor<long>(val, new[] { 1 }); // Rank 1 [1]
                    container.Add(NamedOnnxValue.CreateFromTensor(name, tensor));
                }
                else if (type == typeof(bool))
                {
                    bool[] val = { true };
                    var tensor = new DenseTensor<bool>(val, new[] { 1 });
                    container.Add(NamedOnnxValue.CreateFromTensor(name, tensor));
                }
                else if (type == typeof(double))
                {
                    double[] val = { 1.0 };
                    var tensor = new DenseTensor<double>(val, new[] { 1 }); // Rank 1 [1]
                    container.Add(NamedOnnxValue.CreateFromTensor(name, tensor));
                }
            }

            using (var results = _session!.Run(container))
            {
                var outputValue = results.FirstOrDefault();
                if (outputValue == null) return "No output from model";
                
                // Try to handle different output types (string, int64, etc.)
                try 
                {
                    var output = outputValue.AsEnumerable<string>().FirstOrDefault() ?? "";
                    
                    // Clean Whisper special tokens like <|0.00|> or <|endoftext|>
                    // Regex matches anything between <| and |>
                    output = Regex.Replace(output, @"<\|.*?\|>", "");
                    
                    var cleanedOutput = output.Trim();
                    return cleanedOutput;
                }
                catch 
                {
                    return $"Model returned data of type {outputValue.ValueType}";
                }
            }
        }

        private void SaveDebugAudio(float[] audioData)
        {
            try
            {
                string debugDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WhisperPete", "debug_recordings");
                if (!Directory.Exists(debugDir)) Directory.CreateDirectory(debugDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = Path.Combine(debugDir, $"recording_{timestamp}.wav");

                using (var waveWriter = new WaveFileWriter(filePath, new WaveFormat(16000, 16, 1)))
                {
                    waveWriter.WriteSamples(audioData, 0, audioData.Length);
                }
                
                Logger.Log($"Debug audio saved: {filePath}");
                
                // Optional: Keep only last 10 recordings to save space
                var files = Directory.GetFiles(debugDir, "*.wav")
                                     .OrderBy(f => new FileInfo(f).CreationTime)
                                     .ToList();
                while (files.Count > 10)
                {
                    File.Delete(files[0]);
                    files.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to save debug audio: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
