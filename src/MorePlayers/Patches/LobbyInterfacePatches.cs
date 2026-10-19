using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.UI.Multiplayer;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MorePlayers.Patches
{
    // The lobby panel in the top-right corner: an "Entries" GridLayoutGroup holding the Invite button and four
    // 75px player slots in one row (LobbyInterface.PlayerSlots). UpdatePlayers fills slot i with lobby member i,
    // so more members need more slots. Past 4 slots the grid wraps into rows of 5 cells (the original row width),
    // up to 3 rows, then shrinks the cells to keep it at 3 rows.
    internal static class LobbySlots
    {
        private const int CellsPerRow = 5;
        private const int MaxRows = 3;

        // The grid's scene values, read the first time we change one. Every scene's panel uses the same prefab.
        private static bool _haveBase;
        private static Vector2 _baseCellSize;
        private static Vector2 _baseSpacing;
        private static int _basePaddingLeft;

        internal static void Ensure(LobbyInterface ui)
        {
            var slots = ui.PlayerSlots;
            if (slots == null || slots.Length == 0) return;

            int count = slots.Length;
            int cap = Core.Cap;
            if (count < cap)
            {
                var template = slots[count - 1];
                var grown = new Il2CppReferenceArray<RectTransform>(cap);
                for (int i = 0; i < count; i++) grown[i] = slots[i];
                for (int i = count; i < cap; i++)
                {
                    var entry = Object.Instantiate(template.gameObject, template.parent);
                    entry.name = $"Entry ({i})";
                    entry.SetActive(false);
                    grown[i] = entry.GetComponent<RectTransform>();
                }
                ui.PlayerSlots = grown;
                count = cap;
            }

            if (count > 4) Layout(slots[0].parent.GetComponent<GridLayoutGroup>(), count + 1);
        }

        private static void Layout(GridLayoutGroup grid, int cells)
        {
            if (grid == null) return;
            if (!_haveBase)
            {
                _baseCellSize = grid.cellSize;
                _baseSpacing = grid.spacing;
                _basePaddingLeft = grid.padding.left;
                _haveBase = true;
            }

            int perRow = Math.Max(CellsPerRow, (cells + MaxRows - 1) / MaxRows);
            float scale = (float)CellsPerRow / perRow;
            grid.cellSize = _baseCellSize * scale;
            grid.spacing = new Vector2(_baseSpacing.x * scale, _baseSpacing.y);
            grid.padding.left = Mathf.RoundToInt(_basePaddingLeft * scale);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = perRow;
            LayoutRebuilder.MarkLayoutForRebuild(grid.GetComponent<RectTransform>());
        }
    }

    [HarmonyPatch(typeof(LobbyInterface), nameof(LobbyInterface.Start))]
    internal static class LobbyInterfaceStartPatch
    {
        private static void Postfix(LobbyInterface __instance)
        {
            try
            {
                LobbySlots.Ensure(__instance);
                __instance.UpdateUI();
            }
            catch (Exception e) { PlayerCap.LogFailureOnce(e); }
        }
    }

    // UpdateUI writes "Lobby (n/4)" and shows Invite only to a host with fewer than 4 players.
    [HarmonyPatch(typeof(LobbyInterface), nameof(LobbyInterface.UpdateUI))]
    internal static class UpdateUIPatch
    {
        private static readonly Regex LimitSuffix = new Regex(@"/\d+\)$");

        private static void Postfix(LobbyInterface __instance)
        {
            try
            {
                var title = __instance.LobbyTitle;
                if (title != null) title.text = LimitSuffix.Replace(title.text ?? "", $"/{Core.Cap})");
                InviteButton.Update(__instance);
            }
            catch (Exception e) { PlayerCap.LogFailureOnce(e); }
        }
    }

    [HarmonyPatch(typeof(LobbyInterface), nameof(LobbyInterface.UpdateButtons))]
    internal static class UpdateButtonsPatch
    {
        private static void Postfix(LobbyInterface __instance)
        {
            try { InviteButton.Update(__instance); }
            catch (Exception e) { PlayerCap.LogFailureOnce(e); }
        }
    }

    internal static class InviteButton
    {
        internal static void Update(LobbyInterface ui)
        {
            var lobby = ui.Lobby;
            if (lobby == null || ui.InviteButton == null) return;
            ui.InviteButton.gameObject.SetActive(lobby.IsHost && lobby.PlayerCount < Core.Cap);
        }
    }
}
