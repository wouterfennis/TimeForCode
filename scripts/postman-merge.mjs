// Merges the Postman collections that openapi-to-postmanv2 made from the OpenAPI document of each API into one collection.
// Called by generate-postman-collection.ps1; it needs no packages.
//
// Usage: node scripts/postman-merge.mjs <output file> "<folder name>|<base url variable>|<converted collection>" ...
import { readFileSync, writeFileSync } from 'node:fs';

const [outputFile, ...apiArguments] = process.argv.slice(2);
if (!outputFile || apiArguments.length === 0) {
  console.error('Usage: node scripts/postman-merge.mjs <output file> "<folder>|<base url variable>|<collection file>" ...');
  process.exit(2);
}

// Collection variables the generated requests refer to; the user fills them in Postman.
const variables = new Map([
  ['adminSecret', ''],
  ['token', ''],
]);

// A path variable named "id" belongs to the entity named by the segment before it.
const idVariableBySegment = {
  project: 'projectId',
  donororganization: 'organizationId',
  contributor: 'contributorId',
  donation: 'donationId',
  maintainer: 'maintainerId',
};

function addVariable(name, value = '') {
  if (!variables.has(name)) {
    variables.set(name, value);
  }
}

/** Points the path variables of the request at collection variables, so ids are filled once for every request. */
function bindPathVariables(request) {
  const path = request.url?.path ?? [];
  for (const variable of request.url?.variable ?? []) {
    let name;
    if (variable.key === 'id') {
      const segment = path[path.indexOf(':id') - 1]?.toLowerCase();
      name = idVariableBySegment[segment];
    } else if (/Id$/.test(variable.key)) {
      name = variable.key;
    } else if (variable.key === 'owner') {
      name = 'githubOwner';
    } else if (variable.key === 'repo') {
      name = 'githubRepository';
    }

    if (name) {
      addVariable(name);
      variable.value = `{{${name}}}`;
    }
  }
}

/** The converter fills integer fields with random numeric strings; a 1 is a valid placeholder for hours, page numbers and the like. */
function tidyJsonBody(request) {
  if (request.body?.mode !== 'raw' || !request.body.raw) {
    return;
  }

  try {
    const body = JSON.parse(request.body.raw);
    const tidy = (value) => {
      if (typeof value === 'string' && /^-?\d+$/.test(value)) return 1;
      if (Array.isArray(value)) return value.map(tidy);
      if (value && typeof value === 'object') return Object.fromEntries(Object.entries(value).map(([k, v]) => [k, tidy(v)]));
      return value;
    };
    request.body.raw = JSON.stringify(tidy(body), null, 2);
  } catch {
    // Not JSON: leave the body as the converter made it.
  }
}

/** The tidied item: named "METHOD /path", inheriting the collection authentication, without ids and sample responses. */
function tidyItem(item, baseUrlVariable) {
  if (item.item) {
    return { name: item.name, item: item.item.map((child) => tidyItem(child, baseUrlVariable)) };
  }

  const request = item.request;
  bindPathVariables(request);
  tidyJsonBody(request);
  request.url.host = [`{{${baseUrlVariable}}}`];
  delete request.auth;

  const name = `${request.method} /${request.url.path.join('/')}`;
  const { id, response, ...rest } = item; // the sample responses are in the OpenAPI document; Postman makes new ids on import
  return { ...rest, name, request: { ...request, name: undefined } };
}

const apiFolders = apiArguments.map((argument) => {
  const [folderName, baseUrlVariable, file] = argument.split('|');
  addVariable(baseUrlVariable);
  const collection = JSON.parse(readFileSync(file, 'utf8'));
  return { name: folderName, item: collection.item.map((child) => tidyItem(child, baseUrlVariable)) };
});

const setup = {
  name: 'Setup',
  item: [
    {
      name: 'POST /api/v1/test-admin-access/token (test-only)',
      event: [
        {
          listen: 'test',
          script: {
            type: 'text/javascript',
            exec: ['const token = pm.response.json().token;', 'if (token) { pm.collectionVariables.set(\'token\', token); }'],
          },
        },
      ],
      request: {
        method: 'POST',
        auth: { type: 'noauth' },
        header: [{ key: 'Content-Type', value: 'application/json' }],
        body: { mode: 'raw', raw: '{\n  "secret": "{{adminSecret}}"\n}', options: { raw: { language: 'json' } } },
        url: { raw: '{{authorizationUrl}}/api/v1/test-admin-access/token', host: ['{{authorizationUrl}}'], path: ['api', 'v1', 'test-admin-access', 'token'] },
        description:
          'Not part of the OpenAPI documents: the endpoint is hidden and only answers when the stack was started with TEST_ADMIN_ACCESS_SECRET (ADR-017). Stores the admin access token in the collection variable `token`.',
      },
    },
  ],
};

const defaults = { authorizationUrl: 'http://localhost:8080', donationUrl: 'http://localhost:8082', reportingUrl: 'http://localhost:8083' };
for (const [name, value] of Object.entries(defaults)) {
  variables.set(name, value);
}

const collection = {
  info: {
    name: 'TimeForCode local (all APIs)',
    description:
      'Every endpoint of the Authorization, Donation and Reporting APIs, converted from their OpenAPI documents by scripts/generate-postman-collection.ps1. Fill the collection variables, run the Setup request for an admin token, then the rest. Requests inherit bearer authentication with `{{token}}`; anonymous endpoints ignore it.',
    schema: 'https://schema.getpostman.com/json/collection/v2.1.0/collection.json',
  },
  auth: { type: 'bearer', bearer: [{ key: 'token', value: '{{token}}', type: 'string' }] },
  variable: [...variables].map(([key, value]) => ({ key, value })),
  item: [setup, ...apiFolders],
};

writeFileSync(outputFile, `${JSON.stringify(collection, null, 2)}\n`);
console.log(`Wrote ${outputFile}`);
