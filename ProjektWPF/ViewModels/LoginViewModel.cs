using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ProjektWPF.Data;
using ProjektWPF.Services;

namespace ProjektWPF.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly FileService _fileService;
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialogs;

        private string _email = "";
        private string _password = "";
        private bool _rememberMe;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(FileService fileService, INavigationService navigation, IDialogService dialogs)
        {
            _fileService = fileService;
            _navigation = navigation;
            _dialogs = dialogs;

            LoginCommand = new AsyncRelayCommand(LoginAsync);
        }

        private async Task LoginAsync()
        {
            string passwordHash = PasswordHasher.HashPassword(Password);

            await using var context = new AppDbContext();
            var user = await context.Users.FirstOrDefaultAsync(p => p.Email == Email && p.Password == passwordHash);

            if (user == null)
            {
                _dialogs.ShowWarning("Błedne dane!");
                return;
            }

            if (RememberMe)
            {
                _fileService.AddLocalUserToFile(new Dictionary<long, string> { { user.Id, user.Name } });
            }

            Session.User = user;
            _navigation.Navigate(new HomeView());
        }
    }
}
