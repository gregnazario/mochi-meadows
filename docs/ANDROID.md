# Android Build Guide

The game builds and runs on Android. Everything here is verified working.

## Install the module (one-time, ~3 GB)

**Option A — command line (works headlessly):** the Hub installer prompts
interactively for child modules, so drive it with `expect`:

```sh
cat > /tmp/hub-android.exp <<'EOF'
#!/usr/bin/expect -f
set timeout 18000
log_user 0
spawn "/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless \
  install-modules --version 6000.5.6f1 --module android
while {1} {
    expect {
        -re {\(Y/n\)} { send "Y\r" }
        -re {Task Completed|completed successfully|All Tasks} { exit 0 }
        eof { exit 0 }
    }
}
EOF
chmod +x /tmp/hub-android.exp && /tmp/hub-android.exp
```

**Option B — GUI:** Unity Hub → Installs → 6000.5.6f1 → ⚙ Add modules →
Android Build Support (with SDK & NDK Tools + OpenJDK).

## Build the APK

- Unity editor: **Tools → Mochi Meadows → Build Android APK**
- Command line:

```sh
"/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath MochiMeadows \
  -executeMethod MochiMeadows.EditorTools.BuildScript.BuildAndroid
```

Output: `MochiMeadows/Builds/Android/MochiMeadows.apk` (~23 MB).

## What the build uses (verified via aapt)

- **IL2CPP** — Unity 6 dropped Mono2x on Android; using Mono2x makes the
  build fail with "Target architecture not specified". Keep IL2CPP.
- **ARM64** (`arm64-v8a`), minSdk 26, targetSdk 36
- Package `com.MochiMeadows.MochiMeadows`, label "Mochi Meadows"

## Notes

- Touch input, the Act button, joystick, and saves work out of the box.
- The APK is ready to sideload or upload to Google Play.
