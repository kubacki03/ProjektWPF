using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjektWPF.Data;
using ProjektWPF.Models;

namespace ProjektWPF.Services
{
    class ExpensesService
    {
        private readonly AppDbContext _context;

        public ExpensesService(AppDbContext context)
        {
            _context = context;
        }

        public void AddExpense(Expenses expenses)
        {
            var user = _context.Users.FirstOrDefault(p => p.Id == Session.User.Id);

            user?.Expenses.Add(expenses);

            _context.SaveChanges();
        }

        public List<Expenses> GetMonthlyExpenses(string month)
        {
            return _context.Expenses
                .Where(p => p.UserId == Session.User.Id &&
                            p.Date.ToString("MMMM", CultureInfo.InvariantCulture) == month)
                .ToList();
        }
    }
}