# spotify-control

**Twitch chat controls Spotify during your stream.** Viewers type `!404sr <name>` to queue a track, or `!404sr` on its own to see what's playing.

Designed for non-technical streamers. Setup is roughly: log into Spotify's developer page, click two buttons on a website, paste two strings into Streamer.bot. About 5 minutes.

---

## Install (for streamers)

### What you need first
- **Spotify Premium** on the account that plays music during your stream. Spotify's queue API doesn't work without it.
- **Streamer.bot** running and connected to your Twitch chat.
- A free Spotify Developer account (no credit card needed — you already have one if you have any Spotify account).

### Step 1 — Make a Spotify app (one time, ~2 minutes)

1. Go to **[developer.spotify.com/dashboard](https://developer.spotify.com/dashboard)** and log in with your normal Spotify account.
2. Click **Create app**. Fill in:
   - **App name:** anything (e.g. `stream music`)
   - **App description:** anything (e.g. `chat queue`)
   - **Redirect URI:** paste exactly this URL, then click **Add**:
     ```
     https://hakalachi.github.io/spotify-control/
     ```
   - **Which API/SDKs are you planning to use:** check **Web API**.
3. Agree to the terms and **Save**.
4. On the resulting app page, click **Settings**. Under *Basic Information*, copy the **Client ID** somewhere handy. You'll paste it on the next page.

### Step 2 — Link your Spotify account

1. Open this page in your browser: **https://hakalachi.github.io/spotify-control/**
2. Paste your Client ID into the box. Click **Authorize with Spotify**.
3. Spotify will ask you to log in and approve the app. Click *Agree*.
4. You'll come back to a page showing two strings: a **Client ID** and a **Refresh Token**. Keep this tab open — you'll copy from it in the next step.

### Step 3 — Install in Streamer.bot

1. Download **[install.sb](https://raw.githubusercontent.com/hakalachi/spotify-control/main/install.sb)** from this repo (right-click the link → *Save link as…*).
2. In Streamer.bot, click the **Import** button at the top. Drag `install.sb` into the dialog (or open it in Notepad and paste the contents). Click **Import** to confirm.
3. You'll now have two new actions: `Spotify - Song Command` and `Spotify - Set Credentials`, plus the bound `!404sr` chat command. (Want a different chat command? Open the **Commands** tab and rename it — works the same.)
4. Open `Spotify - Set Credentials` and double-click the C# sub-action. Near the top you'll see two lines:
   ```csharp
   const string CLIENT_ID     = "PASTE_CLIENT_ID";
   const string REFRESH_TOKEN = "PASTE_REFRESH_TOKEN";
   ```
   Replace the placeholders with the values from the auth page (Step 2). Keep the quotes.
5. Click **Compile**, then close the editor.
6. Right-click the `Spotify - Set Credentials` action → **Test Trigger**. Watch the *Logs* tab — you should see `[Spotify] credentials saved`.
7. (Optional but recommended) Right-click `Spotify - Set Credentials` → **Delete**. You don't need it after this — the credentials are now stored.

### Step 4 — Try it from chat

1. **Start playing something on Spotify** (any device — phone, desktop, web). The queue API only works when something is already playing.
2. In your Twitch chat:
   - Type `!404sr` → bot replies with the currently playing track.
   - Type `!404sr bohemian rhapsody` → bot queues the top match and replies with the track name.

That's it. From now on, viewers can do the same.

---

## If something doesn't work

| The bot says... | Try this |
| --- | --- |
| "Spotify isn't linked yet" | Credentials didn't save. Re-do Step 3 (steps 4–6). |
| "Spotify auth failed" | The refresh token is bad or was revoked. Re-run Step 2 to generate a new one, then re-do Step 3. |
| "Spotify isn't playing on any device" | Open Spotify and hit play first. The API needs an active device. |
| "Spotify Premium is required" | The streaming Spotify account needs Premium. No workaround. |
| Nothing at all | (1) Confirm Streamer.bot is connected to Twitch. (2) Open the action's **Run History** — did the trigger fire? (3) Check the **Logs** tab for any `[Spotify]` lines. |

To **revoke access** (e.g. you're done with this), go to your Spotify account → *Apps* → find the app you created → *Remove Access*. The token stops working immediately.

---

<details>
<summary><strong>For maintainers / forkers</strong></summary>

This repo is published as `hakalachi/spotify-control` with the auth helper at <https://hakalachi.github.io/spotify-control/>.

### Forking

If you fork and want to publish your own copy:

1. Find-and-replace `hakalachi/spotify-control` and `hakalachi.github.io/spotify-control` throughout this README with your own `<user>/<repo>` and `<user>.github.io/<repo>`. Commit.
2. Repo **Settings** → **Pages** → *Source: Deploy from a branch* → *Branch: `main` / root* → **Save**. Wait ~1 min for the deploy.

### Re-generating `install.sb`

If you edit `streamer-bot/song-command.cs`, the bundled import file needs re-exporting or it'll silently drift from the canonical source:

1. Re-create or re-import the actions in your own Streamer.bot following `streamer-bot/SETUP.md`.
2. Multi-select both actions, right-click → **Export** → **Export to File**.
3. Save as `install.sb` at the repo root, overwriting the existing file. Commit.

> ⚠️ **Don't re-export after filling in real credentials** in `Spotify - Set Credentials`. The export captures the C# code verbatim — including any real Client ID / Refresh Token you put in the constants. Always export with the `PASTE_CLIENT_ID` / `PASTE_REFRESH_TOKEN` placeholders intact, or delete the action before exporting.

</details>
