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
    /// <summary>
    /// Serwis do pobierania informacji o filmach z zewnętrznego API OMDb
    /// Wymaga klucza API przechowywanego w zmiennych środowiskowych jako "OMDb_API_KEY"
    /// </summary>
    internal class MovieService
    {
        private readonly string api_key = Environment.GetEnvironmentVariable("OMDb_API_KEY");

        /// <summary>
        /// Pobiera pełne informacje o filmie z API OMDb na podstawie tytułu
        /// </summary>
        /// <param name="title">Tytuł filmu do wyszukania</param>
        /// <returns>Obiekt Movie z danymi lub null w przypadku błędu</returns>
        /// <exception cref="Exception">Może zwrócić wyjątek w przypadku błędów API</exception>
        public async Task<Movie> GetMovieFromApi(string title)
        {
            // Konstruowanie URL z parametrami zapytania
            string apiUrl = $"https://omdbapi.com/?apikey={api_key}&t={Uri.EscapeDataString(title)}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Wysłanie żądania GET do API
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode(); // Sprawdzenie kodów błędów HTTP

                    // Odczyt i parsowanie odpowiedzi
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(responseBody);

                    // Sprawdzenie odpowiedzi API
                    if (json["Response"]?.ToString() != "True")
                        throw new Exception("Movie not found in OMDb API");

                    // Mapowanie JSON na obiekt Movie
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
                    // Obsługa błędów sieciowych
                    Console.WriteLine($"Błąd HTTP: {e.Message}");
                    throw new Exception("Problem z połączeniem do API", e);
                }
                catch (JsonException e)
                {
                    // Obsługa błędów parsowania JSON
                    Console.WriteLine($"Błąd parsowania JSON: {e.Message}");
                    throw new Exception("Nieprawidłowa odpowiedź z API", e);
                }
            }
        }
    }
}