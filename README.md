# spotify-control

**Current version: v1.1.0** — see [CHANGELOG.md](CHANGELOG.md) for what's new.

**Twitch chat controls Spotify during your stream.** Viewers can queue tracks, skip, and see what's playing or what's coming next — all from chat.

Designed for non-technical streamers. Setup is roughly: log into Spotify's developer page, click two buttons on a website, paste two strings into Streamer.bot. About 5 minutes.

> **Already running v1.0.0?** Don't redo this whole walkthrough — follow [UPGRADE.md](UPGRADE.md) instead. It's a short migration that takes ~3 minutes.

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
3. Spotify will ask you to log in and approve the app. The consent screen lists the permissions the bot will use:
   - Read what's currently playing and recently played
   - Modify the playback queue and skip tracks

   Click *Agree*.
4. You'll come back to a page showing two strings: a **Client ID** and a **Refresh Token**. Keep this tab open — you'll copy from it in the next step.

### Step 3 — Install in Streamer.bot

1. Download **[install.sb](https://raw.githubusercontent.com/hakalachi/spotify-control/main/install.sb)** from this repo (right-click the link → *Save link as…*).
2. In Streamer.bot, click the **Import** button at the top. Drag `install.sb` into the dialog (or open it in Notepad and paste the contents). Click **Import** to confirm.
3. You'll now have five new actions — one per command, plus a one-time credentials helper — and all the chat commands listed in Step 4 will be bound and ready.
4. Open `Spotify - Set Credentials` and double-click the C# sub-action. Near the top you'll see two lines:
   ```csharp
   const string CLIENT_ID     = "PASTE_CLIENT_ID";
   const string REFRESH_TOKEN = "PASTE_REFRESH_TOKEN";
   ```
   Replace the placeholders with the values from the auth page (Step 2). Keep the quotes.
5. Click **Compile**, then close the editor.
6. Add a temporary trigger so you can fire this action once. With `Spotify - Set Credentials` selected, go to the **Triggers** panel on the right → right-click → **Add** → **Core** → **Hotkey**. In the dialog, click the keybind field and press a combo you won't hit by accident (e.g. **Ctrl+Shift+F12**). Click **OK** to save. (This trigger only exists so you can fire the action once — you'll delete the whole action in step 8.)
7. With Streamer.bot focused, press your hotkey. Watch the *Logs* tab — you should see `[Spotify] credentials saved`.
8. (Optional but recommended) Right-click `Spotify - Set Credentials` → **Delete**. You don't need it after this — the credentials are now stored.

### Step 4 — Try it from chat

1. **Start playing something on Spotify** (any device — phone, desktop, web). The queue and skip APIs only work when something is already playing.
2. In your Twitch chat, try any of these:

   | Command | What it does |
   | --- | --- |
   | `!404sr` | Show the currently playing track |
   | `!404sr <name>` | Queue the top search match and reply with its position in the queue |
   | `!404skip` | Skip to the next track |
   | `!404queue` | Show the next 5 tracks already in the queue |
   | `!404recent` | Show the last 5 tracks you played |

   Want different names? Open the **Commands** tab in Streamer.bot and rename — works the same.

That's it. From now on, viewers can do the same.

---

## Permissions worth thinking about

`!404skip` moves your playback forward immediately, no vote required. Consider restricting *Permitted users* in the Streamer.bot **Commands** tab (Subs, VIPs, or Mods only). The default is open; tighten as you see how chat behaves.

---

## If something doesn't work

| The bot says... | Try this |
| --- | --- |
| "Spotify isn't linked yet" | Credentials didn't save. Re-do Step 3 (steps 4–7). |
| "Spotify auth failed" | The refresh token is bad or was revoked. Re-run Step 2 to generate a new one, then re-do Step 3. |
| "Spotify isn't playing on any device" | Open Spotify and hit play first. The API needs an active device. |
| "Spotify Premium is required" | The streaming Spotify account needs Premium. No workaround. |
| "Spotify denied …" (on `!404recent`) | The refresh token is missing the recently-played scope — re-run Step 2 to re-auth. |
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

If you edit any `streamer-bot/*-command.cs` file, the bundled import file needs re-exporting or it'll silently drift from the canonical source:

1. Re-create or re-import all five actions in your own Streamer.bot following `streamer-bot/SETUP.md`.
2. Multi-select all five actions (the four command actions + `Spotify - Set Credentials`), right-click → **Export** → **Export to File**.
3. Save as `install.sb` at the repo root, overwriting the existing file. Commit.

> ⚠️ **Don't re-export after filling in real credentials** in `Spotify - Set Credentials`. The export captures the C# code verbatim — including any real Client ID / Refresh Token you put in the constants. Always export with the `PASTE_CLIENT_ID` / `PASTE_REFRESH_TOKEN` placeholders intact, or delete the action before exporting.

### Cutting a release

1. Bump the version in this README header + add a section to `CHANGELOG.md` (SemVer: additive commands = minor, breaking auth/storage changes = major).
2. If scopes changed, write or update `UPGRADE.md` so existing users know what they need to re-do.
3. Re-export `install.sb` (above).
4. `git tag vX.Y.Z && git push --tags`, then create a GitHub Release pointing at the tag with the relevant `CHANGELOG.md` section as the notes.

</details>
