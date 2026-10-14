import { describe, expect, it } from "vitest";
import { resolveDocumentLink } from "../../src/content/resolve-link.mjs";

const documentsBySourcePath = new Map([
  ["docs/target.md", { url: "/research/target/" }]
]);

describe("resolveDocumentLink", () => {
  it("maps relative Markdown links to base-aware published URLs", () => {
    const result = resolveDocumentLink({
      sourcePath: "docs/nested/source.md",
      href: "../target.md#finding",
      documentsBySourcePath,
      baseUrl: "/research-publisher/"
    });

    expect(result).toMatchObject({
      href: "/research-publisher/research/target/#finding",
      markdown: true,
      resolved: true,
      resolvedSourcePath: "docs/target.md"
    });
  });

  it("leaves external and same-page links unchanged", () => {
    expect(resolveDocumentLink({
      sourcePath: "docs/source.md",
      href: "https://example.com/source.md",
      documentsBySourcePath
    }).href).toBe("https://example.com/source.md");

    expect(resolveDocumentLink({
      sourcePath: "docs/source.md",
      href: "#finding",
      documentsBySourcePath
    }).href).toBe("#finding");
  });

  it("reports unmatched Markdown targets without inventing a route", () => {
    const result = resolveDocumentLink({
      sourcePath: "docs/source.md",
      href: "missing.md",
      documentsBySourcePath
    });

    expect(result).toMatchObject({
      href: "missing.md",
      markdown: true,
      resolved: false,
      resolvedSourcePath: "docs/missing.md"
    });
  });

  it("points unpublished repository files at the configured source repository", () => {
    const sourceUrl = "https://github.com/example/research/";
    expect(resolveDocumentLink({
      sourcePath: "docs/governance/index.md",
      href: "../../AGENTS.md",
      documentsBySourcePath,
      sourceUrl
    })).toMatchObject({ href: "https://github.com/example/research/blob/HEAD/AGENTS.md", markdown: true, resolved: false });

    expect(resolveDocumentLink({
      sourcePath: "docs/protocol.md",
      href: "../schemas/request.schema.json#/properties",
      documentsBySourcePath,
      sourceUrl
    })).toMatchObject({ href: "https://github.com/example/research/blob/HEAD/schemas/request.schema.json#/properties", markdown: false, resolved: true });
  });

  it("still prefers the published page, and never points outside the repository", () => {
    expect(resolveDocumentLink({
      sourcePath: "docs/source.md",
      href: "target.md",
      documentsBySourcePath,
      sourceUrl: "https://github.com/example/research"
    }).href).toBe("/research/target/");

    expect(resolveDocumentLink({
      sourcePath: "docs/source.md",
      href: "../../outside.json",
      documentsBySourcePath,
      sourceUrl: "https://github.com/example/research"
    }).href).toBe("../../outside.json");

    expect(resolveDocumentLink({
      sourcePath: "docs/source.md",
      href: "/search/",
      documentsBySourcePath,
      sourceUrl: "https://github.com/example/research"
    }).href).toBe("/search/");
  });
});
