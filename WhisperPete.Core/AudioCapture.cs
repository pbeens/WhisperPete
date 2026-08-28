using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace WhisperPete.Core
{
    public class AudioCapture : IDisposable
    {
        private WaveInEvent? _waveIn;
        private readonly List<float> _recordedSamples = new List<float>();
        private bool _isRecording;
        private readonly int _sampleRate = 16000;
        private System.Threading.Tasks.TaskCompletionSource<bool>? _stopTaskSource;

        public event EventHandler<float[]>? DataAvailable;

        public void Start()
        {
            if (_isRecording) return;

            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(_sampleRate, 1) // 16kHz mono
            };

            _waveIn.RecordingStopped += (s, e) =>
            {
                _stopTaskSource?.TrySetResult(true);
            };

            _waveIn.DataAvailable += (s, e) =>
            {
                for (int i = 0; i < e.BytesRecorded; i += 2)
                {
                    short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                    float sampleFloat = sample / 32768f;
                    _recordedSamples.Add(sampleFloat);
                }
            };

            _waveIn.StartRecording();
            _isRecording = true;
            Logger.Log("Audio recording started.");
        }

        public async System.Threading.Tasks.Task<float[]> StopAsync()
        {
            if (!_isRecording || _waveIn == null) return Array.Empty<float>();

            _stopTaskSource = new System.Threading.Tasks.TaskCompletionSource<bool>();
            _waveIn.StopRecording();
            _isRecording = false;

            // Wait for RecordingStopped event to ensure all buffers are processed
            await _stopTaskSource.Task;

            float[] result = _recordedSamples.ToArray();
            Logger.Log($"Audio recording stopped. Captured {result.Length} samples.");
            
            _recordedSamples.Clear();
            _waveIn.Dispose();
            _waveIn = null;
            
            return result;
        }

        [Obsolete("Use StopAsync instead")]
        public float[] Stop()
        {
            return StopAsync().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _waveIn?.Dispose();
        }
    }
}
