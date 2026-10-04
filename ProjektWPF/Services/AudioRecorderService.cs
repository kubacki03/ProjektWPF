using System.IO;
using NAudio.Wave;

namespace ProjektWPF.Services
{
    public sealed class AudioRecorderService : IDisposable
    {
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _writer;
        private TaskCompletionSource? _stopped;

        public string FilePath { get; } = Path.Combine(Path.GetTempPath(), "recorded_audio.wav");

        public bool IsRecording => _waveIn != null;

        public void Start()
        {
            if (IsRecording)
            {
                return;
            }

            _waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(16000, 1)
            };
            _writer = new WaveFileWriter(FilePath, _waveIn.WaveFormat);
            _stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
            _waveIn.StartRecording();
        }

        public async Task<string> StopAsync()
        {
            if (_waveIn == null || _stopped == null)
            {
                throw new InvalidOperationException("Nagrywanie nie zostało rozpoczęte.");
            }

            _waveIn.StopRecording();
            await _stopped.Task;

            return FilePath;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            Cleanup();

            if (e.Exception != null)
            {
                _stopped?.TrySetException(e.Exception);
            }
            else
            {
                _stopped?.TrySetResult();
            }
        }

        private void Cleanup()
        {
            _writer?.Dispose();
            _writer = null;
            _waveIn?.Dispose();
            _waveIn = null;
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
