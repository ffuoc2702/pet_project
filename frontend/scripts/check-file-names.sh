#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
node --input-type=module - "$@" <<'NODE'
import { readdir, readFile } from 'node:fs/promises';
import { join, posix } from 'node:path';
import ts from 'typescript';

const root = process.cwd();
const ignoredDirectories = new Set([
  'node_modules',
  'dist',
  '.cache',
  'obj',
  'bin',
  'test-results',
  'playwright-report',
  '.git',
  '.vite',
]);
const standardNames = new Set(['AGENTS.md', 'README.md']);
const files = [];
const issues = [];

async function collect(directory, relative = '') {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const path = posix.join(relative, entry.name);
    if (entry.isDirectory() && !ignoredDirectories.has(entry.name)) {
      await collect(join(directory, entry.name), path);
    } else if (entry.isFile()) {
      files.push(path);
    }
  }
}

function expectedConvention(path) {
  const name = posix.basename(path);
  if (standardNames.has(name)) return null;
  return {
    pattern: /^\.?[a-z][a-z0-9]*(?:[-.][a-z0-9]+)*$/,
    label: 'filename kebab-case with standard extension suffixes',
  };
}

await collect(root);
const exactPaths = new Set(files);
const pathsIgnoringCase = new Map(
  files.map((path) => [path.toLowerCase(), path]),
);

function checkModule(file, specifier) {
  if (!specifier.startsWith('.') && !specifier.startsWith('@/')) return;
  const path = specifier.startsWith('@/')
    ? 'src/' + specifier.slice(2)
    : posix.normalize(posix.join(posix.dirname(file), specifier));
  const base = path.split(/[?#]/)[0];
  const candidates = [
    base,
    ...['.ts', '.tsx', '.js', '.mjs'].map((extension) => base + extension),
    base + '/index.ts',
    base + '/index.tsx',
  ];
  if (candidates.some((candidate) => exactPaths.has(candidate))) return;
  const actual = candidates
    .map((candidate) => pathsIgnoringCase.get(candidate.toLowerCase()))
    .find(Boolean);
  issues.push(
    actual
      ? `${file}: import ${specifier} has incorrect casing; actual file: ${actual}`
      : `${file}: local import ${specifier} cannot be resolved`,
  );
}

for (const file of files) {
  const convention = expectedConvention(file);
  if (convention && !convention.pattern.test(posix.basename(file))) {
    issues.push(`${file}: expected ${convention.label}`);
  }
  if (!/\.(ts|tsx|js|mjs)$/.test(file)) continue;
  const source = ts.createSourceFile(
    file,
    await readFile(join(root, file), 'utf8'),
    ts.ScriptTarget.Latest,
    true,
  );
  function visit(node) {
    if (
      (ts.isImportDeclaration(node) || ts.isExportDeclaration(node)) &&
      node.moduleSpecifier &&
      ts.isStringLiteral(node.moduleSpecifier)
    ) {
      checkModule(file, node.moduleSpecifier.text);
    } else if (
      ts.isCallExpression(node) &&
      node.expression.kind === ts.SyntaxKind.ImportKeyword &&
      node.arguments[0] &&
      ts.isStringLiteral(node.arguments[0])
    ) {
      checkModule(file, node.arguments[0].text);
    } else if (
      ts.isImportTypeNode(node) &&
      ts.isLiteralTypeNode(node.argument) &&
      ts.isStringLiteral(node.argument.literal)
    ) {
      checkModule(file, node.argument.literal.text);
    }
    ts.forEachChild(node, visit);
  }
  visit(source);
}

if (issues.length) {
  process.stderr.write(issues.join('\n') + '\n');
  process.exitCode = 1;
} else {
  process.stdout.write(
    `Checked ${files.length} frontend files: naming and local import casing are valid.\n`,
  );
}
NODE
