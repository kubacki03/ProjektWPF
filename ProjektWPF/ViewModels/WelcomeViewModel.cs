using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ProjektWPF.Data;
using ProjektWPF.Services;

namespace ProjektWPF.ViewModels
{
    public record SavedUser(long Id, string Name);

    public class WelcomeViewModel : ViewModelBase
    {
        private readonly FileService _fileService;
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialogs;

        public ObservableCollection<SavedUser> SavedUsers { get; } = new ObservableCollection<SavedUser>();

        public ICommand LoginSavedUserCommand { get; }
        public ICommand ShowLoginCommand { get; }
        public ICommand ShowRegisterCommand { get; }

        public WelcomeViewModel(FileService fileService, INavigationService navigation, IDialogService dialogs)
        {
            _fileService = fileService;
            _navigation = navigation;
            _dialogs = dialogs;

            LoginSavedUserCommand = new AsyncRelayCommand(LoginSavedUserAsync);
            ShowLoginCommand = new RelayCommand(() => _navigation.Navigate(new LoginView()));
            ShowRegisterCommand = new RelayCommand(() => _navigation.Navigate(new RegisterView()));

            foreach (var user in _fileService.LoadData())
            {
                SavedUsers.Add(new SavedUser(user.Key, user.Value));
            }
        }

        private async Task LoginSavedUserAsync(object? parameter)
        {
            if (parameter is not long id)
            {
                return;
            }

            await using var context = new AppDbContext();
            var user = await context.Users.FirstOrDefaultAsync(p => p.Id == id);

            if (user == null)
            {
                _dialogs.ShowError("Nie znaleziono użytkownika.");
                return;
            }

            Session.User = user;
            _navigation.Navigate(new HomeView());
        }
    }
}
