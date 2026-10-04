using System.IO;
using System.Windows.Media;

namespace ProjektWPF.Services
{
    public class AlarmService
    {
        private readonly MediaPlayer _player = new MediaPlayer();

        public AlarmService()
        {
            _player.Open(new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "alarm.mp3"), UriKind.Absolute));
        }

        public void Play()
        {
            _player.Position = TimeSpan.Zero;
            _player.Play();
        }
    }
}
