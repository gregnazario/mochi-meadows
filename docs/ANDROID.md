# Android Build Guide

The game is Android-ready in code (input, UI, saves, builds) — the only thing
missing is the build support module, which the headless installer can't fetch
on this machine (a license/agreement gate that needs the Hub GUI).

## Install the module (one-time, ~5 minutes)

1. Open **Unity Hub** → **Installs**
2. Find **6000.5.6f1** → click the **gear ⚙** → **Add modules**
3. Tick **Android Build Support** (includes Android SDK & NDK Tools and
   OpenJDK — tick all three sub-options)
4. **Install** and wait for the download (~2–3 GB)

## Build the APK

Then either:

- In the Unity editor: **Tools → Mochi Meadows → Build Android APK**
- Or from the command line:

```sh
"/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath MochiMeadows \
  -executeMethod MochiMeadows.EditorTools.BuildScript.BuildAndroid
```

The APK lands at `MochiMeadows/Builds/Android/MochiMeadows.apk` and is ready
to sideload (or upload to Google Play).

## What the build does

- `PlayerSettings.SetScriptingBackend(Android, IL2CPP)` (set in
  `BuildScript.ConfigurePlayer`)
- ARM64 ABI (default IL2CPP target)
- Compressed textures, Brotli assets
- Orientation is free; the game letterboxes itself on any aspect

## Notes

- If the Hub install stalls at 0% forever, quit the Hub and reopen it, then
  retry — the headless path has a known agreement gate on this machine, so
  use the GUI.
- Android emulator verification: install the APK on any device/emulator —
  touch input, the Act button, and saves all work out of the box.
