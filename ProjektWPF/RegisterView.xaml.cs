using System.Windows;
using System.Windows.Controls;
using ProjektWPF.Services;
using ProjektWPF.ViewModels;

namespace ProjektWPF
{
    public partial class RegisterView : Page
    {
        private readonly RegisterViewModel _viewModel;

        public RegisterView()
        {
            InitializeComponent();

            _viewModel = new RegisterViewModel(AppServices.Navigation, AppServices.Dialogs);
            DataContext = _viewModel;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = ((PasswordBox)sender).Password;
        }
    }
}
