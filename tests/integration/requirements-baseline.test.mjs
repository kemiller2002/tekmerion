// REQ-TEK is the canonical requirement set (DF-TEK-2026-0001). Its identifiers
// are cited by tests, work items and commits, so their integrity is a contract:
// unique, well-formed, classified, and every earlier requirement traced.
// Evidences TEK-TST-006 and the GH-13 acceptance criteria.

import fs from "node:fs/promises";
import path from "node:path";
import { beforeAll, describe, expect, it } from "vitest";

const root = process.cwd();
const read = (relative) => fs.readFile(path.join(root, relative), "utf8");

const CLASSES = new Set(["now", "slice", "target", "gated", "compat", "deferred", "superseded"]);
const DEFINITION = /^- \*\*(TEK-[A-Z]{3}-\d{3})\*\* `([a-z]+)` — /gm;
const REFERENCE = /TEK-[A-Z]{3}-\d{3}/g;
const QUESTION_HEADING = /^### (OQ-TEK-\d{3}) /gm;

const matches = (text, pattern) => [...text.matchAll(pattern)];

let requirements;
let traceability;
let questions;
let definitions;

beforeAll(async () => {
  requirements = await read("docs/requirements/tekmerion-requirements.md");
  traceability = await read("docs/requirements/traceability.md");
  questions = await read("docs/requirements/open-questions.md");
  definitions = matches(requirements, DEFINITION).map(([, id, cls]) => ({ id, cls }));
});

describe("REQ-TEK identifiers", () => {
  it("defines a non-trivial requirement set", () => {
    expect(definitions.length).toBeGreaterThan(100);
  });

  it("never defines an identifier twice", () => {
    const ids = definitions.map(({ id }) => id);
    expect(ids.filter((id, index) => ids.indexOf(id) !== index)).toEqual([]);
  });

  it("classifies every requirement with a known class", () => {
    expect(definitions.filter(({ cls }) => !CLASSES.has(cls))).toEqual([]);
  });

  it("only references identifiers that are defined", () => {
    const defined = new Set(definitions.map(({ id }) => id));
    const cited = [requirements, traceability, questions].flatMap((text) =>
      matches(text, REFERENCE).map(([id]) => id)
    );
    expect([...new Set(cited.filter((id) => !defined.has(id)))]).toEqual([]);
  });

  it("names the gate of every gated requirement", () => {
    const gatedWithoutGate = matches(requirements, /^- \*\*(TEK-[A-Z]{3}-\d{3})\*\* `gated` —([\s\S]*?)(?=^- \*\*|^#)/gm)
      .filter(([, , body]) => !/OQ-TEK-\d{3}/.test(body))
      .map(([, id]) => id);
    expect(gatedWithoutGate).toEqual([]);
  });

  it("gives a reason for every deferred requirement", () => {
    const deferredWithoutReason = matches(requirements, /^- \*\*(TEK-[A-Z]{3}-\d{3})\*\* `deferred` —([\s\S]*?)(?=^- \*\*|^#)/gm)
      .filter(([, , body]) => !/Reason:/.test(body))
      .map(([, id]) => id);
    expect(deferredWithoutReason).toEqual([]);
  });
});

describe("traceability", () => {
  const V02 = [
    ...["1.1", "1.2", "1.3", "1.4"],
    ...["2.1", "2.2", "2.3", "2.4"],
    ...["3.1", "3.2", "3.3", "3.4", "3.5", "3.6"],
    ...["4.1", "4.2", "4.3", "4.4", "4.5", "4.6"],
    ...["5.1", "5.2", "5.3", "5.4"],
    ...["6.1", "6.2", "6.3", "6.4", "6.5"],
    ...["7.1", "7.2", "7.3", "7.4", "7.5"],
    ...["8.1", "8.2", "8.3"],
    "13"
  ].map((n) => `R${n}`);

  it("maps every REQ-RP-VNEXT 0.2.0 requirement and open question", () => {
    const rows = new Set(matches(traceability, /^\| (R[\d.]+|Q\d) \|/gm).map(([, id]) => id));
    expect([...V02, "Q1", "Q2", "Q3", "Q4"].filter((id) => !rows.has(id))).toEqual([]);
  });

  it("confirms the v0.2 requirement list against the source document", async () => {
    const source = await read("docs/vnext/15-requirements-v0.2.0.md");
    const inSource = new Set(matches(source, /^(R\d+\.\d+) /gm).map(([, id]) => id));
    expect([...inSource].filter((id) => !V02.includes(id))).toEqual([]);
  });

  it("maps every requirement section of issue #11", () => {
    const rows = new Set(matches(traceability, /^\| #11 §(\d+) /gm).map(([, n]) => Number(n)));
    const missing = Array.from({ length: 20 }, (_, i) => i + 1).filter((n) => !rows.has(n));
    expect(missing).toEqual([]);
  });

  it("maps every top-level section of the original draft", () => {
    const rows = traceability.split("\n").filter((line) => line.startsWith("| §"));
    const covered = new Set(rows.map((line) => Number(/^\| §(\d+)/.exec(line)[1])));
    const missing = Array.from({ length: 32 }, (_, i) => i).filter((n) => !covered.has(n));
    expect(missing).toEqual([]);
  });
});

describe("open questions", () => {
  it("gives every question a disposition or an owner", () => {
    const sections = questions.split(/^### /m).slice(1);
    const unanswered = sections
      .filter((section) => /^OQ-TEK-/.test(section))
      .filter((section) => !/`resolved(-provisional)?`/.test(section) && !/\*\*Owner:\*\*/.test(section))
      .map((section) => section.split(" ")[0]);
    expect(unanswered).toEqual([]);
  });

  it("defines every question that the requirements cite", () => {
    const defined = new Set(matches(questions, QUESTION_HEADING).map(([, id]) => id));
    const cited = new Set(
      [requirements, traceability].flatMap((text) => matches(text, /OQ-TEK-\d{3}/g).map(([id]) => id))
    );
    expect([...cited].filter((id) => !defined.has(id))).toEqual([]);
  });
});
