# AGENTS.md — Mochi Meadows

Kawaii cozy farming game (Stardew-inspired). Unity 6000.5.6f1, C#, all art
and audio generated at runtime from code (no binary assets in the repo).
Playable at https://gregnazario.github.io/mochi-meadows/ (GitHub Pages).

## Project layout

```
MochiMeadows/
  Assets/Scenes/MochiMeadows.unity   — one empty scene with a Boot object
  Assets/Scripts/Runtime/
    Core/GameBootstrap.cs            — builds the ENTIRE game at runtime
    Art/PixelArt.cs                  — ASCII pixel art -> textures/sprites
    Art/SpriteBank.cs                — all sprite definitions + outfit recolor
    Art/CustomSpriteLoader.cs        — PNG + sprite-sheet overrides
    Art/SpriteExporter.cs            — -export-art dump
    Game/  FarmGrid, GameManager, QuestManager, NpcController,
           CritterController, SeasonController, WeatherController,
           PlayerController, DayNightController, SaveSystem
    Inputs/InputService.cs           — keyboard + touch joystick abstraction
    Ui/UiController.cs               — ALL UI built at runtime (~2,600 lines)
    Audio/AudioService.cs            — synthesized sfx + procedural lullaby
  Assets/Scripts/Editor/
    SceneBuilder.cs                  — creates the scene
    BuildScript.cs                   — Mac/WebGL/Windows/Linux/iOS/Android builds
  Assets/WebGLTemplates/MochiMeadows/ — responsive canvas template (fills window)
docs/  DESIGN.md, ART_GUIDE.md, LEVEL_GUIDE.md, ANDROID.md
tools/ render_art.py                 — terminal renderer for the ASCII sprites
```

**The scene is empty.** `GameBootstrap.Awake` starts `InitRoutine` which
builds cameras, world, farm, characters, weather, seasons, UI, and camera rig
(coroutine so custom art can load async on WebGL). Nothing works unless the
bootstrap wiring is intact.

## Conventions

- Namespaces: `MochiMeadows.<Area>`; singletons: `GameManager.I`,
  `AudioService.I`, `QuestManager.I`, `InputService.I`, `GameBootstrap.I`.
- Do not add comments to code unless asked (repo convention).
- All colors come from the pastel `Palette` (Art/PixelArt.cs). Keep pastels.
- Sprites are defined as ASCII string maps in `SpriteBank` (16px art, 4x
  scale). The terminal renderer (`tools/render_art.py`) is the fastest way to
  view/edit sprite art — always render-check art before building.
- UI: `UiController` builds every panel at runtime; `CreateButton` returns a
  Button and the background Image via `out var bg` — **never use
  `button.image` (it is null at runtime)**.
- Custom content pipeline: individual PNGs (`StreamingAssets/art/<Name>.png`
  or `persistentDataPath/art_export/`) and sprite sheets override the
  procedural sprites. Layout is data-driven via `level.json`
  (see ART_GUIDE.md / LEVEL_GUIDE.md). Never hardcode world positions —
  read `LevelConfig.Current`.

## Building

Unity CLI (Mac):

```sh
UNITY="/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity"
$UNITY -batchmode -quit -projectPath MochiMeadows \
  -executeMethod MochiMeadows.EditorTools.BuildScript.BuildMac
# BuildWebGL / BuildWindows / BuildLinux / BuildiOS / BuildAndroid
```

After any code change: compile-check first
(`-batchmode -quit -projectPath ... -logFile x.log`, grep `error CS`),
then build, then run the playtest.

## Testing & verification (mandatory before shipping a change)

1. **Playtest** (headless simulated farmer; reports balance + save round-trip):

   ```sh
   $UNITY -batchmode -quit -projectPath MochiMeadows \
     -executeMethod MochiMeadows.EditorTools.BuildScript.BuildMac
   "MochiMeadows/Builds/Mac/MochiMeadows.app/Contents/MacOS/Mochi Meadows" \
     -playtest -playtest-days 8 -playtest-seed 11
   ```

   Report: `~/Library/Application Support/Mochi Meadows/Mochi Meadows/playtest_report.txt`
   Must end `RESULT: PASS` and `save/load round-trip: PASS`.

2. **Screenshot verification** — the game supports dev capture modes that
   auto-start and screenshot themselves (saved to the app support
   `Screenshots/` folder, newest last):

   ```
   -screenshot            title screen
   -screenshot-game       in-game (+ optional -day N, -outfit N, -expanded)
   -screenshot-house      farmhouse interior
   -screenshot-menu       pause menu   (-screenshot-controls adds controls page)
   -screenshot-quest | -shop | -makeover | -fish | -rain | -scrapbook | -newgame
   -export-art | -export-level        writes editable content to app support
   -edit                   in-game level editor: pick a palette item, tap tiles
                           to place/remove ground, decor, trees, spots, Save level
   ```

## Browser tests

Headless Playwright checks for the WebGL build (layout, title, Start flow,
tap-to-farm). Run from `tools/browser-test`: `npm install && node test.js`
(live site) or `node test.js http://localhost:8000` (local). Needs a Chromium
binary; set `CHROME_PATH` if not found. Exit 0 = pass.

## WebGL template

The PWA template (`Assets/WebGLTemplates/MochiPWA`) is installable/offline
(manifest + service worker + generated icons). The icon is procedurally
rendered at build time (lavender + Mochi + heart); if it looks blank, the
`spriteBank.BuildAll()` guard in `IconGenerator.RenderIcon` is what makes the
cat appear in headless builds.

3. **UI/art changes**: verify with pixel checks (sampling exact colors) or the
   vision tool, then re-run the screenshot modes that cover the changed
   screens. Be aware vision models misread small pixel text — confirm with
   pixel sampling when it matters.

## WebGL deployment (GitHub Pages)

The live site serves the `gh-pages` branch (raw build files at repo root).
Deploy flow — use a worktree, never touch the main worktree:

```sh
git worktree add /tmp/mochi-pages gh-pages
rm -rf /tmp/mochi-pages/Build /tmp/mochi-pages/TemplateData /tmp/mochi-pages/index.html
cp -R MochiMeadows/Builds/WebGL/. /tmp/mochi-pages/
cd /tmp/mochi-pages && git add -A && git commit -m "..." && git push origin gh-pages
cd .. && git worktree remove /tmp/mochi-pages && git worktree prune
```

- Pages takes ~2–3 minutes to rebuild; CDN cache may serve stale files.
- **WebGL requires `PlayerSettings.WebGL.decompressionFallback = true`**
  (GitHub Pages doesn't send `Content-Encoding: br`; the loader must
  decompress in-browser). Don't disable it.
- The custom template makes the canvas fill the browser window — this was a
  real bug (taps appeared broken when the game was a fixed 960x600 island).

## Git quirks on this machine

- **Use HTTPS, not SSH.** The SSH key (`~/.ssh/id_mldsa44_ed25519`) has been
  rejected by GitHub intermittently (and the Keeper agent socket keeps
  dying). The remote is already set to
  `https://github.com/gregnazario/mochi-meadows.git` with `gh` as the
  credential helper (`gh auth setup-git`). If pushes start failing with
  "no such identity: ~/.ssh/id_ed25519", re-run `gh auth setup-git` and
  check `git remote -v` is the https URL.
- Pushes can **fail silently** (a piped `git push` exits non-zero while
  printing only the error's last line). Always check `git ls-remote origin`
  after pushing.
- Branch `gh-pages` holds only the web build; `main` holds source.
- Never commit directly to `main` (global rule) — but note the repo was
  initialized with the first commit on main (hosting setup); use branches +
  PRs for real work.

## iOS simulator builds (fragile — follow exactly)

Unity's generated iOS project defaults to x86_64 for the simulator. After
`BuildiOSSim -simulator`, patch:

```sh
cd MochiMeadows/Builds/iOS
python3 -c "s=open('Unity-iPhone.xcodeproj/project.pbxproj').read(); \
  open('Unity-iPhone.xcodeproj/project.pbxproj','w').write(s.replace('ARCHS = x86_64;','ARCHS = arm64;'))"
MOD="/Applications/Unity/Hub/Editor/6000.5.6f1/PlaybackEngines/iOSSupport/Trampoline"
cp "$MOD/Libraries/baselib-sim-arm64.a" Libraries/baselib.a
cp "$MOD/Frameworks/UnityRuntime-sim-arm64/UnityRuntime.framework/UnityRuntime" Frameworks/UnityRuntime.framework/UnityRuntime
xcodebuild -project Unity-iPhone.xcodeproj -target Unity-iPhone -configuration Release \
  -sdk iphonesimulator CODE_SIGNING_ALLOWED=NO CONFIGURATION_BUILD_DIR=/tmp/mochi-out build
```

Then `xcrun simctl install/launch`. **iOS launch args never reach Unity** —
dev screenshot modes work via `defaults write` into the app container's
`Library/Preferences/com.Mochi-Meadows.Mochi-Meadows.plist` with the
`mochi_dev_shot` key (read once then deleted).

## Maps & editor

- Two maps: the meadow and the farmhouse interior (`LevelConfig.interior`).
  `MapManager` toggles the world roots; the farm is parented under the world
  root so it hides with the meadow (the farm freezing while indoors is
  intentional — crops never wither in the house).
- Weather/day-night components live on the bootstrap GameObject — never
  `SetActive(false)` them (it kills the bootstrap coroutines); gate via their
  `Active` flag instead.
- The level editor (`-edit`) writes ground overrides (`level.ground`) and
  mutates trees/decor/spots, saved via `-export-level` style export.

## Known pitfalls (learned the hard way)

- `Button.image` is null at runtime — always use the `out var bg` from
  `CreateButton`.
- Panel construction order matters (sibling order = z-order). A duplicated
  build call (e.g., `BuildMenu()` twice) silently covers other dialogs.
- uGUI double-tinting: white sprite + `Image.color` for tinted icons; colored
  sprite + colored image = wrong color (hearts were red once).
- `JsonUtility` doesn't support jagged arrays/dictionaries — use flat
  serializable classes (see `LevelConfig`).
- CameraRig enforces integer pixel scaling (pixel-perfect) and clamps to the
  world; changing world size requires `LevelConfig`.
- White text on pastel buttons is illegible — `CreateButton` converts
  near-white label colors to `Palette.Chocolate` automatically.

## Design notes

- Cozy, zero-pressure: no fail states, energy is soft-gated, everything
  autosaves. Keep new mechanics kawaii and forgiving.
- Economy baseline (playtest): ~60 → ~1000+ coins by day 14 with the
  simulated farmer; season bonuses (+25% in-season) drive the curve.
- The playtest simulator exercises the full loop and must stay green.
