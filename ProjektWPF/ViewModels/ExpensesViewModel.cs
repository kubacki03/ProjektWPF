using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ProjektWPF.Data;
using ProjektWPF.Models;
using ProjektWPF.Services;

namespace ProjektWPF.ViewModels
{
    public class ExpensesViewModel : ViewModelBase
    {
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialogs;

        private int? _selectedYear;
        private string _newName = "";
        private string _newValue = "";
        private string _newCategory = "";

        public ObservableCollection<Expenses> Expenses { get; } = new ObservableCollection<Expenses>();

        public IReadOnlyList<int> Years { get; } = new[] { 2023, 2024, 2025 };

        public int? SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value))
                {
                    _ = LoadExpensesAsync();
                }
            }
        }

        public string NewName
        {
            get => _newName;
            set => SetProperty(ref _newName, value);
        }

        public string NewValue
        {
            get => _newValue;
            set => SetProperty(ref _newValue, value);
        }

        public string NewCategory
        {
            get => _newCategory;
            set => SetProperty(ref _newCategory, value);
        }

        public ICommand AddExpenseCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand BackCommand { get; }

        public ExpensesViewModel(INavigationService navigation, IDialogService dialogs)
        {
            _navigation = navigation;
            _dialogs = dialogs;

            AddExpenseCommand = new AsyncRelayCommand(AddExpenseAsync);
            FilterCommand = new AsyncRelayCommand(LoadExpensesAsync);
            BackCommand = new RelayCommand(() => _navigation.Navigate(new HomeView()));

            _ = LoadExpensesAsync();
        }

        private async Task LoadExpensesAsync()
        {
            await using var context = new AppDbContext();
            int userId = Session.User.Id;

            var query = context.Expenses.Where(p => p.UserId == userId);

            if (SelectedYear.HasValue)
            {
                int year = SelectedYear.Value;
                query = query.Where(p => p.Date.Year == year);
            }

            var expenses = await query.ToListAsync();

            Expenses.Clear();
            foreach (var expense in expenses)
            {
                Expenses.Add(expense);
            }
        }

        private async Task AddExpenseAsync()
        {
            if (!int.TryParse(NewValue, out int value))
            {
                _dialogs.ShowWarning("Wprowadź poprawną wartość.");
                return;
            }

            var expense = new Expenses
            {
                Name = NewName,
                Category = NewCategory,
                Value = value,
                UserId = Session.User.Id
            };

            await using (var context = new AppDbContext())
            {
                context.Expenses.Add(expense);
                await context.SaveChangesAsync();
            }

            if (!SelectedYear.HasValue || SelectedYear.Value == expense.Date.Year)
            {
                Expenses.Add(expense);
            }
        }
    }
}
