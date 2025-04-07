using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using ProjektWPF.Data;
using ProjektWPF.Models;
using ProjektWPF.Services;

namespace ProjektWPF
{
    public partial class MoviesView : Page
    {
        private readonly MovieService _movieService;
        private readonly AppDbContext _context;

        public MoviesView()
        {
            _context = new AppDbContext();
            _movieService = new MovieService();
            InitializeComponent();
            LoadMovies();
        }

        private void LoadMovies()
        {
            MoviesListBox.ItemsSource = GetUserMovies();
        }

        public List<Movie> GetUserMovies()
        {
            var user = Session.User;
            return _context.Users
       .Where(u => u.Id == user.Id)
       .SelectMany(u => u.UserMovies.Select(um => new { Movie = um.Movie, Rating = um.Rating }))
       .ToList()
       .Select(x => {
           x.Movie.MyRating = x.Rating;
           return x.Movie;
       })
       .ToList();



        }

        public void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
        private void MoviesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MoviesListBox.SelectedItem is Movie selectedMovie)
            {
                
                TitleDetail.Text = selectedMovie.Title;
                YearDetail.Text = selectedMovie.Year.ToString();
                GenreDetail.Text = selectedMovie.Genre;
                RatingDetail.Text = selectedMovie.MyRating.ToString("0.0");
                PlotDetail.Text = selectedMovie.Plot;
                ActorsList.ItemsSource = selectedMovie.Actors;

               
                if (!string.IsNullOrEmpty(selectedMovie.Poster))
                {
                    PosterImage.Source = new BitmapImage(new Uri(selectedMovie.Poster));
                }
                else
                {
                    PosterImage.Source = null; 
                }
            }
        }

        private async void AddMovieButton_Click(object sender, RoutedEventArgs e)
        {
            string title = MovieTitleTextBox.Text;
            if (!float.TryParse(MovieRatingTextBox.Text, out float rating))
            {
                MessageBox.Show("Enter a valid rating (0-10).");
                return;
            }

            try
            {
                await AddMovie(title, rating);
                LoadMovies();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public async System.Threading.Tasks.Task AddMovie(string title, float myRating)
        {
            using (var context = new AppDbContext())
            {
                var user = Session.User;
                if (user == null) throw new Exception("User not logged in");

                var movie = context.Movies.FirstOrDefault(m => m.Title == title);
                if (movie == null)
                {
                    movie = await _movieService.GetMovieFromApi(title);
                    if (movie == null) throw new Exception("Movie not found in API");
                 
                    context.Movies.Add(movie);
                    context.SaveChanges();
                }

                var userMovie = context.UserMovies.FirstOrDefault(um => um.UserId == user.Id && um.MovieId == movie.Id);
                if (userMovie == null)
                {
                    userMovie = new UserMovie
                    {
                        UserId = user.Id,
                        MovieId = movie.Id,
                        Rating = myRating,
                        WatchedDate = DateTime.Now
                    };

                    context.UserMovies.Add(userMovie);
                    context.SaveChanges();
                }
            }
        }
    }
}
