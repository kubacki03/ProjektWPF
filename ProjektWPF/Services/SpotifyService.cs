using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using ProjektWPF.Data;

namespace ProjektWPF.Services
{
    class SpotifyService
    {
        // Dane uwierzytelniające aplikację w Spotify
        private static readonly string CLIENT_ID = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
        private static readonly string CLIENT_SECRET = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
        private static readonly string REDIRECT_URI = "http://localhost:2137/callback";
        private static readonly string TOKEN_URL = "https://accounts.spotify.com/api/token";

        // Tokeny dostępu i odświeżania
        private static string accessToken;
        private static string refreshToken;

        // Kontekst bazy danych
        private static readonly AppDbContext _context = new AppDbContext();

        /// <summary>
        /// Metoda do odświeżania tokenu dostępu przy użyciu refresh token
        /// </summary>
        private static async Task GetRef()
        {
            using (HttpClient client = new HttpClient())
            {
                // Przygotowanie żądania POST do endpointa tokenowego
                var request = new HttpRequestMessage(HttpMethod.Post, TOKEN_URL);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}")));

                // Dodanie parametrów żądania
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", _context.Users.FirstOrDefault(i=>i.Id==Session.User.Id).refreshToken),
                    new KeyValuePair<string, string>("client_id", CLIENT_ID)
                });

                // Wysłanie żądania i odczyt odpowiedzi
                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                // Przetworzenie odpowiedzi JSON
                using (JsonDocument json = JsonDocument.Parse(responseBody))
                {
                    refreshToken = json.RootElement.GetProperty("refresh_token").GetString();
                    accessToken = json.RootElement.GetProperty("access_token").GetString();

                    // Aktualizacja tokenów w bazie danych
                    var user = _context.Users.FirstOrDefault(i => i.Id == Session.User.Id);
                    user.accessToken = accessToken;
                    user.refreshToken = refreshToken;
                    _context.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Inicjalizuje proces logowania przez Spotify OAuth
        /// </summary>
        public static void LoginWithSpotify()
        {
            // Zakresy uprawnień
            string scope = "user-read-playback-state user-modify-playback-state";

            // Budowanie URL do autoryzacji
            string authUrl = $"https://accounts.spotify.com/authorize?client_id={CLIENT_ID}" +
                             "&response_type=code" +
                             $"&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}" +
                             $"&scope={Uri.EscapeDataString(scope)}" +
                             "&show_dialog=true";

            // Otwarcie przeglądarki do logowania
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            // Uruchomienie nasłuchiwania na odpowiedź zwrotną
            StartLocalHttpListener();
        }

        /// <summary>
        /// Nasłuchuje na odpowiedź zwrotną z kodem autoryzacyjnym
        /// </summary>
        private static async void StartLocalHttpListener()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(REDIRECT_URI + "/");
            listener.Start();
            Console.WriteLine("Czekam na kod autoryzacyjny...");

            // Oczekiwanie na połączenie zwrotne
            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString["code"];

            if (code != null)
            {
                Console.WriteLine("Kod autoryzacyjny otrzymany: " + code);
                await GetAccessToken(code);
                Console.WriteLine("Dostęp uzyskany!");
            }

            // Zamknięcie odpowiedzi i nasłuchiwacza
            context.Response.StatusCode = 200;
            context.Response.Close();
            listener.Stop();
        }

        /// <summary>
        /// Wymienia kod autoryzacyjny na token dostępu
        /// </summary>
        /// <param name="code">Kod autoryzacyjny otrzymany z OAuth</param>
        private static async Task GetAccessToken(string code)
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, TOKEN_URL);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}")));

                // Parametry żądania tokenu
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", REDIRECT_URI)
                });

                // Wysłanie żądania i odczyt odpowiedzi
                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                // Przetwarzanie odpowiedzi i zapis tokenów
                using (JsonDocument json = JsonDocument.Parse(responseBody))
                {
                    refreshToken = json.RootElement.GetProperty("refresh_token").GetString();
                    accessToken = json.RootElement.GetProperty("access_token").GetString();

                    // Aktualizacja danych użytkownika w bazie
                    var user = _context.Users.FirstOrDefault(i => i.Id == Session.User.Id);
                    user.accessToken = accessToken;
                    user.refreshToken = refreshToken;
                    _context.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Wstrzymuje aktualnie odtwarzany utwór
        /// </summary>
        public async Task Pause()
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("Brak tokena dostępu. Najpierw zaloguj użytkownika.");
                return;
            }

            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Put, "https://api.spotify.com/v1/me/player/pause");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Muzyka zatrzymana.");
                }
                else
                {
                    // Próba odświeżenia tokenu w przypadku błędu
                    GetRef();
                    Pause();
                }
            }
        }

        /// <summary>
        /// Pobiera playlisty użytkownika i próbuje odtworzyć pierwszą znalezioną
        /// </summary>
        public async Task GetUsersPlaylist()
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("Brak tokena dostępu. Najpierw zaloguj użytkownika.");
            }

            using (HttpClient client = new HttpClient())
            {
                // Pobranie listy playlist użytkownika
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/playlists?limit=1");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Znaleziono piosenki");
                }
                else
                {
                    Console.WriteLine("Błąd: " + (int)response.StatusCode);
                }

                // Przetwarzanie odpowiedzi z playlistami
                string responseBody = await response.Content.ReadAsStringAsync();
                string id = "error";
                using (JsonDocument json = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = json.RootElement;
                    if (root.TryGetProperty("items", out JsonElement items) && items.GetArrayLength() > 0)
                    {
                        id = items[0].GetProperty("id").GetString();
                        Console.WriteLine($"ID: {id}");
                    }
                }

                // Próba uruchomienia odtwarzania playlisty
                var request2 = new HttpRequestMessage(HttpMethod.Put, "https://api.spotify.com/v1/me/player/play");
                request2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var body = new
                {
                    context_uri = "spotify:playlist:" + id
                };

                request2.Content = JsonContent.Create(body);

                HttpResponseMessage response2 = await client.SendAsync(request);
                string responseBody2 = await response.Content.ReadAsStringAsync();

                if (response2.IsSuccessStatusCode)
                {
                    Console.WriteLine("Playback started successfully.");
                }
                else
                {
                    Console.WriteLine($"Error: {responseBody2}");
                }
            }
        }

       

        /// <summary>
        /// Wznawia lub rozpoczyna odtwarzanie
        /// </summary>
        public async Task StartPlay()
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Put, "https://api.spotify.com/v1/me/player/play");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Playback started successfully.");
                }
                else
                {
                    GetRef();
                    StartPlay();
                    Console.WriteLine($"Error: {responseBody}");
                }
            }
        }

        /// <summary>
        /// Przechodzi do następnego utworu
        /// </summary>
        public async Task SkipToNext()
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.spotify.com/v1/me/player/next");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Playback started successfully.");
                }
                else
                {
                    GetRef();
                    SkipToNext();
                    Console.WriteLine($"Error: {responseBody}");
                }
            }
        }

        /// <summary>
        /// Przechodzi do poprzedniego utworu
        /// </summary>
        public async Task SkipToPrevious()
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.spotify.com/v1/me/player/previous");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Playback started successfully.");
                }
                else
                {
                    GetRef();
                    SkipToPrevious();
                    Console.WriteLine($"Error: {responseBody}");
                }
            }
        }

        /// <summary>
        /// Ustawia poziom głośności
        /// </summary>
        /// <param name="volume">Poziom głośności (0-100)</param>
        public async Task SetVolume(int volume)
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Put,
                    $"https://api.spotify.com/v1/me/player/volume?volume_percent={volume}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Playback started successfully.");
                }
                else
                {
                    GetRef();
                    SetVolume(volume);
                    Console.WriteLine($"Error: {responseBody}");
                }
            }
        }
    }
}