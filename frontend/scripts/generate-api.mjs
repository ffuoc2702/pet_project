import { mkdir, writeFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';
import { resolve } from 'node:path';
import openapiTS, { astToString } from 'openapi-typescript';
const source = process.argv[2];
if (!source) throw new Error('Provide an OpenAPI JSON file path or URL.');
const input = /^https?:\/\//.test(source)
  ? new URL(source)
  : pathToFileURL(resolve(source));
const schema = await openapiTS(input);
await mkdir('src/shared/api/generated', { recursive: true });
await writeFile('src/shared/api/generated/schema.d.ts', astToString(schema));
