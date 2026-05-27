# Changelog

All notable changes to this project are documented here. Versions follow [SemVer](https://semver.org/).

## [1.1.0] — 2026-05-26

### Added
- `!404skip` — skip to the next track (`POST /me/player/next`). Premium-gated by Spotify.
- `!404queue` — show the next 5 upcoming tracks (`GET /me/player/queue`).
- `!404recent` — show the last 5 played tracks (`GET /me/player/recently-played`).
- Queue-position blurb on `!404sr <query>`: messages now read `@user added Artist - Song to Spotify queue at #N`. The position is fetched via a best-effort `GET /me/player/queue` after the queue write; if that call fails, the message falls back to the previous format without `#N` rather than failing the command.

### Changed
- Auth helper (`index.html`) now requests one additional OAuth scope: `user-read-recently-played` (for `!404recent`).
- `streamer-bot/SETUP.md` updated for five actions (was two). Each new command has its own self-contained C# action — Streamer.bot sandboxes per-action, so the token-refresh helper is duplicated across files.

### Removed during pre-release
- `!404like` (save current track to Liked Songs) was prototyped but dropped before shipping. Spotify's `PUT /me/tracks` endpoint returns 403 from apps in Development Mode (the default for individual-owned dashboard apps) even when `user-library-modify` is correctly granted and the request matches Spotify's documented JSON-body format. Per Spotify's [Quota Modes](https://developer.spotify.com/documentation/web-api/concepts/quota-modes) docs as of May 2025, the only escape is Extended Quota Mode, which is restricted to organizations — effectively unavailable to individual developers. The `user-library-modify` scope is no longer requested by the auth helper.

### Upgrade notes
- **Existing v1.0.0 users must re-run the auth helper.** Refresh tokens are bound to the scopes granted at consent time; the v1.0.0 token does not carry the new scope. The v1.0.0 commands (`!404sr`) keep working without re-auth — only `!404recent` will return "Spotify denied" errors until you re-link.
- The maintainer must re-export `install.sb` to include the three new actions (see `streamer-bot/SETUP.md` → *Producing the install.sb export*).

## [1.0.0] — 2026-05-13

### Added
- Initial release.
- `!404sr <query>` — search Spotify and queue the top match.
- `!404sr` — show the currently playing track.
- PKCE OAuth helper (`index.html`) served via GitHub Pages — no client secret needed.
- One-click `install.sb` import bundling `Spotify - Song Command` + `Spotify - Set Credentials` actions.
