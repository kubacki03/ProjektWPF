using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Microsoft.EntityFrameworkCore;
using ProjektWPF.Data;
using ProjektWPF.Models;

namespace ProjektWPF
{
    /// <summary>
    /// Logika interakcji dla klasy ExpensesView.xaml
    /// </summary>
    public partial class ExpensesView : Page
    {
        private AppDbContext _context;
        private ObservableCollection<Expenses> _expensesCollection;


        public ExpensesView()
        {
            _context = new AppDbContext();
            InitializeComponent();


            _expensesCollection = new ObservableCollection<Expenses>();

            ExpensesListBox.ItemsSource = _expensesCollection;

            LoadExpenses();
        }

        public void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new HomeView());
        }
        private void LoadExpenses()
        {
          
            int? selectedYear = int.TryParse((YearFilter.SelectedItem as ComboBoxItem)?.Content.ToString(), out int y) ? y : (int?)null;

            var expensesQuery = _context.Expenses.Where(p => p.UserId == Session.User.Id);

          

            if (selectedYear.HasValue)
            {
                expensesQuery = expensesQuery.Where(p => p.Date.Year == selectedYear.Value);
            }

            var filteredExpenses = expensesQuery.ToList();
            _expensesCollection.Clear();

            foreach (var expense in filteredExpenses)
            {
                _expensesCollection.Add(expense);
            }
        }

        private void FilterExpenses(object sender, RoutedEventArgs e)
        {
            LoadExpenses();
        }



        public List<Expenses> GetUserExpenses()
        {
           return _context.Expenses.Where(p=>p.UserId==Session.User.Id).ToList();
        }

        private void AddExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            string expenseName = ExpenseName.Text;
            string expenseCategory = ExpenseCategory.Text;


            if(!int.TryParse(ExpenseValue.Text, out int value))
            {
                MessageBox.Show("Enter a valid value.");
                return;
            }

            var user = _context.Users.Include(x=>x.Expenses).FirstOrDefault(p => p.Id == Session.User.Id);
            var expense = new Expenses { Category=expenseCategory, Name=expenseName, UserId=user.Id, User=user, Value=value  };


          

          
               _context.Expenses.Add(expense);
            user.Expenses.Add(expense);
                _context.SaveChanges();
                _expensesCollection.Add(expense);
                
            
          
        }

       


        private void ExpensesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExpensesListBox.SelectedItem is Expenses selectedMovie)
            {
              

             
            }
        }
    }
}
