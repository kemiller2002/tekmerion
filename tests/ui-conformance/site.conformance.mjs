// UI conformance suite for the Tekmerion research site against
// visual-engineering/UI-LAYOUT-INTERACTION@1.0.0. Every assertion resolves
// controls by role and accessible name, waits with web-first assertions and
// compares observed pre/post state. No coordinates, no fixed sleeps.
// (Named *.conformance.mjs so the repository's Vitest run does not collect it.)
import { expect, test } from "@playwright/test";
import {
  CONTRACT_WIDTHS,
  expectActionOutcome,
  expectLabelIntact,
  expectNoOverflow,
  expectPeerAlignment,
  inventoryActions,
  recordTabSequence,
  scaleDefaultText,
  settleFontsAndFrames,
  wasFocusedDuringSequence
} from "./forma-conformance/index.mjs";
import { CHROME, NAV, REPRESENTATIVE, ROUTES, SEARCH_CONTROLS, SITE_TITLE, SURFACES, declaredActionsFor } from "./surfaces.mjs";

const escape = value => value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
const NAV_NAMES = NAV.map(([name]) => name);
const CHROME_PROBE = "/collections/status/";

const annotate = (type, description) => test.info().annotations.push({ type, description });
const primaryNav = page => page.getByRole("navigation", { name: "Primary" });
const pathPattern = destination => new RegExp(`^https?://[^/]+${escape(destination)}$`);
const pageHeading = (page, name) => page.getByRole("heading", { level: 1, name, exact: true }).first();

const expectRouteFrame = async (page, route) => {
  await expect(pageHeading(page, route.heading)).toBeVisible();
  await expect(page.getByRole("banner").getByRole("link", { name: SITE_TITLE, exact: true })).toBeVisible();
  for (const name of NAV_NAMES) await expectLabelIntact(primaryNav(page).getByRole("link", { name, exact: true }));
  // Wayfinding: one primary navigation everywhere; the current section is marked.
  await expect(primaryNav(page).getByRole("link")).toHaveText(NAV_NAMES);
  await expect(primaryNav(page).locator("[aria-current=page]")).toHaveCount(route.current ? 1 : 0);
  if (route.current) await expect(primaryNav(page).getByRole("link", { name: route.current, exact: true })).toHaveAttribute("aria-current", "page");
};

// --- ULI-L05/L06/L09: every route at 320px; one route per template at every width

const layoutMatrix = [
  ...ROUTES.map(route => ({ route, width: 320 })),
  ...REPRESENTATIVE.flatMap(route => CONTRACT_WIDTHS.filter(width => width !== 320).map(width => ({ route, width })))
];

for (const { route, width } of layoutMatrix) {
  test(`${route.path} at ${width}px renders without overflow, with intact navigation`, async ({ page }) => {
    annotate("uli-surface", route.id);
    annotate("uli-surface", "chrome");
    await page.setViewportSize({ width, height: 900 });
    await page.goto(route.path);
    await expectRouteFrame(page, route);
    await expectNoOverflow(page);
  });
}

// --- ULI-L05: 200% default text size (browser text-scaling setting) ---------

const textMatrix = [
  ...ROUTES.map(route => ({ route, width: 320 })),
  ...REPRESENTATIVE.flatMap(route => CONTRACT_WIDTHS.filter(width => width !== 320).map(width => ({ route, width })))
];

for (const { route, width } of textMatrix) {
  test(`${route.path} at ${width}px with 200% text reflows without overflow`, async ({ page }) => {
    annotate("uli-surface", route.id);
    await page.setViewportSize({ width, height: 900 });
    await page.goto(route.path);
    await scaleDefaultText(page, 2);
    await expectNoOverflow(page);
    for (const name of NAV_NAMES) await expectLabelIntact(primaryNav(page).getByRole("link", { name, exact: true }));
  });
}

// --- ULI-A01/A06/A10: only declared action classes render --------------------

for (const route of ROUTES) {
  test(`${route.path} renders only declared actions`, async ({ page }) => {
    annotate("uli-surface", route.id);
    await page.goto(route.path);
    await settleFontsAndFrames(page);
    const declared = declaredActionsFor(route);
    const isDeclared = item => declared.some(action => action.control.role === item.role && action.control.name === item.name);
    const outside = [...(await inventoryActions(page.getByRole("banner"))), ...(await inventoryActions(page.getByRole("contentinfo")))];
    const mainItems = await inventoryActions(page.locator("main"));
    expect(outside.filter(item => !isDeclared(item)).map(item => `${item.role} "${item.name}" -> ${item.href}`), "ULI-A10: chrome actions without a declared contract").toEqual([]);
    expect(await page.locator("body > :not(.skip-link, header, main, footer, script)").count(), "unexpected top-level regions").toBe(0);
    // In <main>, anything that is not a link (content links are verified by
    // destination) must be a declared control.
    expect(mainItems.filter(item => item.role !== "link" && !isDeclared(item)).map(item => `${item.role} "${item.name}"`), "ULI-A10: controls without a declared contract").toEqual([]);
    expect([...outside, ...mainItems].filter(item => !item.name).map(item => item.href), "ULI-A06: actions without an accessible name").toEqual([]);
  });
}

// --- ULI-A01: every content link in <main> has a meaningful destination ------

const pageHasFragment = async (request, path, fragment) => {
  const response = await request.get(path);
  const body = await response.text();
  return { status: response.status(), found: !fragment || new RegExp(`\\sid="${escape(fragment)}"`).test(body) };
};

for (const route of ROUTES) {
  test(`${route.path} content links resolve`, async ({ page, request, baseURL }) => {
    annotate("uli-content-links", route.id);
    await page.goto(route.path);
    const links = await page.locator("main a[href]").evaluateAll(anchors => anchors.map(anchor => ({
      text: anchor.textContent.replace(/\s+/g, " ").trim(), href: anchor.getAttribute("href"), resolved: anchor.href
    })));
    const cache = new Map();
    const problems = [];
    for (const item of links) {
      const url = new URL(item.resolved);
      if (item.href.startsWith("javascript:") || item.href === "#" || item.href === "") {
        problems.push(`"${item.text}" has no destination (${item.href})`);
      } else if (url.origin === new URL(baseURL).origin) {
        const key = `${url.pathname}#${url.hash}`;
        const result = cache.get(key) ?? await pageHasFragment(request, url.pathname, decodeURIComponent(url.hash.slice(1)));
        cache.set(key, result);
        if (result.status !== 200) problems.push(`"${item.text}" -> ${item.href}: HTTP ${result.status}`);
        else if (!result.found) problems.push(`"${item.text}" -> ${item.href}: fragment target missing`);
      } else if (!["https:", "mailto:"].includes(url.protocol)) {
        problems.push(`"${item.text}" -> ${item.href}: not an https destination`);
      }
    }
    expect(problems, `ULI-A01: ${links.length} content links on ${route.path}`).toEqual([]);
  });
}

// --- Declared navigation actions: destination and outcome (ULI-A01/A02/A06) --

for (const surface of SURFACES) {
  for (const action of surface.actions.filter(item => item.kind === "navigate")) {
    test(`${surface.id}/${action.id} destination is valid`, async ({ page, request }) => {
      annotate("uli-destination", `${surface.id}/${action.id}`);
      await page.goto(action.from);
      const control = page.getByRole("link", { name: action.control.name, exact: true });
      await expect(control).toHaveCount(1);
      await expect(control).toHaveAttribute("href", action.destination);
      expect((await request.get(action.destination)).status(), action.destination).toBe(200);
    });
  }
}

const navigationActions = SURFACES.flatMap(surface => surface.actions.filter(action => action.kind === "navigate").map(action => ({ surface, action })));

for (const width of [390, 1280]) {
  test.describe(`navigation outcomes at ${width}px`, () => {
    test.use({ viewport: { width, height: 900 } });
    for (const { surface, action } of navigationActions) {
      test(`${surface.id}: ${action.id} reaches ${action.destination}`, async ({ page }) => {
        annotate("uli-action", `${surface.id}/${action.id}`);
        await page.goto(action.from);
        await expectActionOutcome(page, {
          ruleId: "ULI-A02",
          action: action.control,
          outcome: { kind: "url", expected: pathPattern(action.destination) }
        });
        await expect(pageHeading(page, action.heading)).toBeVisible();
      });
    }

    test("skip link moves to the main content", async ({ page }) => {
      annotate("uli-action", "chrome/skip-link");
      await page.goto(CHROME_PROBE);
      const skip = page.getByRole("link", { name: "Skip to content" });
      await expect(skip).not.toBeInViewport();
      await page.keyboard.press("Tab");
      await expect(skip).toBeFocused();
      await expect(skip).toBeInViewport();
      await expectActionOutcome(page, {
        ruleId: "ULI-A02",
        action: { role: "link", name: "Skip to content" },
        outcome: { kind: "url", expected: pathPattern(`${CHROME_PROBE}#main`) }
      });
      await expect(page.getByRole("main")).toBeInViewport();
    });
  });
}

// --- Table of contents: every entry reaches its section (ULI-A01/A02) --------

const tocRoute = ROUTES.find(route => route.path === "/research/c-relationship-catalog/") ?? ROUTES.find(route => route.kind === "document");

test(`${tocRoute.path} table of contents entries reach their sections`, async ({ page }) => {
  annotate("uli-surface", tocRoute.id);
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto(tocRoute.path);
  const toc = page.getByRole("complementary").filter({ has: page.getByRole("heading", { name: "On this page" }) });
  const entries = await toc.getByRole("link").evaluateAll(anchors => anchors.map(anchor => ({ name: anchor.textContent.trim(), hash: anchor.getAttribute("href") })));
  expect(entries.length).toBeGreaterThan(1);
  for (const entry of entries) {
    await expectActionOutcome(page, {
      ruleId: "ULI-A02",
      action: { role: "link", name: entry.name, scope: toc },
      outcome: { kind: "url", expected: pathPattern(`${tocRoute.path}${entry.hash}`) }
    });
    await expect(page.locator(`[id="${entry.hash.slice(1)}"]`)).toBeInViewport();
  }
});

// --- Search: honest loading, results, failure and recovery (ULI-A02/A07) ----

const statusLine = page => page.getByRole("status", { name: "Search status" });
const searchButton = page => page.getByRole("button", { name: "Search", exact: true });
const KNOWN_TITLE = "Reusable Research Publishing System";

test.describe("search", () => {
  test("a query lists matching documents and a result opens its page", async ({ page }) => {
    annotate("uli-action", "search/search-submit");
    await page.goto("/search/");
    await expect(statusLine(page)).toHaveText("");
    await page.getByRole("searchbox", { name: "Query" }).fill("reusable publishing system");
    await expectActionOutcome(page, {
      ruleId: "ULI-A02",
      action: { role: "button", name: "Search" },
      outcome: { kind: "text", target: { role: "status", name: "Search status" }, expected: /^\d+ results?$/ }
    });
    const result = page.getByRole("main").getByRole("link", { name: KNOWN_TITLE, exact: true });
    await expect(result).toBeVisible();
    await result.click();
    await expect(page).toHaveURL(/\/research\/rp-ve-2026-0001-reusable-research-publishing-system\/$/);
    await expect(pageHeading(page, KNOWN_TITLE)).toBeVisible();
  });

  test("a filter narrows results to the chosen status", async ({ page }) => {
    annotate("uli-action", "search/search-submit");
    const count = async () => Number((await statusLine(page).textContent()).match(/^(\d+) results?$/)?.[1] ?? Number.NaN);
    const resultStatuses = () => page.locator("#search-results article dl > div:nth-child(2) dd").allTextContents();
    await page.goto("/search/");
    await page.getByRole("searchbox", { name: "Query" }).fill("research");
    await searchButton(page).click();
    await expect(statusLine(page)).toHaveText(/^\d+ results?$/);
    const unfiltered = await count();
    expect(new Set(await resultStatuses()).size, "the unfiltered results span several statuses").toBeGreaterThan(1);
    await page.getByRole("combobox", { name: "Status" }).selectOption("accepted");
    // The status already reads "N results", so the observable outcome is the
    // smaller count and the result statuses, not the status text alone.
    await searchButton(page).click();
    await expect.poll(count).toBeLessThan(unfiltered);
    expect(await count()).toBeGreaterThan(0);
    expect([...new Set(await resultStatuses())]).toEqual(["accepted"]);
  });

  test("a query with no match says so and lists nothing", async ({ page }) => {
    annotate("uli-action", "search/search-submit");
    await page.goto("/search/");
    await page.getByRole("searchbox", { name: "Query" }).fill("zzqqxxnomatchtoken");
    await searchButton(page).click();
    await expect(statusLine(page)).toHaveText("0 results");
    await expect(page.getByRole("main").getByText("No results matched the current query and filters.")).toBeVisible();
  });

  test("an index that fails to load is reported as an error and a retry recovers", async ({ page }) => {
    annotate("uli-action", "search/search-submit");
    let release;
    const gate = new Promise(resolve => { release = resolve; });
    let fail = true;
    // Only the first request for the index entry point fails.
    await page.route(/\/pagefind\/pagefind\.js(\?.*)?$/, async route => {
      await gate;
      if (fail) return route.abort("connectionrefused");
      return route.fallback();
    });
    await page.goto("/search/");
    await page.getByRole("searchbox", { name: "Query" }).fill("reusable publishing system");
    await searchButton(page).click();
    // Pending: honest progress while the index loads.
    await expect(statusLine(page)).toHaveText("Searching…");
    await expect(page.locator("#search-results")).toHaveAttribute("aria-busy", "true");
    release();
    // Failure: an error, never a result count or results.
    await expect(statusLine(page)).toHaveText(/could not load/);
    await expect(page.locator("#search-results")).not.toHaveAttribute("aria-busy", "true");
    await expect(page.getByRole("main").getByRole("link", { name: KNOWN_TITLE })).toHaveCount(0);
    await expect(page.getByRole("searchbox", { name: "Query" })).toHaveValue("reusable publishing system");
    // Recovery: the next attempt loads the index and reports results.
    fail = false;
    await searchButton(page).click();
    await expect(statusLine(page)).toHaveText(/^\d+ results?$/);
    await expect(page.getByRole("main").getByRole("link", { name: KNOWN_TITLE, exact: true })).toBeVisible();
  });
});

// --- ULI-L01/L02/L10: declared alignment groups ----------------------------

for (const surface of SURFACES) {
  for (const group of surface.groups) {
    for (const width of group.widths) {
      test(`${surface.id}: ${group.id} aligns at ${width}px`, async ({ page }) => {
        annotate("uli-group", `${surface.id}/${group.id}@${width}`);
        await page.setViewportSize({ width, height: 900 });
        await page.goto(surface.id === "chrome" ? CHROME_PROBE : surface.path);
        await expectPeerAlignment(page, {
          ruleId: group.ruleId,
          id: group.id,
          edge: group.edge,
          peers: group.peers,
          scope: group.scope === "navigation" ? primaryNav(page) : undefined
        });
      });
    }
  }
}

// --- ULI-A08: keyboard reachability; reduced motion; forced colors ----------

test("every chrome action is reachable by keyboard", async ({ page }) => {
  annotate("uli-surface", "chrome");
  await page.goto(CHROME_PROBE);
  await recordTabSequence(page, CHROME.length + 1);
  for (const action of CHROME) {
    expect(await wasFocusedDuringSequence(page.getByRole("link", { name: action.control.name, exact: true })), action.id).toBe(true);
  }
});

test("the search form is operable by keyboard", async ({ page }) => {
  annotate("uli-surface", "search");
  await page.goto("/search/");
  await recordTabSequence(page, CHROME.length + SEARCH_CONTROLS.length + 2);
  for (const control of [...SEARCH_CONTROLS, { role: "button", name: "Search" }]) {
    expect(await wasFocusedDuringSequence(page.getByRole(control.role, { name: control.name, exact: true })), control.name).toBe(true);
  }
  await page.getByRole("searchbox", { name: "Query" }).fill("reusable publishing system");
  await page.getByRole("searchbox", { name: "Query" }).press("Enter");
  await expect(statusLine(page)).toHaveText(/^\d+ results?$/);
});

test("reduced motion removes smooth scrolling", async ({ page }) => {
  annotate("uli-surface", "home");
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto("/");
  await expect.poll(() => page.evaluate(() => getComputedStyle(document.documentElement).scrollBehavior)).toBe("auto");
});

test("forced colors keep the primary button bounded and navigation visible", async ({ page }) => {
  annotate("uli-surface", "home");
  await page.emulateMedia({ forcedColors: "active" });
  await page.goto("/");
  await expect.poll(() => page.getByRole("link", { name: "Search all research" }).evaluate(element => getComputedStyle(element).borderTopStyle)).toBe("solid");
  for (const name of NAV_NAMES) await expect(primaryNav(page).getByRole("link", { name, exact: true })).toBeVisible();
  await expectNoOverflow(page);
});

// --- Failure path: an unknown route is a 404 page, not a soft success -------

test("an unknown route serves the not-found page with a 404 status", async ({ page }) => {
  annotate("uli-surface", "chrome");
  const response = await page.goto("/no-such-research/");
  expect(response.status()).toBe(404);
  await expect(pageHeading(page, "Page not found")).toBeVisible();
  await expect(primaryNav(page).getByRole("link")).toHaveText(NAV_NAMES);
});
