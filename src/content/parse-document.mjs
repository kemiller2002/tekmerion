import fs from "node:fs/promises";
import path from "node:path";
import matter from "gray-matter";
import { unified } from "unified";
import remarkParse from "remark-parse";
import remarkGfm from "remark-gfm";
import remarkRehype from "remark-rehype";
import rehypeSanitize from "rehype-sanitize";
import rehypeSlug from "rehype-slug";
import rehypeStringify from "rehype-stringify";
import { visit } from "unist-util-visit";

function rewriteLinks(rewriteLink) {
  return () => (tree) => {
    visit(tree, "link", (node) => {
      node.url = rewriteLink(node.url);
    });
  };
}

const textOf = (node) => (node.type === "text" ? node.value : (node.children ?? []).map(textOf).join(""));

// Records each rendered heading with the id rehype-slug gave it, so a table of
// contents links to the anchor that actually exists instead of re-deriving a
// slug that can disagree with it.
function collectHeadings(target) {
  return () => (tree) => {
    visit(tree, "element", (node) => {
      const depth = /^h([1-6])$/.exec(node.tagName)?.[1];
      if (depth && node.properties?.id) {
        target.push({ depth: Number(depth), text: textOf(node).replace(/\s+/g, " ").trim(), id: String(node.properties.id) });
      }
    });
  };
}

// A wide table scrolls inside its own container rather than widening the page.
function containTables() {
  return () => (tree) => {
    visit(tree, "element", (node, index, parent) => {
      if (node.tagName !== "table" || !parent || parent.properties?.className?.includes("table-scroll")) {
        return;
      }
      parent.children[index] = { type: "element", tagName: "div", properties: { className: ["table-scroll"] }, children: [node] };
    });
  };
}

export async function renderDocumentHtml(markdown, rewriteLink = (url) => url, headings = []) {
  return String(
    await unified()
      .use(remarkParse)
      .use(remarkGfm)
      .use(rewriteLinks(rewriteLink))
      .use(remarkRehype)
      .use(rehypeSanitize)
      .use(rehypeSlug)
      .use(collectHeadings(headings))
      .use(containTables())
      .use(rehypeStringify)
      .process(markdown)
  );
}

export async function parseDocument(projectRoot, relativePath) {
  const absolutePath = path.join(projectRoot, relativePath);
  const raw = await fs.readFile(absolutePath, "utf8");
  const parsed = matter(raw);
  const markdownAst = unified().use(remarkParse).use(remarkGfm).parse(parsed.content);
  const headings = [];
  const links = [];

  visit(markdownAst, (node) => {
    if (node.type === "link") {
      links.push(node.url);
    }
  });

  const html = await renderDocumentHtml(parsed.content, undefined, headings);

  return {
    absolutePath,
    relativePath,
    frontmatter: parsed.data ?? {},
    excerpt: parsed.excerpt ?? "",
    body: parsed.content,
    raw,
    headings,
    links,
    html
  };
}
