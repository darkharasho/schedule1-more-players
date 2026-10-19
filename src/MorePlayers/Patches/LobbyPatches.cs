using System;
using HarmonyLib;
using Il2CppFishySteamworks;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.UI.Multiplayer;
using Il2CppSteamworks;
using Object = UnityEngine.Object;

namespace MorePlayers.Patches
{
    // The game's 4-player cap is the const Lobby.PlayerLimit. IL2CPP compiled it into each use, so there is no
    // single value to change. Its uses: the Steam lobby size (Lobby.CreateLobby and TryOpenInviteInterface pass 4
    // to ILobbyService.CreateLobby), SteamLobbyService's member array (new CSteamID[4], which UpdateLobbyMembers
    // overruns and throws on once a fifth player joins), the invite check in SteamLobbyService.OpenInviteUI, and
    // the lobby panel (LobbyInterfacePatches). The FishySteamworks transport has its own client limit too.
    internal static class PlayerCap
    {
        private static bool _loggedFailure;

        // Applies a changed cap now: the current lobby's Steam member limit if we host it, the running server's
        // client limit, and the lobby panel's slots.
        internal static void ApplyLive()
        {
            try
            {
                int cap = Core.Cap;
                var lobby = Object.FindObjectOfType<Lobby>();
                var service = lobby?._lobbyService?.TryCast<SteamLobbyService>();
                if (service != null && lobby.IsHost && service._lobbyID != 0)
                    SteamMatchmaking.SetLobbyMemberLimit(service._lobbySteamID, cap);

                var transport = Object.FindObjectOfType<FishySteamworks>();
                if (transport != null && RaiseTransportLimit(transport))
                    transport.SetMaximumClients(cap);

                var ui = Object.FindObjectOfType<LobbyInterface>();
                if (ui != null)
                {
                    LobbySlots.Ensure(ui);
                    ui.UpdateUI();
                }
                Core.Log.Msg($"Max players: {cap}");
            }
            catch (Exception e) { LogFailureOnce(e); }
        }

        // FishySteamworks refuses connections past _maximumClients. Only raises it, never lowers a bigger value.
        internal static bool RaiseTransportLimit(FishySteamworks transport)
        {
            int cap = Core.Cap;
            if (transport._maximumClients >= cap) return false;
            transport._maximumClients = (ushort)cap;
            return true;
        }

        internal static void LogFailureOnce(Exception e)
        {
            if (_loggedFailure) return;
            _loggedFailure = true;
            Core.Log.Error($"Couldn't apply the player cap, likely a game update; it may stay at the game's default. {e}");
        }
    }

    // Lobby.CreateLobby and TryOpenInviteInterface both create the Steam lobby with maxPlayers = 4.
    [HarmonyPatch(typeof(SteamLobbyService), nameof(SteamLobbyService.CreateLobby))]
    internal static class CreateLobbyPatch
    {
        private static void Prefix(ref int maxPlayers) => maxPlayers = Core.Cap;
    }

    // UpdateLobbyMembers fills _players (sized 4 by the game) from Steam's member list with no length check.
    // Grow it first. Also covers a lobby bigger than our own cap, so joining one never throws.
    [HarmonyPatch(typeof(SteamLobbyService), nameof(SteamLobbyService.UpdateLobbyMembers))]
    internal static class UpdateLobbyMembersPatch
    {
        private static void Prefix(SteamLobbyService __instance)
        {
            try
            {
                int needed = Core.Cap;
                if (__instance._lobbyID != 0)
                    needed = Math.Max(needed, SteamMatchmaking.GetNumLobbyMembers(__instance._lobbySteamID));

                var players = __instance._players;
                int length = players?.Length ?? 0;
                if (length >= needed) return;

                var grown = new Il2CppStructArray<CSteamID>(needed);
                for (int i = 0; i < length; i++) grown[i] = players[i];
                __instance._players = grown;
            }
            catch (Exception e) { PlayerCap.LogFailureOnce(e); }
        }
    }

    // OpenInviteUI creates a 4-player lobby when there isn't one, and won't open Steam's invite dialog once the
    // lobby has 4 members. Same steps with the configured cap.
    [HarmonyPatch(typeof(SteamLobbyService), nameof(SteamLobbyService.OpenInviteUI))]
    internal static class OpenInviteUIPatch
    {
        private static bool Prefix(SteamLobbyService __instance)
        {
            try
            {
                int cap = Core.Cap;
                if (__instance._lobbyID == 0)
                {
                    Core.Log.Msg("Not currently in a lobby, creating one...");
                    SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, cap);
                }
                if (SteamMatchmaking.GetNumLobbyMembers(__instance._lobbySteamID) >= cap)
                {
                    Core.Log.Warning("Lobby already at max capacity!");
                    return false;
                }
                SteamFriends.ActivateGameOverlayInviteDialog(__instance._lobbySteamID);
                return false;
            }
            catch (Exception e)
            {
                PlayerCap.LogFailureOnce(e);
                return true;
            }
        }
    }

    // The Steam transport's client limit is set in the scene; make sure it fits the cap before the server starts.
    [HarmonyPatch(typeof(FishySteamworks), nameof(FishySteamworks.StartConnection))]
    internal static class StartConnectionPatch
    {
        private static void Prefix(FishySteamworks __instance, bool server)
        {
            if (!server) return;
            try { PlayerCap.RaiseTransportLimit(__instance); }
            catch (Exception e) { PlayerCap.LogFailureOnce(e); }
        }
    }
}
