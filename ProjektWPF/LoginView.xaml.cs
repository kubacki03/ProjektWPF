using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ProjektWPF.Data;
using ProjektWPF.Services;

namespace ProjektWPF
{
    /// <summary>
    /// Logika interakcji dla klasy LoginView.xaml
    /// </summary>
    public partial class LoginView : Page
    {

        AppDbContext _context;
        public LoginView()
        {
            _context = new AppDbContext();
            InitializeComponent();
        }


        public void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var user= _context.Users.FirstOrDefault(p=> p.Email==EmailTextBox.Text && p.Password==  PasswordHasher.HashPassword(PasswordBox.Password));

            if (user == null) {
               
                MessageBox.Show("Błedne dane!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (RememberMe.IsChecked == true) {
                FileService fileService = new FileService();
                Dictionary<long, string> data = new Dictionary<long, string>();
                data.Add(user.Id, user.Name);
                fileService.AddLocalUserToFile(data);
            }

            Session.User = user;
            NavigationService.Navigate(new HomeView());
        }
    }
}
