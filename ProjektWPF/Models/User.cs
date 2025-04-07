using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ProjektWPF.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string Email { get; set; }
        public string Password { get; set; }
        public int Age { get; set; }
        public ICollection<Expenses> Expenses { get; set; } = new List<Expenses>();
        public ICollection<Note> Notes { get; set; } = new List<Note>();
        public ICollection<UserMovie> UserMovies { get; set; } = new List<UserMovie>();

        public string? accessToken { get; set; }
        public string? refreshToken { get; set; }
    }

}
