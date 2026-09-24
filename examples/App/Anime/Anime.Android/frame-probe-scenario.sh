#!/usr/bin/env bash
# ISSUE-146 on-device frame scenario for the Anime example.
#
# Drives a fixed, repeatable interaction sequence on an Android device/emulator with the frame
# probe in verbose mode, then prints one summary line per step: the route-rebuild frame's stage
# split, plus how many frames followed it and the worst of them.
#
#   cold launch -> 3 x (home -> vip -> user -> home) -> 3 x (open detail -> back)
#
# Usage (from the repository root):
#   examples/App/Anime/Anime.Android/frame-probe-scenario.sh <label> [apk]
#
#   <label>  Name of the run; the raw probe log is written to artifacts/frame-probe/<label>.log.
#   [apk]    Signed APK to install first. Omit (or pass "-") to reuse the installed build.
#
# Build the APK with, e.g.:
#   dotnet build examples/App/Anime/Anime.Android/Anime.Android.csproj -c Release -p:RuntimeIdentifier=android-x64
#
# With more than one device attached, select one first: export ANDROID_SERIAL=emulator-5554
#
# Tap coordinates assume a 1080x2400 (420 dpi) screen — the default Pixel-class emulator image.
# Step markers are written into logcat under the probe's own tag, so each probe line can be
# attributed to the step that caused it.
set -u
LABEL=$1
APK=${2:--}
OUT_DIR=artifacts/frame-probe
OUT=$OUT_DIR/$LABEL.log
mkdir -p "$OUT_DIR"

if [ "$APK" != "-" ]; then
  adb install -r -d "$APK" >/dev/null 2>&1 || { echo "install failed: $APK"; exit 1; }
fi
ACTIVITY=$(adb shell cmd package resolve-activity --brief com.miko.anime | tail -1 | tr -d '\r')

adb shell am force-stop com.miko.anime
sleep 1
adb logcat -c
adb shell am start -W -n "$ACTIVITY" --ez frameprobe true --ez frameprobe-verbose true | grep -E "TotalTime"
sleep 10

# The message is single-quoted for the device shell: "->" would otherwise be parsed as a redirect.
mark() { adb shell log -t MikoFrameProbe "'[scenario] $1'"; }

for round in 1 2 3; do
  mark "tab home_to_vip r$round";  adb shell input tap 540 2262; sleep 1.5
  mark "tab vip_to_user r$round";  adb shell input tap 900 2262; sleep 1.5
  mark "tab user_to_home r$round"; adb shell input tap 180 2262; sleep 1.5
done

for round in 1 2 3; do
  mark "detail push r$round"; adb shell input tap 192 1320; sleep 2.5
  mark "detail pop r$round";  adb shell input tap 62 202;   sleep 2.0
done

adb logcat -d -s MikoFrameProbe > "$OUT"
echo "== $LABEL ($OUT)"
awk '
  /\[scenario\]/ { if (step != "") flush(); step = substr($0, index($0, "[scenario]") + 11); n = 0; worst = 0; rb = ""; detail = ""; next }
  /REBUILD/ { match($0, /total=[0-9.]+/); rb = substr($0, RSTART + 6, RLENGTH - 6);
              match($0, /build=[0-9.]+ style=[0-9.]+ layout=[0-9.]+ paint=[0-9.]+ passes=[0-9\/]+/); detail = substr($0, RSTART, RLENGTH); next }
  /frame-probe\] #/ { if (rb != "") { match($0, /total=[0-9.]+/); t = substr($0, RSTART + 6, RLENGTH - 6) + 0; n++; if (t > worst) worst = t } }
  function flush() { printf "%-22s rebuild=%7sms  %-60s next-frames=%3d worst-next=%6.1fms\n", step, rb, detail, n, worst }
  END { if (step != "") flush() }
' "$OUT"
