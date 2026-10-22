#!/usr/bin/env bash
# End-to-end check of the dotnet new templates (Release-1.0.0 D1).
#
# Packs every library and the template package into a private feed, installs the templates
# into a private hive, scaffolds each template/layout and builds every head in Release from
# the packed packages — the path a user takes, which the unit tests never exercise.
#
#   tools/template-e2e.sh                 # all layouts, all heads the machine can build
#   LAYOUTS="blank full" tools/template-e2e.sh
#   HEADS="Desktop Android" tools/template-e2e.sh
#
# Android heads need the android workload; iOS heads need macOS with the ios workload and
# build for ios-arm64 without signing. Heads whose workload is missing are skipped, not failed.
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
work="${WORK_DIR:-$(mktemp -d "${TMPDIR:-/tmp}/miko-template-e2e.XXXXXX")}"
layouts="${LAYOUTS:-blank sidemenu tabs list full}"
heads="${HEADS:-Desktop Simulator Android iOS}"
version="1.0.0-dev"

mkdir -p "$work"
# Resolve symlinks (macOS: /tmp and mktemp's /var/folders both are). Building through a
# symlinked path makes the Razor SDK compute TargetPath without the folder, so a page under
# Pages/ without an @namespace lands in the root namespace and the `full` layout fails.
work="$(cd "$work" && pwd -P)"
feed="$work/feed"
hive="$work/hive"
mkdir -p "$feed" "$hive"
echo "work dir: $work"

# Look for the platform SDK pack rather than a workload id: the iOS SDK also arrives through
# the maui workload, in which case `dotnet workload list` shows no "ios" entry.
dotnet_root="$(dirname "$(readlink -f "$(command -v dotnet)")")"
has_workload() {
  case "$1" in
    android) compgen -G "$dotnet_root/packs/Microsoft.Android.Sdk.*" >/dev/null ;;
    ios)     compgen -G "$dotnet_root/packs/Microsoft.iOS.Sdk.*" >/dev/null ;;
  esac
}

dotnet pack "$repo/miko.slnx" -c Release -o "$feed" -p:Version="$version" --nologo -v quiet
dotnet pack "$repo/templates/Miko.Templates.csproj" -c Release -o "$feed" -p:Version="$version" --nologo -v quiet
dotnet new install "$feed/Miko.Templates.$version.nupkg" --debug:custom-hive "$hive" >/dev/null

# The scaffolded apps restore Miko* from the private feed only; a private packages folder keeps
# stale copies of earlier local packs (same version) out of the build.
write_nuget_config() {
  cat > "$1/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config><add key="globalPackagesFolder" value="$work/packages" /></config>
  <packageSources>
    <add key="miko-e2e" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="miko-e2e"><package pattern="Miko*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
EOF
}

failures=()
build() {
  local label="$1"; shift
  echo "::group::$label"
  if dotnet build "$@" -c Release --nologo -v quiet -warnaserror:NU1603; then
    echo "PASS $label"
  else
    echo "FAIL $label"
    failures+=("$label")
  fi
  echo "::endgroup::"
}

# A hyphenated name exercises the identifier rewriting dotnet new applies (my-app -> my_app).
for layout in $layouts; do
  app="$work/apps/razor-$layout"
  dotnet new miko-razor -n my-app -o "$app" --layout="$layout" --debug:custom-hive "$hive" >/dev/null
  write_nuget_config "$app"
  build "miko-razor/$layout" "$app"

  app="$work/apps/multiplatform-$layout"
  dotnet new miko-multiplatform -n my-app -o "$app" --layout="$layout" --debug:custom-hive "$hive" >/dev/null
  write_nuget_config "$app"
  for head in $heads; do
    project=$(ls "$app"/*."$head"/*."$head".csproj)
    case "$head" in
      Android) has_workload android || { echo "skip $head (no android workload)"; continue; }
               build "miko-multiplatform/$layout/$head" "$project" ;;
      iOS)     [[ "$(uname)" == Darwin ]] && has_workload ios || { echo "skip $head (needs macOS + ios workload)"; continue; }
               build "miko-multiplatform/$layout/$head" "$project" -r ios-arm64 \
                 -p:EnableCodeSigning=false -p:ValidateXcodeVersion=false ;;
      *)       build "miko-multiplatform/$layout/$head" "$project" ;;
    esac
  done
done

if ((${#failures[@]})); then
  printf 'Failed builds:\n'; printf '  %s\n' "${failures[@]}"
  exit 1
fi
echo "All template builds passed."
