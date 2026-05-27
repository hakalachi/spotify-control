using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
                var req = new HttpRequestMessage(HttpMethod.Post,
                    "https://api.spotify.com/v1/me/player/next");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var resp = http.SendAsync(req).GetAwaiter().GetResult();

                if (resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.NoContent)
                {
                    CPH.SendMessage("⏭ Skipped to next track.");
                    return true;
                }

                var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                CPH.LogError("[Spotify] skip failed: " + text);
                if (text.IndexOf("NO_ACTIVE_DEVICE", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("No active device", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    CPH.SendMessage("Spotify isn't playing on any device — start playback first.");
                }
                else if (resp.StatusCode == HttpStatusCode.Forbidden)
                {
                    CPH.SendMessage("Spotify Premium is required to skip tracks.");
                }
                else
                {
                    CPH.SendMessage("Couldn't skip the track.");
                }
                return false;
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
