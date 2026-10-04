using System.Windows;
using System.Windows.Controls;
using ProjektWPF.Services;
using ProjektWPF.ViewModels;

namespace ProjektWPF
{
    public partial class LoginView : Page
    {
        private readonly LoginViewModel _viewModel;

        public LoginView()
        {
            InitializeComponent();

            _viewModel = new LoginViewModel(new FileService(), AppServices.Navigation, AppServices.Dialogs);
            DataContext = _viewModel;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = ((PasswordBox)sender).Password;
        }
    }
}
