using System;
using MelonLoader;
using MelonLoader.Preferences;
using MorePlayers.Patches;

[assembly: MelonInfo(typeof(MorePlayers.Core), MorePlayers.Core.ModName, MorePlayers.Core.ModVersion, "darkharasho", "https://github.com/darkharasho/schedule1-more-players")]
[assembly: MelonGame("TVGS", "Schedule I")]
// Patched by hand in OnInitializeMelon, so a game update that breaks a patch leaves the game at its defaults.
[assembly: HarmonyDontPatchAll]

namespace MorePlayers
{
    public class Core : MelonMod
    {
        public const string ModName = "MorePlayers";
        public const string ModVersion = "0.1.0";

        // Steam's own lobby member limit.
        internal const int MaxCap = 250;

        internal static MelonLogger.Instance Log;
        internal static MelonPreferences_Entry<int> MaxPlayers;

        internal static int Cap => Math.Clamp(MaxPlayers.Value, 1, MaxCap);

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;

            var category = MelonPreferences.CreateCategory("MorePlayers", "More Players");
            MaxPlayers = category.CreateEntry("MaxPlayers", 8, "Max players",
                "Most players allowed in a lobby, including the host. The game's default is 4. Everyone in the lobby needs the mod. Changing it while hosting updates the current lobby.",
                validator: new ValueRange<int>(1, MaxCap));
            MaxPlayers.OnEntryValueChanged.Subscribe((_, _) => PlayerCap.ApplyLive());

            try
            {
                HarmonyInstance.PatchAll(typeof(Core).Assembly);
            }
            catch (Exception e)
            {
                Log.Error($"Patching failed, likely a game update; the player cap stays at the game's default. {e}");
                HarmonyInstance.UnpatchSelf();
                return;
            }

            Log.Msg($"{ModName} {ModVersion} loaded (max players {Cap})");
        }
    }
}
