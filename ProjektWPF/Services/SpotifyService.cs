using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjektWPF.Data;

namespace ProjektWPF.Services
{
    public record SpotifyTokens(string? AccessToken, string? RefreshToken);

    public interface ISpotifyTokenStore
    {
        Task<SpotifyTokens> LoadAsync();
        Task SaveAsync(SpotifyTokens tokens);
    }

    /// <summary>Tokeny Spotify zalogowanego użytkownika trzymane w bazie danych.</summary>
    public class DbSpotifyTokenStore : ISpotifyTokenStore
    {
        public async Task<SpotifyTokens> LoadAsync()
        {
            await using var context = new AppDbContext();
            var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == Session.User.Id);
            return new SpotifyTokens(user?.accessToken, user?.refreshToken);
        }

        public async Task SaveAsync(SpotifyTokens tokens)
        {
            await using var context = new AppDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == Session.User.Id);
            if (user == null)
            {
                return;
            }

            user.accessToken = tokens.AccessToken;
            user.refreshToken = tokens.RefreshToken;
            await context.SaveChangesAsync();
        }
    }

    public class SpotifyService
    {
        private const string RedirectUri = "http://127.0.0.1:8080/callback";
        private const string TokenUrl = "https://accounts.spotify.com/api/token";
        private const string ApiUrl = "https://api.spotify.com/v1";
        private const string Scope = "user-read-playback-state user-modify-playback-state";

        private static readonly HttpClient SharedClient = new HttpClient();

        private readonly HttpClient _http;
        private readonly ISpotifyTokenStore _tokens;
        private readonly string? _clientId;
        private readonly string? _clientSecret;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        private SpotifyTokens? _cached;

        public SpotifyService()
            : this(SharedClient, new DbSpotifyTokenStore(),
                   Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID"),
                   Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET"))
        {
        }

        public SpotifyService(HttpClient http, ISpotifyTokenStore tokens, string? clientId, string? clientSecret)
        {
            _http = http;
            _tokens = tokens;
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        // ---------- Logowanie (Authorization Code Flow) ----------

        public async Task LoginAsync(CancellationToken cancellationToken = default)
        {
            RequireCredentials();

            string state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            string authUrl = "https://accounts.spotify.com/authorize" +
                             $"?client_id={Uri.EscapeDataString(_clientId!)}" +
                             "&response_type=code" +
                             $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                             $"&scope={Uri.EscapeDataString(Scope)}" +
                             $"&state={state}" +
                             "&show_dialog=true";

            using var listener = new HttpListener();
            listener.Prefixes.Add(RedirectUri + "/");
            listener.Start();

            try
            {
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

                using var registration = cancellationToken.Register(listener.Stop);
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (Exception) when (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                string? code = context.Request.QueryString["code"];
                bool stateOk = context.Request.QueryString["state"] == state;

                await RespondAsync(context, code != null && stateOk
                    ? "Zalogowano do Spotify. Możesz zamknąć to okno."
                    : "Logowanie do Spotify nie powiodło się.");

                if (code == null || !stateOk)
                {
                    throw new InvalidOperationException("Spotify nie zwróciło poprawnego kodu autoryzacyjnego.");
                }

                await ExchangeCodeAsync(code, cancellationToken);
            }
            finally
            {
                listener.Close();
            }
        }

        private static async Task RespondAsync(HttpListenerContext context, string message)
        {
            byte[] body = Encoding.UTF8.GetBytes($"<html><meta charset=\"utf-8\"><body>{message}</body></html>");
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = body.Length;
            await context.Response.OutputStream.WriteAsync(body);
            context.Response.Close();
        }

        private async Task ExchangeCodeAsync(string code, CancellationToken cancellationToken)
        {
            var tokens = await RequestTokensAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = RedirectUri
            }, previousRefreshToken: null, cancellationToken);

            await StoreAsync(tokens);
        }

        // ---------- Komendy odtwarzacza ----------

        public Task Pause() =>
            SendAsync(() => new HttpRequestMessage(HttpMethod.Put, $"{ApiUrl}/me/player/pause"));

        public Task StartPlay() =>
            SendAsync(() => new HttpRequestMessage(HttpMethod.Put, $"{ApiUrl}/me/player/play"));

        public Task SkipToNext() =>
            SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"{ApiUrl}/me/player/next"));

        public Task SkipToPrevious() =>
            SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"{ApiUrl}/me/player/previous"));

        public Task SetVolume(int volume)
        {
            int percent = Math.Clamp(volume, 0, 100);
            return SendAsync(() => new HttpRequestMessage(HttpMethod.Put,
                $"{ApiUrl}/me/player/volume?volume_percent={percent}"));
        }

        /// <summary>Włącza pierwszą playlistę użytkownika.</summary>
        public async Task PlayFirstPlaylist()
        {
            string json = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, $"{ApiUrl}/me/playlists?limit=1"));

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Nie znaleziono żadnej playlisty.");
            }

            string playlistId = items[0].GetProperty("id").GetString()!;
            await SendAsync(() => new HttpRequestMessage(HttpMethod.Put, $"{ApiUrl}/me/player/play")
            {
                Content = JsonContent.Create(new { context_uri = "spotify:playlist:" + playlistId })
            });
        }

        // ---------- Wspólna obsługa żądań ----------

        /// <summary>
        /// Wysyła żądanie z tokenem dostępu. Przy 401 odświeża token i ponawia żądanie dokładnie raz.
        /// Fabryka jest potrzebna, bo HttpRequestMessage nie można wysłać dwa razy.
        /// </summary>
        private async Task<string> SendAsync(Func<HttpRequestMessage> createRequest)
        {
            var tokens = await GetTokensAsync();
            if (string.IsNullOrEmpty(tokens.AccessToken))
            {
                if (string.IsNullOrEmpty(tokens.RefreshToken))
                {
                    throw new InvalidOperationException("Brak tokena dostępu. Najpierw zaloguj się do Spotify.");
                }

                tokens = await RefreshAsync(tokens);
            }

            using var response = await SendWithTokenAsync(createRequest, tokens.AccessToken!);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                tokens = await RefreshAsync(tokens);
                using var retry = await SendWithTokenAsync(createRequest, tokens.AccessToken!);
                return await ReadResultAsync(retry);
            }

            return await ReadResultAsync(response);
        }

        private async Task<HttpResponseMessage> SendWithTokenAsync(Func<HttpRequestMessage> createRequest, string accessToken)
        {
            using var request = createRequest();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return await _http.SendAsync(request);
        }

        private static async Task<string> ReadResultAsync(HttpResponseMessage response)
        {
            string body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Spotify zwróciło błąd {(int)response.StatusCode}: {body}", null, response.StatusCode);
            }

            return body;
        }

        // ---------- Tokeny ----------

        private async Task<SpotifyTokens> GetTokensAsync() => _cached ??= await _tokens.LoadAsync();

        private async Task StoreAsync(SpotifyTokens tokens)
        {
            _cached = tokens;
            await _tokens.SaveAsync(tokens);
        }

        private async Task<SpotifyTokens> RefreshAsync(SpotifyTokens stale)
        {
            await _refreshLock.WaitAsync();
            try
            {
                // Inne równoległe żądanie mogło już odświeżyć token.
                if (_cached != null && _cached.AccessToken != stale.AccessToken && !string.IsNullOrEmpty(_cached.AccessToken))
                {
                    return _cached;
                }

                if (string.IsNullOrEmpty(stale.RefreshToken))
                {
                    throw new InvalidOperationException("Sesja Spotify wygasła. Zaloguj się ponownie.");
                }

                var refreshed = await RequestTokensAsync(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = stale.RefreshToken
                }, stale.RefreshToken, CancellationToken.None);

                await StoreAsync(refreshed);
                return refreshed;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<SpotifyTokens> RequestTokensAsync(
            Dictionary<string, string> form, string? previousRefreshToken, CancellationToken cancellationToken)
        {
            RequireCredentials();

            using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
            {
                Content = new FormUrlEncodedContent(form)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}")));

            using var response = await _http.SendAsync(request, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Nie udało się pobrać tokena Spotify ({(int)response.StatusCode}): {body}", null, response.StatusCode);
            }

            using var json = JsonDocument.Parse(body);
            string accessToken = json.RootElement.GetProperty("access_token").GetString()!;

            // Przy odświeżaniu Spotify nie musi zwracać nowego refresh tokena – wtedy zostaje stary.
            string? refreshToken = json.RootElement.TryGetProperty("refresh_token", out var rt)
                ? rt.GetString()
                : previousRefreshToken;

            return new SpotifyTokens(accessToken, refreshToken);
        }

        private void RequireCredentials()
        {
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                throw new InvalidOperationException(
                    "Brak zmiennych środowiskowych SPOTIFY_CLIENT_ID / SPOTIFY_CLIENT_SECRET.");
            }
        }
    }
}
