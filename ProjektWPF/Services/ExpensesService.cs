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
    /// <summary>
    /// Serwis do zarządzania wydatkami użytkowników
    /// Wymaga przekazania kontekstu bazy danych w konstruktorze
    /// </summary>
    class ExpensesService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Inicjalizuje nową instancję serwisu z podanym kontekstem bazy danych
        /// </summary>
        /// <param name="context">Kontekst bazy danych</param>
        public ExpensesService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Dodaje nowy wydatek dla aktualnie zalogowanego użytkownika
        /// </summary>
        /// <param name="expenses">Obiekt wydatku do dodania</param>
        /// <remarks>
        /// Używa statycznej klasy Session do pobrania ID aktualnego użytkownika
        /// Automatycznie zapisuje zmiany w bazie danych
        /// </remarks>
        public void AddExpense(Expenses expenses)
        {
            // Pobierz aktualnego użytkownika z bazy danych
            var user = _context.Users.FirstOrDefault(p => p.Id == Session.User.Id);

            // Dodaj wydatek do kolekcji użytkownika
            user?.Expenses.Add(expenses);

            // Zapisz zmiany w bazie
            _context.SaveChanges();
        }

        /// <summary>
        /// Pobiera listę wydatków dla aktualnego użytkownika w podanym miesiącu
        /// </summary>
        /// <param name="month">Nazwa miesiąca w języku angielskim (np. "January")</param>
        /// <returns>Lista wydatków spełniających kryteria</returns>
        /// <remarks>
        /// Porównuje miesiąc w formacie tekstowym używając kultury invariant
        /// Wymaga spójnego formatu dat w bazie danych
        /// </remarks>
        public List<Expenses> GetMonthlyExpenses(string month)
        {
            return _context.Expenses
                .Where(p => p.UserId == Session.User.Id &&
                            p.Date.ToString("MMMM", CultureInfo.InvariantCulture) == month)
                .ToList();
        }
    }
}