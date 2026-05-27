using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const string Scopes =
        "user-modify-playback-state user-read-currently-playing user-read-playback-state " +
        "user-read-recently-played";

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

            CPH.TryGetArg("rawInput", out string rawInput);
            CPH.TryGetArg("userName", out string userName);
            rawInput = (rawInput ?? string.Empty).Trim();
            userName = string.IsNullOrEmpty(userName) ? "viewer" : userName;

            return string.IsNullOrEmpty(rawInput)
                ? ShowNowPlaying(accessToken)
                : QueueTrack(accessToken, rawInput, userName);
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

            // Spotify rotates refresh tokens periodically — persist the new one.
            var rotated = (string)j["refresh_token"];
            if (!string.IsNullOrEmpty(rotated))
                CPH.SetGlobalVar("spotify.refreshToken", rotated, true);

            return newToken;
        }
    }

    private bool ShowNowPlaying(string accessToken)
    {
        using (var http = new HttpClient())
        {
            var req = new HttpRequestMessage(HttpMethod.Get,
                "https://api.spotify.com/v1/me/player/currently-playing");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var resp = http.SendAsync(req).GetAwaiter().GetResult();

            if (resp.StatusCode == HttpStatusCode.NoContent)
            {
                CPH.SendMessage("Nothing is playing right now.");
                return true;
            }

            var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!resp.IsSuccessStatusCode)
            {
                CPH.LogError("[Spotify] now-playing failed: " + text);
                CPH.SendMessage("Couldn't read the current track.");
                return false;
            }

            var j = JObject.Parse(text);
            var item = j["item"];
            if (item == null || item.Type == JTokenType.Null)
            {
                CPH.SendMessage("Nothing is playing right now.");
                return true;
            }

            var name = (string)item["name"];
            var artists = string.Join(", ",
                item["artists"].Select(a => (string)a["name"]));
            CPH.SendMessage($"♪ Now playing: {name} — {artists}");
            return true;
        }
    }

    private bool QueueTrack(string accessToken, string query, string userName)
    {
        using (var http = new HttpClient())
        {
            var searchReq = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=1");
            searchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var searchResp = http.SendAsync(searchReq).GetAwaiter().GetResult();
            var searchText = searchResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!searchResp.IsSuccessStatusCode)
            {
                CPH.LogError("[Spotify] search failed: " + searchText);
                CPH.SendMessage("Spotify search failed.");
                return false;
            }

            var top = JObject.Parse(searchText)["tracks"]?["items"]?.FirstOrDefault();
            if (top == null)
            {
                CPH.SendMessage($"@{userName} no track found for \"{query}\".");
                return true;
            }

            var uri = (string)top["uri"];
            var name = (string)top["name"];
            var artists = string.Join(", ",
                top["artists"].Select(a => (string)a["name"]));

            var queueReq = new HttpRequestMessage(HttpMethod.Post,
                $"https://api.spotify.com/v1/me/player/queue?uri={Uri.EscapeDataString(uri)}");
            queueReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var queueResp = http.SendAsync(queueReq).GetAwaiter().GetResult();
            if (!queueResp.IsSuccessStatusCode)
            {
                var qText = queueResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                CPH.LogError("[Spotify] queue failed: " + qText);
                if (qText.IndexOf("NO_ACTIVE_DEVICE", StringComparison.OrdinalIgnoreCase) >= 0
                    || qText.IndexOf("No active device", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    CPH.SendMessage("Spotify isn't playing on any device — start playback first.");
                }
                else if (queueResp.StatusCode == HttpStatusCode.Forbidden)
                {
                    CPH.SendMessage("Spotify Premium is required to queue tracks.");
                }
                else
                {
                    CPH.SendMessage("Couldn't queue that track.");
                }
                return false;
            }

            int position = TryGetQueuePosition(http, accessToken, uri);
            if (position > 0)
                CPH.SendMessage($"@{userName} added {artists} - {name} to Spotify queue at #{position}");
            else
                CPH.SendMessage($"@{userName} added {artists} - {name} to Spotify queue");
            return true;
        }
    }

    private int TryGetQueuePosition(HttpClient http, string accessToken, string uri)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get,
                "https://api.spotify.com/v1/me/player/queue");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var resp = http.SendAsync(req).GetAwaiter().GetResult();
            if (!resp.IsSuccessStatusCode) return 0;
            var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var queue = JObject.Parse(text)["queue"] as JArray;
            if (queue == null) return 0;
            // Last occurrence — if the same URI was queued multiple times, the most recent add is ours.
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                if ((string)queue[i]["uri"] == uri) return i + 1;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }
}
