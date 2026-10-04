using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ProjektWPF.Data;
using ProjektWPF.Models;
using ProjektWPF.Services;

namespace ProjektWPF.ViewModels
{
    public class MoviesViewModel : ViewModelBase
    {
        private readonly MovieService _movieService;
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialogs;

        private Movie? _selectedMovie;
        private string _newTitle = "";
        private string _newRating = "";

        public ObservableCollection<Movie> Movies { get; } = new ObservableCollection<Movie>();

        public Movie? SelectedMovie
        {
            get => _selectedMovie;
            set => SetProperty(ref _selectedMovie, value);
        }

        public string NewTitle
        {
            get => _newTitle;
            set => SetProperty(ref _newTitle, value);
        }

        public string NewRating
        {
            get => _newRating;
            set => SetProperty(ref _newRating, value);
        }

        public ICommand AddMovieCommand { get; }
        public ICommand SortCommand { get; }
        public ICommand BackCommand { get; }

        public MoviesViewModel(MovieService movieService, INavigationService navigation, IDialogService dialogs)
        {
            _movieService = movieService;
            _navigation = navigation;
            _dialogs = dialogs;

            AddMovieCommand = new AsyncRelayCommand(AddMovieAsync);
            SortCommand = new RelayCommand(SortByRating);
            BackCommand = new RelayCommand(() => _navigation.GoBack());

            _ = LoadMoviesAsync();
        }

        private async Task LoadMoviesAsync()
        {
            await using var context = new AppDbContext();
            int userId = Session.User.Id;

            var userMovies = await context.UserMovies
                .Where(um => um.UserId == userId)
                .Select(um => new { um.Movie, um.Rating })
                .ToListAsync();

            Movies.Clear();
            foreach (var item in userMovies)
            {
                item.Movie.MyRating = item.Rating;
                Movies.Add(item.Movie);
            }
        }

        private void SortByRating()
        {
            var sorted = Movies.OrderByDescending(m => m.MyRating).ToList();

            Movies.Clear();
            foreach (var movie in sorted)
            {
                Movies.Add(movie);
            }
        }

        private async Task AddMovieAsync()
        {
            if (!float.TryParse(NewRating, out float rating))
            {
                _dialogs.ShowWarning("Wprowadź poprawną ocenę (0-10).");
                return;
            }

            try
            {
                await AddMovieAsync(NewTitle, rating);
                await LoadMoviesAsync();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError(ex.Message);
            }
        }

        private async Task AddMovieAsync(string title, float rating)
        {
            int userId = Session.User.Id;

            await using var context = new AppDbContext();

            var movie = await context.Movies.FirstOrDefaultAsync(m => m.Title == title);
            if (movie == null)
            {
                movie = await _movieService.GetMovieFromApi(title);
                if (movie == null)
                {
                    throw new Exception("Movie not found in API");
                }

                context.Movies.Add(movie);
                await context.SaveChangesAsync();
            }

            bool alreadyAdded = await context.UserMovies.AnyAsync(um => um.UserId == userId && um.MovieId == movie.Id);
            if (!alreadyAdded)
            {
                context.UserMovies.Add(new UserMovie
                {
                    UserId = userId,
                    MovieId = movie.Id,
                    Rating = rating,
                    WatchedDate = DateTime.Now
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
