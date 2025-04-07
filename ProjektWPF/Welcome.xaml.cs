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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace ProjektWPF
{
    /// <summary>
    /// Logika interakcji dla klasy Welcome.xaml
    /// </summary>
    public partial class Welcome : Page
    {
        AppDbContext _context;
        FileService _fileService;
        public Welcome()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _fileService = new FileService();
            LoadUsers(); 



        }
        private void LoadUsers()
        {
            Dictionary<long, string> users = _fileService.LoadData();


            foreach (var user in users)
            {
                StackPanel userStack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(10), HorizontalAlignment = HorizontalAlignment.Center };

                
                Grid circle = new Grid { Width = 100, Height = 100 };
                Ellipse ellipse = new Ellipse { Width = 100, Height = 100, Fill = Brushes.LightGray, Stroke = Brushes.Black, StrokeThickness = 2 };
                TextBlock emoji = new TextBlock { Text = "🧑", FontSize = 50, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

                circle.Children.Add(ellipse);
                circle.Children.Add(emoji);

               
                circle.Tag = user.Key; 

               
                circle.MouseLeftButtonUp += (sender, e) => LoginBySaveAccount(sender, e);

                TextBlock nameText = new TextBlock { Text = user.Value, FontSize = 20, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };

             
                userStack.Children.Add(circle);
                userStack.Children.Add(nameText);

                UsersPanel.Children.Add(userStack);
            }
        }


        private void ShowRegister(object sender, RoutedEventArgs e)
        {

       
            NavigationService.Navigate(new RegisterView()); 
        }

        private void Login(object sender, RoutedEventArgs e)
        {
         NavigationService.Navigate(new LoginView());   


        }

        private void LoginBySaveAccount(object sender, RoutedEventArgs e)
        {

            long id = (long)((Grid)sender).Tag;


            var user = _context.Users.FirstOrDefault(p => p.Id == id);

            if (user != null)
            {

                Session.User = user;
               
                NavigationService.Navigate(new HomeView());
            }
            else
            {
                MessageBox.Show("Nie znaleziono użytkownika.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
