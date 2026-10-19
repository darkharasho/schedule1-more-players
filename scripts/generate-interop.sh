#!/usr/bin/env bash
# Generates the IL2CPP interop assemblies (what MelonLoader puts in MelonLoader/Il2CppAssemblies) without
# starting the game: Cpp2IL dumps GameAssembly.dll, then Il2CppInterop wraps it, with MelonLoader's settings.
# Output goes to build/interop. Then run scripts/update-refs.sh to refresh lib/refs from it.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
game="${GAME_PATH:-/var/mnt/data/SteamLibrary/steamapps/common/Schedule I}"
profile="${PROFILE_PATH:-$HOME/.config/r2modmanPlus-local/ScheduleI/profiles/Co-op playthrough}"
cpp2il_version="2022.1.0-pre-release.21"
unity_version="$(strings "$game/Schedule I_Data/globalgamemanagers" | grep -m1 -E '^[0-9]{4}\.[0-9]+\.[0-9]+f[0-9]+$' | sed 's/f[0-9]*$//')"
work="$root/build/interop-work"
out="$root/build/interop"

mkdir -p "$work/tools" "$work/unity"
cpp2il="$work/tools/Cpp2IL-$cpp2il_version-Linux"
if [ ! -x "$cpp2il" ]; then
    curl -sfL -o "$cpp2il" "https://github.com/SamboyCoding/Cpp2IL/releases/download/$cpp2il_version/Cpp2IL-$cpp2il_version-Linux"
    chmod +x "$cpp2il"
fi
if [ ! -f "$work/unity/UnityEngine.CoreModule.dll" ]; then
    curl -sfL -o "$work/unity.zip" "https://github.com/LavaGang/MelonLoader.UnityDependencies/releases/download/$unity_version/Managed.zip"
    unzip -qo "$work/unity.zip" -d "$work/unity"
fi

rm -rf "$work/dummy" "$out"
NO_COLOR=1 "$cpp2il" --game-path "$game" --exe-name "Schedule I" --output-as dummydll \
    --use-processor attributeanalyzer attributeinjector --output-to "$work/dummy"

dotnet build "$root/tools/InteropGen/InteropGen.csproj" -c Release -v q -p:MelonLoaderPath="$profile/MelonLoader/net6"
dotnet "$root/tools/InteropGen/bin/Release/net8.0/InteropGen.dll" "$work/dummy" "$game/GameAssembly.dll" "$work/unity" "$out"
echo "Interop assemblies: $out"
