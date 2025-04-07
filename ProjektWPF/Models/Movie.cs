using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektWPF.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }

        public float MyRating { get; set; }

        public string Plot {  get; set; }
        public List<String> Actors { get; set; }
        public string Poster { get; set; }

         public ICollection<UserMovie> UserMovies { get; set; } = new List<UserMovie>();
    }
}
