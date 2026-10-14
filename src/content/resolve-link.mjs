import path from "node:path";

function withBasePath(baseUrl, targetPath) {
  const base = !baseUrl || baseUrl === "/" ? "/" : `/${baseUrl.replace(/^\/+|\/+$/g, "")}/`;
  const target = targetPath.replace(/^\/+/, "");
  return base === "/" ? `/${target}` : `${base}${target}`;
}

// A repository-relative link to a file the site does not publish (source code,
// a schema, an unpublished Markdown file) would be a dead link on the site.
// When the repository's source URL is configured it points at that file in
// the repository instead; without one it is left unchanged, as before.
function sourceFileHref(sourceUrl, repositoryPath, query, fragment) {
  if (!sourceUrl || repositoryPath.startsWith("../") || repositoryPath === "..") {
    return null;
  }
  return `${sourceUrl.replace(/\/+$/, "")}/blob/HEAD/${repositoryPath}${query}${fragment}`;
}

export function resolveDocumentLink({ sourcePath, href, documentsBySourcePath, baseUrl = "/", sourceUrl = "" }) {
  if (!href || href.startsWith("#") || /^(?:[a-z]+:)?\/\//i.test(href) || /^(?:mailto|tel|data|javascript):/i.test(href)) {
    return { href, markdown: false, resolved: true };
  }

  const match = href.match(/^([^?#]+)(\?[^#]*)?(#.*)?$/);
  if (!match) {
    return { href, markdown: false, resolved: true };
  }

  const [, targetPath, query = "", fragment = ""] = match;
  const resolvedSourcePath = targetPath.startsWith("/")
    ? path.posix.normalize(targetPath.replace(/^\/+/, ""))
    : path.posix.normalize(path.posix.join(path.posix.dirname(sourcePath), targetPath));

  // A root-relative non-Markdown link names a site route, not a repository file.
  if (!/\.md$/i.test(targetPath) && targetPath.startsWith("/")) {
    return { href, markdown: false, resolved: true };
  }

  if (!/\.md$/i.test(targetPath)) {
    return { href: sourceFileHref(sourceUrl, resolvedSourcePath, query, fragment) ?? href, markdown: false, resolved: true };
  }

  const targetDocument = documentsBySourcePath.get(resolvedSourcePath);

  if (!targetDocument) {
    return { href: sourceFileHref(sourceUrl, resolvedSourcePath, query, fragment) ?? href, markdown: true, resolved: false, resolvedSourcePath };
  }

  return {
    href: `${withBasePath(baseUrl, targetDocument.url)}${query}${fragment}`,
    markdown: true,
    resolved: true,
    resolvedSourcePath
  };
}
