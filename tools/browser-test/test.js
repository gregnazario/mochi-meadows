#!/usr/bin/env node
// Headless browser tests for the Mochi Meadows WebGL build.
//
//   npm install
//   node test.js                       # test the live GitHub Pages build
//   node test.js http://localhost:8000 # test a local build
//
// Requires a Chromium-based browser. Set CHROME_PATH to the binary if the
// default Playwright Chrome for Testing isn't installed, e.g.:
//   CHROME_PATH="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
const { chromium } = require('playwright-core');
const { PNG } = require('pngjs');

const URL = process.argv[2] || 'https://gregnazario.github.io/mochi-meadows/';
const CHROME = process.env.CHROME_PATH
  || path.join(process.env.HOME, 'Library/Caches/ms-playwright/chromium-1228/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const path = require('path');

let failures = 0;
function check(name, ok, detail) {
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? ' — ' + detail : ''}`);
  if (!ok) failures++;
}

async function snap(page) {
  return PNG.sync.read(await page.screenshot());
}

function countColor(img, r, g, b, tol, stride = 4) {
  let n = 0;
  for (let y = 0; y < img.height; y += stride) {
    for (let x = 0; x < img.width; x += stride) {
      const i = (y * img.width + x) << 2;
      if (Math.abs(img.data[i] - r) < tol && Math.abs(img.data[i+1] - g) < tol && Math.abs(img.data[i+2] - b) < tol) n++;
    }
  }
  return n;
}

function firstPixel(img, r, g, b, tol, stride = 2) {
  for (let y = 0; y < img.height; y += stride) {
    for (let x = 0; x < img.width; x += stride) {
      const i = (y * img.width + x) << 2;
      if (Math.abs(img.data[i] - r) < tol && Math.abs(img.data[i+1] - g) < tol && Math.abs(img.data[i+2] - b) < tol) return { x, y };
    }
  }
  return null;
}

function tileGrass(img, cx, cy, t) {
  let n = 0;
  for (let dy = 0; dy < t; dy += 2) {
    for (let dx = 0; dx < t; dx += 2) {
      const i = ((cy - t/2 + dy) * img.width + (cx - t/2 + dx)) << 2;
      if (Math.abs(img.data[i]-160) < 20 && Math.abs(img.data[i+1]-224) < 20 && Math.abs(img.data[i+2]-144) < 20) n++;
    }
  }
  return n;
}

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: CHROME, args: ['--disable-gpu'] });
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 720 } });
  const page = await ctx.newPage();
  const consoleErrors = [];
  page.on('pageerror', e => consoleErrors.push('PAGEERROR: ' + e.message));

  console.log(`Testing ${URL}\n`);

  // 1) loads without console errors
  await page.goto(URL, { waitUntil: 'load', timeout: 60000 });
  await page.waitForTimeout(15000);
  check('no console/page errors on load', consoleErrors.length === 0, consoleErrors[0] || '');

  // 2) canvas fills the window
  const box = await page.locator('canvas').boundingBox();
  const fills = box && box.width >= 1270 && box.height >= 710;
  check('canvas fills the browser window', !!fills, box ? `${box.width}x${box.height}` : 'no canvas');

  // 3) title screen renders (lavender bg + mint Start button)
  const title = await snap(page);
  const lavender = countColor(title, 82, 69, 140, 30);
  const mint = countColor(title, 191, 242, 204, 16);
  check('title screen renders (lavender + Start button)', lavender > 5000 && mint > 100,
    `lavender=${lavender} mint=${mint}`);

  // 4) click Start (bottom-most mint button), then the new-game Start
  if (mint > 100) {
    let pos = null;
    for (let y = title.height - 1; y >= 0 && !pos; y -= 2)
      for (let x = 0; x < title.width; x += 2) {
        const i = (y * title.width + x) << 2;
        if (Math.abs(title.data[i]-191) < 16 && Math.abs(title.data[i+1]-242) < 16 && Math.abs(title.data[i+2]-204) < 16) { pos = { x, y }; break; }
      }
    if (pos) await page.mouse.click(box.x + pos.x, box.y + pos.y);

    await page.waitForTimeout(2500);
    const ng = await snap(page);
    const ngMint = countColor(ng, 191, 242, 204, 16);
    if (ngMint > 50) {
      let pos2 = null;
      for (let y = ng.height - 1; y >= Math.floor(ng.height * 0.55) && !pos2; y -= 2)
        for (let x = 0; x < ng.width; x += 2) {
          const i = (y * ng.width + x) << 2;
          if (Math.abs(ng.data[i]-191) < 16 && Math.abs(ng.data[i+1]-242) < 16 && Math.abs(ng.data[i+2]-204) < 16) { pos2 = { x, y }; break; }
        }
      if (pos2) await page.mouse.click(box.x + pos2.x, box.y + pos2.y);
    }

    await page.waitForTimeout(7000);
    const inGame = await snap(page);
    const grass = countColor(inGame, 160, 224, 144, 20);
    check('Start button enters the game', grass > 8000, `grass=${grass}`);

    // 5) tap a grass tile right of the player and look for tilled soil
    if (grass > 8000) {
      // the player spawns center-bottom; scan only there
      let skin = null;
      for (let y = Math.floor(inGame.height * 0.5); y < inGame.height - 80 && !skin; y += 2)
        for (let x = Math.floor(inGame.width * 0.35); x < inGame.width * 0.65; x += 2) {
          const i = (y * inGame.width + x) << 2;
          if (Math.abs(inGame.data[i]-255) < 14 && Math.abs(inGame.data[i+1]-233) < 14 && Math.abs(inGame.data[i+2]-214) < 14) { skin = { x, y }; break; }
        }
      const tile = box.width / 26.67;
      let target = null;
      if (skin) {
        for (const dx of [48, 96, 144, -48]) {
          const n = tileGrass(inGame, skin.x + dx, skin.y, 24);
          if (n >= 8) { target = { x: skin.x + dx, y: skin.y }; break; }
        }
      }
      if (!target) {
        for (let y = 140; y < inGame.height - 140; y += 48) {
          for (let x = inGame.width / 2 + 60; x < inGame.width - 120; x += 48) {
            if (tileGrass(inGame, x, y, 24) >= 8) { target = { x, y }; break; }
          }
          if (target) break;
        }
      }
      if (target) {
        const before = tileGrass(inGame, target.x, target.y, tile);
        await page.mouse.click(box.x + target.x, box.y + target.y);
        await page.waitForTimeout(6000);
        const afterSnap = await snap(page);
        const after = tileGrass(afterSnap, target.x, target.y, tile);
        check('tap on a tile produces a farm action (grass -> soil)', after < before,
          `grass ${before} -> ${after} (tile at ${target.x.toFixed(0)},${target.y.toFixed(0)})`);
      } else {
        check('grass tile found for tap test', false, 'no grassy tile found');
      }
    }
  } else {
    check('Start button found', false, 'no mint pixels on title');
  }

  console.log(`\n${failures === 0 ? 'ALL TESTS PASSED' : failures + ' TEST(S) FAILED'}`);
  await browser.close();
  process.exit(failures === 0 ? 0 : 1);
})();
