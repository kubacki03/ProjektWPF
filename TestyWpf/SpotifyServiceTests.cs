using System.Net;
using System.Text;
using ProjektWPF.Services;

namespace TestyWpf
{
    public class SpotifyServiceTests
    {
        private sealed class FakeTokenStore : ISpotifyTokenStore
        {
            public SpotifyTokens Tokens { get; private set; }

            public FakeTokenStore(string? access, string? refresh) => Tokens = new SpotifyTokens(access, refresh);

            public Task<SpotifyTokens> LoadAsync() => Task.FromResult(Tokens);

            public Task SaveAsync(SpotifyTokens tokens)
            {
                Tokens = tokens;
                return Task.CompletedTask;
            }
        }

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

            public List<(HttpMethod Method, string Url, string? Auth)> Calls { get; } = new();

            public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls.Add((request.Method, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString()));
                return Task.FromResult(_respond(request));
            }
        }

        private static HttpResponseMessage Json(HttpStatusCode code, string body = "") =>
            new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        private static bool IsTokenEndpoint(HttpRequestMessage request) =>
            request.RequestUri!.Host == "accounts.spotify.com";

        private static SpotifyService Create(FakeHandler handler, FakeTokenStore store) =>
            new(new HttpClient(handler), store, "id", "secret");

        [Test]
        public async Task Pause_SendsPutWithBearerToken()
        {
            var handler = new FakeHandler(_ => Json(HttpStatusCode.NoContent));
            var spotify = Create(handler, new FakeTokenStore("access-1", "refresh-1"));

            await spotify.Pause();

            Assert.That(handler.Calls, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(handler.Calls[0].Method, Is.EqualTo(HttpMethod.Put));
                Assert.That(handler.Calls[0].Url, Does.EndWith("/me/player/pause"));
                Assert.That(handler.Calls[0].Auth, Is.EqualTo("Bearer access-1"));
            });
        }

        [Test]
        public async Task SetVolume_ClampsValueToValidRange()
        {
            var handler = new FakeHandler(_ => Json(HttpStatusCode.NoContent));
            var spotify = Create(handler, new FakeTokenStore("a", "r"));

            await spotify.SetVolume(250);
            await spotify.SetVolume(-5);

            Assert.Multiple(() =>
            {
                Assert.That(handler.Calls[0].Url, Does.EndWith("volume_percent=100"));
                Assert.That(handler.Calls[1].Url, Does.EndWith("volume_percent=0"));
            });
        }

        [Test]
        public async Task Unauthorized_RefreshesTokenAndRetriesOnce()
        {
            var handler = new FakeHandler(request =>
            {
                if (IsTokenEndpoint(request))
                {
                    return Json(HttpStatusCode.OK, """{"access_token":"access-2","refresh_token":"refresh-2"}""");
                }

                return request.Headers.Authorization!.Parameter == "access-2"
                    ? Json(HttpStatusCode.NoContent)
                    : Json(HttpStatusCode.Unauthorized);
            });
            var store = new FakeTokenStore("access-1", "refresh-1");

            await Create(handler, store).SkipToNext();

            Assert.Multiple(() =>
            {
                Assert.That(handler.Calls.Count(c => c.Url.Contains("/me/player/next")), Is.EqualTo(2));
                Assert.That(store.Tokens, Is.EqualTo(new SpotifyTokens("access-2", "refresh-2")));
            });
        }

        [Test]
        public async Task Refresh_WithoutNewRefreshToken_KeepsOldOne()
        {
            var handler = new FakeHandler(request =>
            {
                if (IsTokenEndpoint(request))
                {
                    return Json(HttpStatusCode.OK, """{"access_token":"access-2"}""");
                }

                return request.Headers.Authorization!.Parameter == "access-2"
                    ? Json(HttpStatusCode.NoContent)
                    : Json(HttpStatusCode.Unauthorized);
            });
            var store = new FakeTokenStore("access-1", "refresh-1");

            await Create(handler, store).StartPlay();

            Assert.That(store.Tokens, Is.EqualTo(new SpotifyTokens("access-2", "refresh-1")));
        }

        [Test]
        public void Unauthorized_AfterRefresh_ThrowsInsteadOfLooping()
        {
            var handler = new FakeHandler(request =>
                IsTokenEndpoint(request)
                    ? Json(HttpStatusCode.OK, """{"access_token":"access-2"}""")
                    : Json(HttpStatusCode.Unauthorized));
            var spotify = Create(handler, new FakeTokenStore("access-1", "refresh-1"));

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => spotify.Pause());

            Assert.Multiple(() =>
            {
                Assert.That(ex!.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(handler.Calls.Count(c => c.Url.Contains("/me/player/pause")), Is.EqualTo(2));
            });
        }

        [Test]
        public void NoTokens_ThrowsAndSendsNothing()
        {
            var handler = new FakeHandler(_ => Json(HttpStatusCode.NoContent));
            var spotify = Create(handler, new FakeTokenStore(null, null));

            Assert.ThrowsAsync<InvalidOperationException>(() => spotify.Pause());
            Assert.That(handler.Calls, Is.Empty);
        }

        [Test]
        public async Task OnlyRefreshToken_ObtainsAccessTokenBeforeFirstCall()
        {
            var handler = new FakeHandler(request =>
                IsTokenEndpoint(request)
                    ? Json(HttpStatusCode.OK, """{"access_token":"fresh"}""")
                    : Json(HttpStatusCode.NoContent));

            await Create(handler, new FakeTokenStore(null, "refresh-1")).Pause();

            Assert.That(handler.Calls.Last().Auth, Is.EqualTo("Bearer fresh"));
        }

        [Test]
        public void ApiError_IncludesStatusAndBodyInException()
        {
            var handler = new FakeHandler(_ => Json(HttpStatusCode.NotFound, """{"error":"NO_ACTIVE_DEVICE"}"""));
            var spotify = Create(handler, new FakeTokenStore("a", "r"));

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => spotify.Pause());

            Assert.That(ex!.Message, Does.Contain("404").And.Contain("NO_ACTIVE_DEVICE"));
        }

        [Test]
        public async Task PlayFirstPlaylist_FetchesPlaylistThenStartsPlayback()
        {
            var handler = new FakeHandler(request =>
                request.Method == HttpMethod.Get
                    ? Json(HttpStatusCode.OK, """{"items":[{"id":"abc123"}]}""")
                    : Json(HttpStatusCode.NoContent));

            await Create(handler, new FakeTokenStore("a", "r")).PlayFirstPlaylist();

            Assert.That(handler.Calls, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(handler.Calls[1].Method, Is.EqualTo(HttpMethod.Put));
                Assert.That(handler.Calls[1].Url, Does.EndWith("/me/player/play"));
            });
        }

        [Test]
        public void PlayFirstPlaylist_NoPlaylists_Throws()
        {
            var handler = new FakeHandler(_ => Json(HttpStatusCode.OK, """{"items":[]}"""));
            var spotify = Create(handler, new FakeTokenStore("a", "r"));

            Assert.ThrowsAsync<InvalidOperationException>(() => spotify.PlayFirstPlaylist());
        }
    }
}
