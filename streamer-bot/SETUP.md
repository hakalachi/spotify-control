# Streamer.bot setup — manual / fallback

> Most streamers should use the one-click `install.sb` import described in the project README. Use this document if:
>
> - You're the **maintainer** producing `install.sb` for the first time, or re-producing it after editing `song-command.cs`.
> - The `install.sb` import failed (e.g. your Streamer.bot version is incompatible) and you need to rebuild the actions by hand.

You'll create **five** actions:

1. **`Spotify - Song Command`** — search + queue (and now-playing when no args). Triggered by `!404sr` in chat.
2. **`Spotify - Skip Command`** — skip to next track. Triggered by `!404skip`.
3. **`Spotify - Queue Command`** — show the next 5 queued tracks. Triggered by `!404queue`.
4. **`Spotify - Recent Command`** — show the last 5 played tracks. Triggered by `!404recent`.
5. **`Spotify - Set Credentials`** — a helper action you run once to save your Client ID and Refresh Token to persisted globals. Can be deleted afterward.

## Action 1 — `Spotify - Song Command`

1. **Actions** tab → right-click → **Add** → name it `Spotify - Song Command`.
2. Inside the action: right-click the Sub-Actions panel → **Add** → **Core** → **C#** → **Execute C# Code**.
3. Paste the entire contents of `song-command.cs` into the editor.
4. Click **Compile**. You should see "Compiled successfully" with no errors. Save.
5. **Triggers** panel for this action → right-click → **Add** → **Twitch** → **Command** → **Command Triggered**.
6. Add a new command:
   - **Name:** `404sr`
   - **Command:** `!404sr`
   - **Permitted users:** Everyone (tighten if you want to restrict)
   - **Case sensitive:** off
   - Make sure **Use Arguments** is checked so `rawInput` is populated.
7. Save.

## Actions 2–4 — same pattern as Action 1

Repeat the Action 1 steps for each of these, swapping in the file and trigger command. None of them need `Use Arguments` (only the Song Command parses chat input).

| Action name | Paste from | Trigger command | Permitted users (suggestion) |
| --- | --- | --- | --- |
| `Spotify - Skip Command` | `skip-command.cs` | `!404skip` | Mods/VIPs (so viewers can't grief) |
| `Spotify - Queue Command` | `queue-command.cs` | `!404queue` | Everyone |
| `Spotify - Recent Command` | `recent-command.cs` | `!404recent` | Everyone |

> The "Permitted users" column is a suggestion based on what the command does to your account. You can always set everything to Everyone and tighten later.

## Action 5 — `Spotify - Set Credentials`

This action exists only to write two persisted globals (`spotify.clientId`, `spotify.refreshToken`) so the main action can read them. Run it once and you can delete it.

1. **Actions** tab → right-click → **Add** → name it `Spotify - Set Credentials`.
2. Add a sub-action: **Core** → **C#** → **Execute C# Code**. Paste this:

   ```csharp
   public class CPHInline
   {
       const string CLIENT_ID     = "PASTE_CLIENT_ID";
       const string REFRESH_TOKEN = "PASTE_REFRESH_TOKEN";

       public bool Execute()
       {
           if (CLIENT_ID.StartsWith("PASTE_") || REFRESH_TOKEN.StartsWith("PASTE_"))
           {
               CPH.LogError("[Spotify] credentials not set — edit the constants in this action first.");
               return false;
           }
           CPH.SetGlobalVar("spotify.clientId",     CLIENT_ID,     true);
           CPH.SetGlobalVar("spotify.refreshToken", REFRESH_TOKEN, true);
           CPH.UnsetGlobalVar("spotify.accessToken", true);
           CPH.UnsetGlobalVar("spotify.expiresAt",  true);
           CPH.LogInfo("[Spotify] credentials saved.");
           return true;
       }
   }
   ```
3. Compile and save. **Don't add a trigger** — this action is only run via Test Trigger.

## Producing the install.sb export

Once all five actions exist and are working:

1. In the Actions list, hold **Ctrl** and click each of the five actions to select them all (`Spotify - Song Command`, `Spotify - Skip Command`, `Spotify - Queue Command`, `Spotify - Recent Command`, `Spotify - Set Credentials`).
2. Right-click → **Export** → **Selected Actions**.
3. Streamer.bot copies a long base64 string to your clipboard.
4. Paste it into `install.sb` at the repo root, replacing all existing content. Commit.
5. Tag the release: `git tag v1.1.0 && git push --tags`, then create a GitHub Release pointing at the tag with the relevant section of `CHANGELOG.md` as the notes.

## Notes on the globals

| Global | Type | Set by | Why persisted |
| --- | --- | --- | --- |
| `spotify.clientId` | string | `Spotify - Set Credentials` | Streamer's Spotify app ID. |
| `spotify.refreshToken` | string | `Spotify - Set Credentials` (and rotated by the main action) | Long-lived auth credential. |
| `spotify.accessToken` | string | The main action when it refreshes | Short-lived (1 hour). Cached to avoid a token refresh on every chat command. |
| `spotify.expiresAt` | long (unix seconds) | The main action | Lets us know whether the cached access token is still valid. |

All four are stored as **persisted** globals so they survive a Streamer.bot restart. The file lives at `%AppData%/StreamerBot/data/global.json` — treat it like a secret.
