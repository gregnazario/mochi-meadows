# Browser tests

Headless-browser verification for the WebGL build: loads the game, checks the
canvas fills the window, verifies the title renders, clicks Start, and taps a
tile to confirm farming actions work.

## Run

```sh
cd tools/browser-test
npm install
node test.js                        # live GitHub Pages build
node test.js http://localhost:8000  # local build (python3 -m http.server)
```

Requires a Chromium-based browser:

- Default: Playwright's "Chrome for Testing" in
  `~/Library/Caches/ms-playwright/chromium-1228/...` (install with
  `npx playwright-core install chromium` or symlink an existing Chrome).
- Or point at any Chrome: `CHROME_PATH="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" node test.js`

## Checks

1. No console/page errors after load
2. Canvas fills the browser window (responsive template)
3. Title screen renders (lavender + mint Start button)
4. Start button click enters the game
5. Tap on a grass tile produces a farm action (soil appears)

Exit code 0 = all pass. Useful after any WebGL/UI change before deploying.
