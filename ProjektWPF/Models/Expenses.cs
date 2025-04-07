using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektWPF.Models
{
    public class Expenses
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public string Category { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
