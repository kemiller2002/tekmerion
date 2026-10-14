// Declared conformance inventory for the Tekmerion research site against
// visual-engineering/UI-LAYOUT-INTERACTION@1.0.0 (pinned at visual-engineering
// 8da8fbc). This file is data. Routes and headings are read from the catalog
// and collections the build publishes in dist/data, which are the same records
// the Astro templates in site/src render, so a page the build stops producing
// is observable as a failure.
//
// Action classes:
// - chrome: skip link, site title and primary navigation (BaseLayout.astro).
// - home calls to action and the search form: declared individually.
// - content links: document cards, tables of contents, related documents and
//   links inside research documents. Each is enumerated at runtime and its
//   destination verified (internal page or fragment resolves; external link
//   is an https URL). Third-party availability is unknown scope.

import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

export const STANDARD = Object.freeze({ id: "visual-engineering/UI-LAYOUT-INTERACTION", version: "1.0.0", source: "kemiller2002/visual-engineering@8da8fbc" });

const DIST = process.env.UI_CONFORMANCE_SITE_DIR ? `${process.env.UI_CONFORMANCE_SITE_DIR.replace(/\/$/, "")}/` : fileURLToPath(new URL("../../dist/", import.meta.url));
const readJson = name => JSON.parse(readFileSync(`${DIST}data/${name}`, "utf8"));

const CATALOG = readJson("research-catalog.json");
const COLLECTIONS = readJson("research-collections.json");

// site/src/pages/collections/[taxonomy]/index.astro
const TAXONOMIES = Object.freeze([
  ["artifact-type", "artifactTypes", "Artifact Types"], ["research-area", "researchAreas", "Research Areas"],
  ["discipline", "disciplines", "Disciplines"], ["tag", "tags", "Tags"], ["status", "statuses", "Statuses"],
  ["project", "projects", "Projects"], ["purpose", "purposes", "Reader Purposes"], ["audience", "audiences", "Audiences"]
]);
const termSlug = key => key.toLowerCase().replace(/[^a-z0-9]+/g, "-");

export const SITE_TITLE = "Visual Engineering Research";
const HOME_HEADING = "Find the thinking behind the work.";

const route = (id, path, heading, kind, current) => ({ id, path, heading, kind, current });

export const ROUTES = Object.freeze([
  route("home", "/", HOME_HEADING, "home", "Overview"),
  route("search", "/search/", "Find research quickly", "search", "Search"),
  ...TAXONOMIES.flatMap(([taxonomy, key, label]) => [
    route(`collections-${taxonomy}`, `/collections/${taxonomy}/`, label, "collection", "Collections"),
    ...Object.keys(COLLECTIONS[key] ?? {}).sort().map(term =>
      route(`collections-${taxonomy}-${termSlug(term)}`, `/collections/${taxonomy}/${termSlug(term)}/`, term, "term", "Collections"))
  ]),
  ...CATALOG.records.map(record => route(`research-${record.slug}`, record.url, record.title, "document", null))
]);

// Every contract width is exercised on one route of each template; every
// route is exercised at 320px and at 320px with 200% text.
const firstOf = kind => ROUTES.find(item => item.kind === kind);
const longestDocument = [...CATALOG.records].sort((a, b) => b.html.length - a.html.length)[0];
export const REPRESENTATIVE = Object.freeze([
  firstOf("home"), firstOf("search"), firstOf("collection"), ROUTES.find(item => item.path === "/collections/tag/"), firstOf("term"),
  ROUTES.find(item => item.path === longestDocument.url),
  ROUTES.find(item => item.path === "/research/c-relationship-catalog/")
].filter(Boolean));

const link = name => ({ role: "link", name });
const headingOf = path => ROUTES.find(item => item.path === path)?.heading ?? null;

const navigate = (id, name, destination, from) =>
  ({ id, control: link(name), kind: "navigate", destination, heading: headingOf(destination), from, intent: `Navigate to ${destination}`, expectedOutcome: `URL becomes ${destination} and the destination's heading "${headingOf(destination)}" is shown` });

const NEUTRAL = "/collections/status/";

export const NAV = Object.freeze([["Overview", "/"], ["Search", "/search/"], ["Collections", "/collections/artifact-type/"]]);

export const CHROME = Object.freeze([
  { id: "skip-link", control: link("Skip to content"), kind: "skip", destination: "#main", from: NEUTRAL, intent: "Move keyboard focus past the header to the page content", expectedOutcome: "The link appears on keyboard focus; activating it changes the hash to #main and the main region is in view" },
  navigate("site-title", SITE_TITLE, "/", NEUTRAL),
  ...NAV.map(([name, path]) => navigate(`nav-${name.toLowerCase()}`, name, path, NEUTRAL))
]);

export const SEARCH_CONTROLS = Object.freeze([
  { role: "searchbox", name: "Query" }, { role: "combobox", name: "Artifact type" },
  { role: "combobox", name: "Status" }, { role: "combobox", name: "Reader purpose" }
]);

export const SURFACES = Object.freeze([
  {
    id: "chrome", path: "/* (shared header and footer on every route)", kind: "catalog",
    actions: CHROME,
    groups: [
      { id: "primary-nav", ruleId: "ULI-L10", relation: "centerline", edge: "center", widths: [320, 390, 768, 1280], peers: NAV.map(([name]) => link(name)), scope: "navigation", rationale: "Primary navigation destinations share one row in source order at every contract width" }
    ]
  },
  {
    id: "home", path: "/", kind: "catalog",
    actions: [
      navigate("home-search", "Search all research", "/search/", "/"),
      navigate("home-browse", "Browse by collection", "/collections/artifact-type/", "/"),
      navigate("home-artifact-types", "View artifact types", "/collections/artifact-type/", "/"),
      navigate("home-research-areas", "View research areas", "/collections/research-area/", "/"),
      navigate("home-purposes", "View reader purposes", "/collections/purpose/", "/"),
      navigate("home-search-index", "Search the full index", "/search/", "/")
    ],
    groups: [
      { id: "hero-actions", ruleId: "ULI-L10", relation: "centerline", edge: "center", widths: [768, 1280], peers: [link("Search all research"), link("Browse by collection")], rationale: "Hero actions share one centred row where both fit" },
      { id: "feature-links", ruleId: "ULI-L10", relation: "top-edge", edge: "top", widths: [1280], peers: [link("View artifact types"), link("View research areas"), link("View reader purposes")], rationale: "The three feature columns end in peer links; columns are equal-height grid cells" }
    ]
  },
  {
    id: "search", path: "/search/", kind: "catalog",
    actions: [
      { id: "search-submit", control: { role: "button", name: "Search" }, kind: "search", intent: "Run a client-side full-text search with the chosen filters", expectedOutcome: "The status reads \"Searching…\" while pending, then the result count; matching documents are listed as links; a failed index load reports an error, never results, and a retry recovers", resetStrategy: "Reload /search/; search is client-side and read-only" }
    ],
    groups: [
      { id: "search-fields-row", ruleId: "ULI-L01", relation: "top-edge", edge: "top", widths: [1280], peers: SEARCH_CONTROLS, rationale: "Query and filter fields share one row above 64rem; equal labels must not stagger the controls" },
      { id: "search-row-bottom", ruleId: "ULI-L01", relation: "bottom-edge", edge: "bottom", widths: [1280], peers: [...SEARCH_CONTROLS, { role: "button", name: "Search" }], rationale: "Fields and the submit button end on one baseline row (align-items: end)" }
    ]
  },
  ...ROUTES.filter(item => item.kind !== "home" && item.kind !== "search").map(item => ({ id: item.id, path: item.path, kind: "catalog", actions: [], groups: [] }))
]);

export const surfaceForRoute = item => SURFACES.find(surface => surface.path === item.path);

export const declaredActionsFor = item => [
  ...CHROME.map(action => ({ ...action, surfaceId: "chrome" })),
  ...(surfaceForRoute(item)?.actions ?? []).map(action => ({ ...action, surfaceId: surfaceForRoute(item).id }))
];
