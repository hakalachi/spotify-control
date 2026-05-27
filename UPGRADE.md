# Upgrading spotify-control

This page is for streamers who already have an earlier version of `spotify-control` installed and want to move to the latest release. If you're installing for the first time, start with the [README](readme.md) instead.

For the full list of changes per version, see [CHANGELOG.md](CHANGELOG.md).

---

## v1.0.0 → v1.1.0

**What's new:** three extra chat commands — `!404skip`, `!404queue`, `!404recent` — and the existing `!404sr <query>` now tells viewers their track's position in the queue.

**The short version:** it's the same as a fresh install (re-run the auth helper, re-import `install.sb`, fire `Spotify - Set Credentials`). The only upgrade-specific wrinkles are (1) you have to re-auth instead of reusing the old token, and (2) you should delete the old action first so the import doesn't leave you with duplicates.

**Estimated time:** ~3 minutes.

### Why you need to re-authorize

`!404recent` needs a Spotify permission your existing refresh token doesn't have: `user-read-recently-played`. Spotify binds refresh tokens to the permissions granted at the moment you clicked *Agree* — the only way to add a permission is to authorize the app again. Your v1.0.0 `!404sr` command will keep working until you do this, but `!404recent` will reply with "Spotify denied" errors.

### Steps

1. **Delete the old action.** In Streamer.bot's **Actions** tab, right-click `Spotify - Song Command` → **Delete**. (If `Spotify - Set Credentials` is still around from your original install, delete it too.) This prevents duplicate actions after the import.
2. **Re-run the auth helper.** Open https://hakalachi.github.io/spotify-control/, paste the **same Client ID** you used originally, click **Authorize with Spotify**, and click *Agree* on the consent screen — note the newly listed permission. Copy the new **Refresh Token** that appears.
3. **Re-install the bundle.** Follow [README → Step 3](readme.md#step-3--install-in-streamerbot) exactly as a new user would, using the refresh token from step 2.
4. **Verify in chat.** Try `!404recent` — it should list your last 5 tracks. If it does, the new scope was granted correctly and the rest of the new commands will work too.

> Your **Client ID is unchanged** — you're not creating a new Spotify app, just re-authorizing the existing one. Only the refresh token rotates.

### If something goes wrong

| Symptom | Fix |
| --- | --- |
| `!404recent` says "Spotify denied" | The new refresh token wasn't stored. Re-do step 3 — make sure you fired `Spotify - Set Credentials` and saw `[Spotify] credentials saved` in the logs. |
| Two `Spotify - Song Command` actions in the Actions list | You skipped step 1. Delete the older one (the v1.1.0 version's C# has a `TryGetQueuePosition` method — that's the keeper). |
| All commands silently fail | Same diagnostic path as a fresh install — see the [README troubleshooting table](readme.md#if-something-doesnt-work). |

---

## Future upgrades

When the next version ships, this file will get a new section at the top covering that migration. Most releases will be **drop in the new `install.sb` and re-fire `Spotify - Set Credentials`** — re-auth is only required when scopes change, and the section for that version will say so.
