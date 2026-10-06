// Browser proof for GH-18 against a built slice site served over HTTP.
//
//   dotnet run --project src/Tekmerion.Cli -- ingest \
//     --config tests/fixtures/visual-engineering/tekmerion.config.json \
//     --out .tmp/site --forma node_modules/@echelon-foundry/design-system/dist/all.css
//   (cd .tmp/site && python3 -m http.server 8765) &
//   TEKMERION_SITE=http://localhost:8765/ node tests/e2e/slice-navigation.mjs
//
// Requires the `playwright` package and a Chromium (PLAYWRIGHT_CHROMIUM may
// name its executable). JavaScript is disabled: the research experience must
// work without it (TEK-EXP-001). Exits non-zero on the first failed check.

import { chromium } from "playwright";

const base = process.env.TEKMERION_SITE ?? "http://localhost:8765/";
const browser = await chromium.launch(process.env.PLAYWRIGHT_CHROMIUM ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM } : {});
const results = [];
const check = (name, ok, detail = "") => {
  results.push({ name, ok, detail });
  if (!ok) process.exitCode = 1;
};

try {
  const page = await browser.newPage({ javaScriptEnabled: false });

  await page.goto(base + "a/EX-COMP-011/");
  await page.getByRole("link", { name: /Visual Scene Construction/ }).first().click();
  check("experiment report -> research package", page.url().endsWith("/a/RP-COMP-005/"), page.url());

  const frontier = page.locator('h4:has-text("Frontier records originating here") + ul > li > a');
  check("package lists originating frontier records", (await frontier.count()) === 5, String(await frontier.count()));

  await page.goBack();
  check("back returns to the experiment report", page.url().endsWith("/a/EX-COMP-011/"), page.url());
  await page.goForward();
  check("forward returns to the package", page.url().endsWith("/a/RP-COMP-005/"), page.url());

  await frontier.first().click();
  check("package -> originating frontier record", /\/a\/RFR-[0-9A-F]{8}\/$/.test(page.url()), page.url());
  await page.goBack();

  const why = page.locator("details.ef-disclosure").filter({ hasText: "Why is this shown?" }).first();
  await why.locator("summary").click();
  // Since Forma 0.3.0 the disclosure opens with a block-size and
  // content-visibility transition, so its content is rendered (and in
  // innerText) only once the opening has started, not on the click itself.
  await why.locator(".ef-disclosure__content").waitFor({ state: "visible" });
  const explanation = await why.innerText();
  check("'why is this shown?' names the canonical declaration", /declares source-rep|declares origin-document/.test(explanation), explanation.slice(0, 120));

  await page.goto(base + "research/rp-comp-005-visual-scene-construction-predictive-processing-and-active-perception/");
  await page.waitForURL(/\/a\/RP-COMP-005\/$/, { timeout: 5000 }).catch(() => {});
  check("legacy URL redirects to the stable address", page.url().endsWith("/a/RP-COMP-005/"), page.url());

  const keyboard = await browser.newPage();
  await keyboard.goto(base + "a/RP-COMP-005/");
  await keyboard.keyboard.press("Tab");
  const first = await keyboard.evaluate(() => document.activeElement?.textContent?.trim());
  check("first Tab reaches the skip link", first === "Skip to content", String(first));

  for (const path of ["a/EX-COMP-011/", "p/composition-science/", "a/RP-COMP-005/"]) {
    const narrow = await browser.newPage({ viewport: { width: 320, height: 800 } });
    await narrow.goto(base + path);
    const overflow = await narrow.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    results.push({ name: `320px horizontal overflow ${path}`, ok: overflow === 0, detail: `${overflow}px (measured, recorded; not gated)` });
  }
} finally {
  await browser.close();
}

for (const r of results) console.log(`${r.ok ? "ok  " : "FAIL"} ${r.name}${r.detail ? " — " + r.detail : ""}`);
