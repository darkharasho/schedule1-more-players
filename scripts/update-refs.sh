#!/usr/bin/env bash
# Regenerates lib/refs: reference-only copies (public API signatures, no method bodies) of the game's
# interop assemblies and MelonLoader, so CI can build without the game installed.
# Interop comes from MelonLoader/Il2CppAssemblies once the game has started with MelonLoader, otherwise from
# build/interop (scripts/generate-interop.sh). Set INTEROP_DIR to pick another.
# Needs the refasmer tool: dotnet tool install -g JetBrains.Refasmer.CliTool
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
profile="${PROFILE_PATH:-$HOME/.config/r2modmanPlus-local/ScheduleI/profiles/Co-op playthrough}"
melon="$profile/MelonLoader/net6"
interop="${INTEROP_DIR:-$profile/MelonLoader/Il2CppAssemblies}"
[ -f "$interop/Assembly-CSharp.dll" ] || interop="$root/build/interop"
[ -f "$interop/Assembly-CSharp.dll" ] || { echo "No interop assemblies; run scripts/generate-interop.sh" >&2; exit 1; }
out="$root/lib/refs"
refasmer="$(command -v refasmer || echo "$HOME/.dotnet/tools/refasmer")"

# Keep in sync with the <Reference> items in src/MorePlayers/MorePlayers.csproj.
interop_dlls=(Il2Cppmscorlib Assembly-CSharp Il2Cppcom.rlabrecque.steamworks.net Il2CppFishNet.Runtime
    UnityEngine.CoreModule UnityEngine.UI Unity.TextMeshPro)
melon_dlls=(MelonLoader 0Harmony Il2CppInterop.Runtime Il2CppInterop.Common)

rm -rf "$out"
mkdir -p "$out"
# Refasmer can't read some interop assemblies (UnityEngine.CoreModule: "Read out of bounds"); those are copied
# as they are. Interop assemblies only hold wrappers that call into the game, no game code.
ref() {
    "$refasmer" -q -c --omit-non-api-members=true -O "$out" "$1" 2>/dev/null && [ -f "$out/$(basename "$1")" ] \
        || { echo "refasmer failed on $(basename "$1"); copying it"; cp "$1" "$out/"; }
}
for name in "${interop_dlls[@]}"; do ref "$interop/$name.dll"; done
for name in "${melon_dlls[@]}"; do ref "$melon/$name.dll"; done
ls -l "$out"
