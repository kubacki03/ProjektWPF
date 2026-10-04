using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using ProjektWPF.Data;
using ProjektWPF.Models.ProjektWPF.Models;
using ProjektWPF.Services;

namespace ProjektWPF.ViewModels
{
    public class HomeViewModel : ViewModelBase, IDisposable
    {
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialogs;
        private readonly FileService _fileService;
        private readonly AudioRecorderService _recorder = new AudioRecorderService();
        private readonly SpotifyService _spotify = new SpotifyService();
        private readonly AlarmService _alarm = new AlarmService();
        private readonly DispatcherTimer _timer;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        private AIService? _aiService;
        private CancellationTokenSource? _systemMonitorCts;
        private TimeSpan _timeLeft = TimeSpan.FromMinutes(1);

        private string _monthExpensesText = "";
        private string _weatherText = "";
        private string _ramText = "";
        private string _cpuText = "";
        private string _timeDisplay = "01:00";
        private string _minutesInput = "1";
        private string _secondsInput = "00";
        private double _volumeLevel = 0.5;
        private bool _isRecording;

        public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();

        public string MonthExpensesText
        {
            get => _monthExpensesText;
            private set => SetProperty(ref _monthExpensesText, value);
        }

        public string WeatherText
        {
            get => _weatherText;
            private set => SetProperty(ref _weatherText, value);
        }

        public string RamText
        {
            get => _ramText;
            private set => SetProperty(ref _ramText, value);
        }

        public string CpuText
        {
            get => _cpuText;
            private set => SetProperty(ref _cpuText, value);
        }

        public string TimeDisplay
        {
            get => _timeDisplay;
            private set => SetProperty(ref _timeDisplay, value);
        }

        public string MinutesInput
        {
            get => _minutesInput;
            set => SetProperty(ref _minutesInput, value);
        }

        public string SecondsInput
        {
            get => _secondsInput;
            set => SetProperty(ref _secondsInput, value);
        }

        public bool IsRecording
        {
            get => _isRecording;
            private set
            {
                if (SetProperty(ref _isRecording, value))
                {
                    OnPropertyChanged(nameof(RecordButtonText));
                }
            }
        }

        public string RecordButtonText => IsRecording ? "Stop Recording" : "Start Recording";

        public bool IsSystemPanelOpen { get; private set; }

        public double VolumeLevel
        {
            get => _volumeLevel;
            set
            {
                if (SetProperty(ref _volumeLevel, value))
                {
                    _ = RunSpotifyAsync(() => _spotify.SetVolume((int)(100 * value)));
                }
            }
        }

        public ICommand ToggleRecordingCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ToggleSystemMonitorCommand { get; }

        public ICommand SpotifyLoginCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StartPlayCommand { get; }
        public ICommand SkipNextCommand { get; }
        public ICommand SkipPreviousCommand { get; }

        public ICommand StartTimerCommand { get; }
        public ICommand StopTimerCommand { get; }
        public ICommand SetTimeCommand { get; }

        public ICommand NavigateToNotesCommand { get; }
        public ICommand NavigateToMoviesCommand { get; }
        public ICommand NavigateToExpensesCommand { get; }

        public HomeViewModel(FileService fileService, INavigationService navigation, IDialogService dialogs)
        {
            _fileService = fileService;
            _navigation = navigation;
            _dialogs = dialogs;

            Messages.Add(new Message { Author = "Asystent", Content = $"Hej {Session.User.Name}, jak moge Ci pomóc" });

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTimerTick;

            ToggleRecordingCommand = new AsyncRelayCommand(ToggleRecordingAsync);
            LogoutCommand = new RelayCommand(Logout);
            ToggleSystemMonitorCommand = new AsyncRelayCommand(ToggleSystemMonitorAsync);

            SpotifyLoginCommand = new RelayCommand(() => SpotifyService.LoginWithSpotify());
            PauseCommand = new AsyncRelayCommand(() => RunSpotifyAsync(_spotify.Pause));
            StartPlayCommand = new AsyncRelayCommand(() => RunSpotifyAsync(_spotify.StartPlay));
            SkipNextCommand = new AsyncRelayCommand(() => RunSpotifyAsync(_spotify.SkipToNext));
            SkipPreviousCommand = new AsyncRelayCommand(() => RunSpotifyAsync(_spotify.SkipToPrevious));

            StartTimerCommand = new RelayCommand(() => _timer.Start());
            StopTimerCommand = new RelayCommand(() => _timer.Stop());
            SetTimeCommand = new RelayCommand(SetTime);

            NavigateToNotesCommand = new RelayCommand(() => _navigation.Navigate(new NotebookView()));
            NavigateToMoviesCommand = new RelayCommand(() => _navigation.Navigate(new MoviesView()));
            NavigateToExpensesCommand = new RelayCommand(() => _navigation.Navigate(new ExpensesView()));

            _ = LoadMonthExpensesAsync();
            _ = LoadWeatherAsync();
        }

        private async Task LoadMonthExpensesAsync()
        {
            await using var context = new AppDbContext();
            int userId = Session.User.Id;

            int sum = await context.Expenses.Where(p => p.UserId == userId).SumAsync(s => s.Value);
            MonthExpensesText = $"Wydatki: {sum}";
        }

        private async Task LoadWeatherAsync()
        {
            try
            {
                WeatherText = await new WeatherService().GetWeather();
            }
            catch (Exception)
            {
                WeatherText = "Brak danych pogodowych";
            }
        }

        private void Logout()
        {
            if (_dialogs.Confirm("Czy chcesz się wylogować z tego komputera?", "Wylogowanie"))
            {
                _fileService.RemoveLocalUser(Session.User.Id);
            }

            Session.User = null!;
            _navigation.Navigate(new Welcome());
        }

        private async Task ToggleRecordingAsync()
        {
            if (!_recorder.IsRecording)
            {
                try
                {
                    _recorder.Start();
                    IsRecording = true;
                }
                catch (Exception ex)
                {
                    _dialogs.ShowError($"Nie można rozpocząć nagrywania: {ex.Message}");
                }

                return;
            }

            IsRecording = false;

            try
            {
                string audioPath = await _recorder.StopAsync();
                await AskAssistantAsync(audioPath, _lifetime.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Błąd asystenta AI: {ex.Message}");
            }
        }

        private async Task AskAssistantAsync(string audioPath, CancellationToken cancellationToken)
        {
            _aiService ??= new AIService();

            string userText = await _aiService.TranscribeAsync(audioPath, cancellationToken);
            if (string.IsNullOrWhiteSpace(userText))
            {
                return;
            }

            Messages.Add(new Message { Author = "User", Content = userText });

            string answer = await _aiService.ChatAsync(userText, cancellationToken);
            Messages.Add(new Message { Author = "Bot", Content = answer });

            await _aiService.SpeakAsync(answer, cancellationToken);
        }

        private async Task RunSpotifyAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Błąd Spotify: {ex.Message}");
            }
        }

        private async Task ToggleSystemMonitorAsync()
        {
            IsSystemPanelOpen = !IsSystemPanelOpen;
            OnPropertyChanged(nameof(IsSystemPanelOpen));

            if (!IsSystemPanelOpen)
            {
                _systemMonitorCts?.Cancel();
                return;
            }

            _systemMonitorCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            var token = _systemMonitorCts.Token;

            try
            {
                float availableRam = await Task.Run(ComputerDataService.GetAvailableMemory, token);
                RamText = $"Dostępny RAM: {availableRam} MB";

                while (!token.IsCancellationRequested)
                {
                    float cpuUsage = await ComputerDataService.GetCpuUsage();
                    CpuText = $"CPU: {cpuUsage:F0}%";
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void SetTime()
        {
            if (int.TryParse(MinutesInput, out int minutes) && int.TryParse(SecondsInput, out int seconds))
            {
                _timeLeft = new TimeSpan(0, minutes, seconds);
                UpdateTimeDisplay();
            }
            else
            {
                _dialogs.ShowWarning("Wprowadź poprawne wartości minut i sekund!");
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_timeLeft.TotalSeconds > 0)
            {
                _timeLeft = _timeLeft.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimeDisplay();
            }
            else
            {
                _timer.Stop();
                _alarm.Play();
                _dialogs.ShowInfo("Czas minął!", "Timer");
            }
        }

        private void UpdateTimeDisplay()
        {
            TimeDisplay = _timeLeft.ToString(@"mm\:ss");
        }

        public void Dispose()
        {
            _timer.Stop();
            _lifetime.Cancel();
            _recorder.Dispose();
        }
    }
}
