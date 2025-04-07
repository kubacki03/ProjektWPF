using NAudio.Wave;
using ProjektWPF.Models;
using ProjektWPF.Models.ProjektWPF.Models;
using ProjektWPF.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Controls;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;
using System.Windows.Threading;
using ProjektWPF.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ProjektWPF
{
    public partial class HomeView : Page
    {
        private WaveInEvent waveIn; 
        private WaveFileWriter writer; 
        private string tempAudioFile = "recorded_audio.wav"; 
        public bool clicked=false;
       
        public ObservableCollection<Message> Messages { get; set; }
        SpotifyService SpotifyService { get; set; } = new SpotifyService();
        AppDbContext _context;

        private DispatcherTimer timer;
        private TimeSpan timeLeft;
        private MediaPlayer mediaPlayer;

        public HomeView()
        {
            _context = new AppDbContext();
            InitializeComponent();
            timeLeft = TimeSpan.FromMinutes(1); 
            mediaPlayer = new MediaPlayer();
            mediaPlayer.Open(new Uri("C:\\Users\\Kuba\\source\\repos\\ProjektWPF\\ProjektWPF\\Assets\\alarm.mp3", UriKind.RelativeOrAbsolute)); 


            timer = new DispatcherTimer();
         timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            Messages = new ObservableCollection<Message>
            {
                new Message { Author = "Asystent", Content = $"Hej {Session.User.Name}, jak moge Ci pomóc" }
               
            };
            if (TimeDisplay == null)
            {
                MessageBox.Show("Błąd: TimeDisplay nie został znaleziony w XAML!");
            }

            DataContext = this;  
           
           
          
            
            GetMonthExpenses();
              _=LoadWeatherAsync();

           
        }


        public void Logout(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
            "Czy chcesz się wylogować z tego komputera?",
            "Sukces",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information
            );

           if (result == MessageBoxResult.Yes)
            {
            
                DeleteFromMemory(Session.User.Id);

            }
            Session.User = null;
            NavigationService.Navigate(new Welcome());

        }

        public void DeleteFromMemory(int userId)
        {
            
            Dictionary<string, string> users;
            if (File.Exists("data.json"))
            {
                string json = File.ReadAllText("data.json");
                users = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            else
            {
                users = new Dictionary<string, string>();
            }

           
            string key = userId.ToString();
            if (users != null && users.ContainsKey(key))
            {
                users.Remove(key);

                string updatedJson = JsonConvert.SerializeObject(users, Formatting.Indented);
                File.WriteAllText("data.json", updatedJson);
            }
        }


        public void GetMonthExpenses()
        {
            var sum= _context.Expenses.Where(p => p.UserId == Session.User.Id).Sum(s => s.Value);
            MonthExpenses.Text = $"Wydatki: {sum}";
        }


        public async Task LoadWeatherAsync()
        {
            WeatherService weatherService = new WeatherService();
            string weatherData = await weatherService.GetWeather();

            Dispatcher.Invoke(() =>
            {
                Weather.Text = weatherData;
            });
        }

        private void SpotifyLoginButton_Click(object sender, RoutedEventArgs e)
        {
            SpotifyService.LoginWithSpotify();
        }
        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            SpotifyService spotify = new SpotifyService();
            await spotify.Pause();
        }
        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            SpotifyService spotify = new SpotifyService();
            await spotify.StartPlay();
        }
        private async void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SpotifyService spotify = new SpotifyService();
            await spotify.SetVolume((int) (100*volumeSlider.Value));
        }
        private async void StartPlayButton_Click(object sender, RoutedEventArgs e)
        {
          await SpotifyService.StartPlay();
        }
        private async void SkipNextButton_Click(object sender, RoutedEventArgs e)
        {
            await SpotifyService.SkipToNext();
        }
        private async void SkipPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            await SpotifyService.SkipToPrevious();
        }
        private bool isPanelOpen = false;


        private void ToggleSidePanel(object sender, RoutedEventArgs e)
        {
            double targetWidth = isPanelOpen ? 0 : 250; 
            var animation = new DoubleAnimation(targetWidth, TimeSpan.FromSeconds(0.3));
            SidePanelColumn.Width = new GridLength(targetWidth);
            isPanelOpen = !isPanelOpen;
        }

        bool isPanel2Open = false;

        private void ToggleSidePanel2(object sender, RoutedEventArgs e)
        {
            double targetWidth = isPanel2Open ? 0 : 250; 
            var animation = new DoubleAnimation(targetWidth, TimeSpan.FromSeconds(0.3));
            SidePanel2Column.Width = new GridLength(targetWidth);
            isPanel2Open = !isPanel2Open;
        }

        bool isPanel3Open = false;

        private async void ToggleSidePanel3(object sender, RoutedEventArgs e)
        {
           
            isPanel3Open = !isPanel3Open;

            if (isPanel3Open)
            {
                _ = UpdateCpuUsageAsync(); 
            }

            
            float availableRam = ComputerDataService.GetAvailableMemory();
            RamUsage.Text = $"Dostępny RAM: {availableRam} MB";

           
            double targetWidth = isPanel3Open ? 250 : 0;
            var animation = new DoubleAnimation(targetWidth, TimeSpan.FromSeconds(0.3));
            SidePanel3Column.Width = new GridLength(targetWidth);
        }

        
        private async Task UpdateCpuUsageAsync()
        {
            while (isPanel3Open)
            {
                float cpuUsage = await ComputerDataService.GetCpuUsage();
                CpuUsage.Text = $"CPU: {cpuUsage}%";
                await Task.Delay(10000); 
            }
        }


        private void NavigateToNote(object sender, RoutedEventArgs e)
        {
          
    
         NavigationService.Navigate(new NotebookView());
        }

        

             private void NavigateToExpenses(object sender, RoutedEventArgs e)
        {


            NavigationService.Navigate(new ExpensesView());
        }


        public void NavigateToMovies(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MoviesView());
        }
        private async void ToggleRecording(object sender, RoutedEventArgs e)
        {
            if (clicked == true)
            {
                clicked = false;
            }
            else
            {
                clicked= true;
            }
            if (clicked==true)
            {
                StartRecording();
                RecordButton.Content = "Stop Recording";
            }
            if( clicked == false)
            {
                StopRecording();
                RecordButton.Content = "Start Recording";
              
                await TranscribeAudioAsync();
            }
        }

       
        private void StartRecording()
        {
            waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(16000, 1) 
            };
            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;

            writer = new WaveFileWriter(tempAudioFile, waveIn.WaveFormat);

            waveIn.StartRecording();
        }

       
        private void StopRecording()
        {
            waveIn.StopRecording();
            writer?.Dispose();
          
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (writer != null)
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

     
        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            writer?.Dispose();
            waveIn?.Dispose();
        }

     
        private async Task TranscribeAudioAsync()
        {

            AIService ai = new AIService();
            Dictionary<string, string> dic = await ai.SpeechToText();

            foreach (var a in dic)
            {
                Messages.Add(new Message { Author = a.Key, Content = a.Value });
            }


        }
        
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (timeLeft.TotalSeconds > 0)
            {
                timeLeft = timeLeft.Subtract(TimeSpan.FromSeconds(1));
                UpdateDisplay();
            }
            else
            {
                timer.Stop();
                mediaPlayer.Play(); 
                MessageBox.Show("Czas minął!");
            }
        }

        private void StartTimer_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
        }

        private void StopTimer_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        private void SetTime_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(MinutesInput.Text, out int minutes) && int.TryParse(SecondsInput.Text, out int seconds))
            {
                timeLeft = new TimeSpan(0, minutes, seconds);
                UpdateDisplay();
            }
            else
            {
                MessageBox.Show("Wprowadź poprawne wartości minut i sekund!");
            }
        }

        private void UpdateDisplay()
        {
            TimeDisplay.Text = timeLeft.ToString(@"mm\:ss");
        }
        



    }
}
