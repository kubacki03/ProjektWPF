using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ProjektWPF.Data;
using ProjektWPF.Models;
using ProjektWPF.Services;

namespace ProjektWPF.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialogs;

        private string _name = "";
        private string _email = "";
        private string _password = "";
        private string _age = "";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

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

        public string Age
        {
            get => _age;
            set => SetProperty(ref _age, value);
        }

        public ICommand RegisterCommand { get; }

        public RegisterViewModel(INavigationService navigation, IDialogService dialogs)
        {
            _navigation = navigation;
            _dialogs = dialogs;

            RegisterCommand = new AsyncRelayCommand(RegisterAsync);
        }

        private async Task RegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) || !int.TryParse(Age, out int age))
            {
                _dialogs.ShowWarning("Wypełnij poprawnie wszystkie pola!");
                return;
            }

            await using var context = new AppDbContext();

            if (await context.Users.AnyAsync(u => u.Email == Email))
            {
                _dialogs.ShowWarning("Email już istnieje!");
                return;
            }

            context.Users.Add(new User
            {
                Name = Name,
                Email = Email,
                Password = PasswordHasher.HashPassword(Password),
                Age = age
            });
            await context.SaveChangesAsync();

            _dialogs.ShowInfo("Rejestracja zakończona sukcesem!", "Sukces");
            _navigation.GoBack();
        }
    }
}
