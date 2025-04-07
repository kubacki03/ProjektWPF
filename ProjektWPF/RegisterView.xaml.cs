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
using System.Windows.Shapes;

namespace ProjektWPF
{
    /// <summary>
    /// Logika interakcji dla klasy RegisterView.xaml
    /// </summary>
    using System;
    using System.Linq;
    using System.Windows;
    using ProjektWPF.Data;
    using ProjektWPF.Models;
    using ProjektWPF.Services;

    public partial class RegisterView : Page
    {
        public RegisterView()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;
            int age;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) || !int.TryParse(AgeTextBox.Text, out age))
            {
                MessageBox.Show("Wypełnij poprawnie wszystkie pola!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new AppDbContext())
            {
                if (db.Users.Any(u => u.Email == email))
                {
                    MessageBox.Show("Email już istnieje!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = new User { Name = name, Email = email, Password =PasswordHasher.HashPassword(password), Age = age };
                db.Users.Add(user);
                db.SaveChanges();
              
                MessageBox.Show("Rejestracja zakończona sukcesem!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
        }
    }

}
