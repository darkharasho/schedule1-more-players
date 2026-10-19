# More Players

Raises Schedule I's co-op lobby cap from 4 to **8** players. The cap is configurable from 1 to 250.

**Everyone in the lobby needs this mod**, not just the host. Without it, the game breaks for a player once a lobby has more than 4 members.

## Config

`UserData/MelonPreferences.cfg`, section `[MorePlayers]`

| Setting | Default | Notes |
| --- | --- | --- |
| `MaxPlayers` | 8 | Includes the host. Changing it while hosting updates the current lobby. |

## What it changes

- The Steam lobby is created with the configured size, and Invite stays available until it's full.
- The game's lobby member list grows to fit, so a 5th player no longer breaks it.
- The Steam transport accepts as many connections as the cap.
- The lobby panel in the top-right gets more player slots. Past 4 they wrap into rows, and past 14 players the slots shrink.

## Notes

- For the default IL2CPP version of the game, not the Mono beta branch.
- Lowering the cap below the current player count doesn't kick anyone; it only stops new invites and joins.
- Not yet tested with more than 4 real players. Please report issues on GitHub.
