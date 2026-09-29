// Measures the first-slice baseline (GH-19, TEK-PRF-001). Records numbers; it
// does not judge them. Thresholds are to be set from recorded baselines, not
// guessed.
//
//   dotnet build src/Tekmerion.Cli -c Release
//   node scripts/tekmerion-baseline.mjs [runs]
//
// Prints one JSON document to stdout.

import { execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";

const root = path.resolve(path.dirname(new URL(import.meta.url).pathname), "..");
const cli = path.join(root, "src/Tekmerion.Cli/bin/Release/net8.0/tekmerion.dll");
const config = path.join(root, "tests/fixtures/visual-engineering/tekmerion.config.json");
const forma = path.join(root, "node_modules/@echelon-foundry/design-system/dist/all.css");
const runs = Number(process.argv[2] ?? 5);
const out = fs.mkdtempSync(path.join(os.tmpdir(), "tekmerion-baseline-"));

const size = (relative) => fs.statSync(path.join(out, relative)).size;
const treeBytes = (dir) =>
  fs.readdirSync(dir, { withFileTypes: true }).reduce((sum, entry) => {
    const full = path.join(dir, entry.name);
    return sum + (entry.isDirectory() ? treeBytes(full) : fs.statSync(full).size);
  }, 0);
const fileCount = (dir) =>
  fs.readdirSync(dir, { withFileTypes: true }).reduce((n, e) => n + (e.isDirectory() ? fileCount(path.join(dir, e.name)) : 1), 0);

const durations = [];
for (let run = 0; run < runs; run++) {
  const start = process.hrtime.bigint();
  execFileSync("dotnet", [cli, "ingest", "--config", config, "--out", path.join(out, "site"), "--forma", forma, "--json"], { stdio: ["ignore", "ignore", "ignore"] });
  durations.push(Number(process.hrtime.bigint() - start) / 1e6);
}

const site = (relative) => size(path.join("site", relative));
const sorted = [...durations].sort((a, b) => a - b);

console.log(
  JSON.stringify(
    {
      measuredAt: new Date().toISOString(),
      machine: { platform: process.platform, arch: process.arch, cpus: os.cpus().length, cpuModel: os.cpus()[0]?.model ?? null, node: process.version },
      corpus: "tests/fixtures/visual-engineering (visual-engineering@7ef65a3, 23 documents + 55 frontier records)",
      runs,
      ingestWallMs: { min: Math.round(sorted[0]), median: Math.round(sorted[Math.floor(runs / 2)]), max: Math.round(sorted[runs - 1]), note: "full process: .NET start-up, read, ingest, render, stage, verify, promote" },
      output: { files: fileCount(path.join(out, "site")), bytes: treeBytes(path.join(out, "site")) },
      indexes: {
        manifest: site("data/v1/manifest.json"),
        artifacts: site("data/v1/artifacts.json"),
        edges: site("data/v1/edges.json"),
        findings: site("data/v1/findings.json"),
      },
      boundedRetrieval: { artifact: "RP-COMP-005", fetches: 2, bytes: site("data/v1/artifact/a/RP-COMP-005.json") + site("data/v1/edges/a/RP-COMP-005.json") },
      browserPayload: {
        "a/RP-COMP-005/ (html + css, no scripts)": site("a/RP-COMP-005/index.html") + site("assets/forma/all.css"),
        "p/composition-science/ (html + css)": site("p/composition-science/index.html") + site("assets/forma/all.css"),
        stylesheet: site("assets/forma/all.css"),
        scripts: 0,
      },
    },
    null,
    2,
  ),
);
fs.rmSync(out, { recursive: true, force: true });
