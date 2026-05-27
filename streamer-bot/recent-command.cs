using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    public bool Execute()
    {
        try
        {
            var clientId = CPH.GetGlobalVar<string>("spotify.clientId", true);
            var refreshToken = CPH.GetGlobalVar<string>("spotify.refreshToken", true);
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(refreshToken))
            {
                CPH.SendMessage("Spotify isn't linked yet — the streamer needs to run the auth helper.");
                return false;
            }

            var accessToken = EnsureAccessToken(clientId, refreshToken);
            if (accessToken == null)
            {
                CPH.SendMessage("Spotify auth failed — the streamer needs to re-link Spotify.");
                return false;
            }

            using (var http = new HttpClient())
            {
                var req = new HttpRequestMessage(HttpMethod.Get,
                    "https://api.spotify.com/v1/me/player/recently-played?limit=5");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var resp = http.SendAsync(req).GetAwaiter().GetResult();

                var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode)
                {
                    CPH.LogError("[Spotify] recently-played failed: " + text);
                    if (resp.StatusCode == HttpStatusCode.Forbidden
                        || resp.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        CPH.SendMessage("Spotify denied recently-played — the streamer needs to re-link Spotify to grant access.");
                    }
                    else
                    {
                        CPH.SendMessage("Couldn't read recently played.");
                    }
                    return false;
                }

                var items = JObject.Parse(text)["items"] as JArray;
                if (items == null || items.Count == 0)
                {
                    CPH.SendMessage("No recently played tracks.");
                    return true;
                }

                var sb = new StringBuilder("⏮ Recently played: ");
                for (int i = 0; i < items.Count; i++)
                {
                    var track = items[i]["track"];
                    var name = (string)track["name"];
                    var artists = string.Join(", ",
                        track["artists"].Select(a => (string)a["name"]));
                    if (i > 0) sb.Append(" | ");
                    sb.Append((i + 1) + ") " + artists + " - " + name);
                }

                CPH.SendMessage(sb.ToString());
                return true;
            }
        }
        catch (Exception ex)
        {
            CPH.LogError("[Spotify] " + ex);
            CPH.SendMessage("Spotify hit an error — check the Streamer.bot logs.");
            return false;
        }
    }

    private string EnsureAccessToken(string clientId, string refreshToken)
    {
        long expiresAt = CPH.GetGlobalVar<long>("spotify.expiresAt", true);
        var cached = CPH.GetGlobalVar<string>("spotify.accessToken", true);
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!string.IsNullOrEmpty(cached) && expiresAt > now + 30) return cached;

        using (var http = new HttpClient())
        {
            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", clientId),
            });
            var resp = http.PostAsync("https://accounts.spotify.com/api/token", body)
                .GetAwaiter().GetResult();
            var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!resp.IsSuccessStatusCode)
            {
                CPH.LogError("[Spotify] token refresh failed: " + text);
                return null;
            }
            var j = JObject.Parse(text);
            var newToken = (string)j["access_token"];
            int ttl = (int)j["expires_in"];
            CPH.SetGlobalVar("spotify.accessToken", newToken, true);
            CPH.SetGlobalVar("spotify.expiresAt", now + ttl, true);

            var rotated = (string)j["refresh_token"];
            if (!string.IsNullOrEmpty(rotated))
                CPH.SetGlobalVar("spotify.refreshToken", rotated, true);

            return newToken;
        }
    }
}
