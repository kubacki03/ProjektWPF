using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using ProjektWPF.Data;

namespace ProjektWPF.Services
{
    class SpotifyService
    {
        private static readonly string CLIENT_ID = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
        private static readonly string CLIENT_SECRET = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
        private static readonly string REDIRECT_URI = "http://127.0.0.1:8080/callback";
        private static readonly string TOKEN_URL = "https://accounts.spotify.com/api/token";

        private static string accessToken;
        private static string refreshToken;

        private static readonly AppDbContext _context = new AppDbContext();

        private static async Task GetRef()
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, TOKEN_URL);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}")));

                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", _context.Users.FirstOrDefault(i=>i.Id==Session.User.Id).refreshToken),
                    new KeyValuePair<string, string>("client_id", CLIENT_ID)
                });

                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                using (JsonDocument json = JsonDocument.Parse(responseBody))
                {
                    refreshToken = json.RootElement.GetProperty("refresh_token").GetString();
                    accessToken = json.RootElement.GetProperty("access_token").GetString();

                    var user = _context.Users.FirstOrDefault(i => i.Id == Session.User.Id);
                    user.accessToken = accessToken;
                    user.refreshToken = refreshToken;
                    _context.SaveChanges();
                }
            }
        }

        public static void LoginWithSpotify()
        {
            string scope = "user-read-playback-state user-modify-playback-state";

            string authUrl = $"https://accounts.spotify.com/authorize?client_id={CLIENT_ID}" +
                             "&response_type=code" +
                             $"&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}" +
                             $"&scope={Uri.EscapeDataString(scope)}" +
                             "&show_dialog=true";

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            StartLocalHttpListener();
        }

        private static async void StartLocalHttpListener()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(REDIRECT_URI + "/");
            listener.Start();
            Console.WriteLine("Czekam na kod autoryzacyjny...");

            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString["code"];

            if (code != null)
            {
                Console.WriteLine("Kod autoryzacyjny otrzymany: " + code);
                await GetAccessToken(code);
                Console.WriteLine("Dostęp uzyskany!");
            }

            context.Response.StatusCode = 200;
            context.Response.Close();
            listener.Stop();
        }

        private static async Task GetAccessToken(string code)
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, TOKEN_URL);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}")));

                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", REDIRECT_URI)
                });

                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                using (JsonDocument json = JsonDocument.Parse(responseBody))
                {
                    refreshToken = json.RootElement.GetProperty("refresh_token").GetString();
                    accessToken = json.RootElement.GetProperty("access_token").GetString();

                    var user = _context.Users.FirstOrDefault(i => i.Id == Session.User.Id);
                    user.accessToken = accessToken;
                    user.refreshToken = refreshToken;
                    _context.SaveChanges();
                }
            }
        }

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
                    GetRef();
                    Pause();
                }
            }
        }

        public async Task GetUsersPlaylist()
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("Brak tokena dostępu. Najpierw zaloguj użytkownika.");
            }

            using (HttpClient client = new HttpClient())
            {
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