## Project Context

A MelonLoader/Harmony mod for Schedule I (TVGS, Steam app 3164500, Unity 2022.3.62 IL2CPP) that raises the 4-player co-op lobby cap. The cap is the const `ScheduleOne.Networking.Lobby.PlayerLimit`, compiled into each use, so the mod patches the uses: `SteamLobbyService.OnLobbyCreated` (raises the lobby member limit), `SteamLobbyService.UpdateLobbyMembers` (grows the `_players` array, sized 4 and filled without a bounds check), `SteamLobbyService.OpenInviteUI` (replaced: create size and the "max capacity" check), `FishySteamworks.StartServer` (transport `_maximumClients`), and `LobbyInterface` (`Start` adds slots to the "Entries" GridLayoutGroup, `UpdateUI`/`UpdateButtons` fix the "n/4" title and the Invite button). Every player needs the mod: clients run `UpdateLobbyMembers` too. Packaged for Thunderstore as `darkharasho-ScheduleOneMorePlayers` (`darkharasho-MorePlayers` is the Stonewards mod; Thunderstore package names are per team, not per community).

## Layout

- `src/MorePlayers/Core.cs` — MelonPreferences entry and manual Harmony patching (`HarmonyDontPatchAll`)
- `src/MorePlayers/Patches/LobbyPatches.cs` — lobby service and transport patches, `PlayerCap.ApplyLive`
- `src/MorePlayers/Patches/LobbyInterfacePatches.cs` — lobby panel slots, title and Invite button
- `scripts/package.sh` builds the Thunderstore zip (`Mods/MorePlayers.dll`); `scripts/update-refs.sh` regenerates `lib/refs` for CI; `scripts/generate-interop.sh` + `tools/InteropGen` make interop assemblies without starting the game
- Release: bump the version in `Core.cs`, the csproj, `thunderstore/manifest.json` and `CHANGELOG.md`, then push tag `v<version>` (Thunderstore and GitHub), or `v<version>-<suffix>` such as `v0.1.0-beta.1` for a GitHub-only prerelease

## Notes

- Read game code: `scripts/generate-interop.sh` leaves Cpp2IL dummy DLLs (signatures only) in `build/interop-work/dummy`. For method bodies, run the same Cpp2IL with `--output-as isil` and read `IsilDump/Assembly-CSharp/ScheduleOne/...`. Decompile with `DOTNET_ROOT=/home/linuxbrew/.linuxbrew/opt/dotnet/libexec ~/.dotnet/tools/ilspycmd`.
- The scene layout (lobby panel hierarchy, GridLayoutGroup values) was read from `Schedule I_Data/level0` with UnityPy: Entries grid has cell 75x75, spacing (-150, 0), padding.left -75, start corner UpperRight, FixedRowCount 1.
- Interop namespaces carry MelonLoader's `Il2Cpp` prefix (`Il2CppScheduleOne`, `Il2CppSteamworks`, `Il2CppFishySteamworks`); Unity namespaces don't.
- r2modman profile: `~/.config/r2modmanPlus-local/ScheduleI/profiles/Co-op playthrough`. It also has `ifBars-BiggerLobbies`, an older mod that patches a `Lobby.Players` array the game no longer has; disable it alongside this one.
- Never hook tiny IL2CPP stubs (a few instructions ending in `jmp`, e.g. `SteamLobbyService.CreateLobby`, `FishySteamworks.StartConnection`): the native detour overwrites ~14 bytes and can't relocate short branches in them, which crashes the game. Check a target's disassembly in the ISIL dump first and pick a caller or callee with a normal prologue.
