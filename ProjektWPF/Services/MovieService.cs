using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjektWPF.Models;

namespace ProjektWPF.Services
{
    public class MovieService
    {
        private readonly string api_key = Environment.GetEnvironmentVariable("OMDb_API_KEY");

        public async Task<Movie> GetMovieFromApi(string title)
        {
            string apiUrl = $"https://omdbapi.com/?apikey={api_key}&t={Uri.EscapeDataString(title)}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(responseBody);

                    if (json["Response"]?.ToString() != "True")
                    {
                        throw new Exception("Movie not found in OMDb API");
                    }

                    var movie = new Movie
                    {
                        Title = json["Title"]?.ToString(),
                        Genre = json["Genre"]?.ToString(),
                        Year = int.TryParse(json["Year"]?.ToString(), out int year) ? year : 0,
                        Plot = json["Plot"]?.ToString(),
                        Poster = json["Poster"]?.ToString(),
                        MyRating = float.TryParse(json["imdbRating"]?.ToString(),
                                     System.Globalization.NumberStyles.Any,
                                     System.Globalization.CultureInfo.InvariantCulture,
                                     out float rating) ? rating : 0,
                        Actors = json["Actors"]?.ToString()
                                    ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    ?.Select(a => a.Trim())
                                    ?.ToList() ?? new List<string>()
                    };

                    return movie;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Błąd HTTP: {e.Message}");
                    throw new Exception("Problem z połączeniem do API", e);
                }
                catch (JsonException e)
                {
                    Console.WriteLine($"Błąd parsowania JSON: {e.Message}");
                    throw new Exception("Nieprawidłowa odpowiedź z API", e);
                }
            }
        }
    }
}