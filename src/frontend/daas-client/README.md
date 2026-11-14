# DaaS frontend

## Build locally

From this directory, run `npm ci` and `npm run build`. Vite writes the static site to `dist/`.

## Cloudflare Workers Builds

This frontend is a static Vite site deployed as a Cloudflare Worker with static assets. In the Worker’s Git build settings, set the root directory to `src/frontend/daas-client`, the build command to `npm run build`, and the deploy command to `npx wrangler deploy`. The [`wrangler.jsonc`](./wrangler.jsonc) file points the Worker at `dist/` and enables SPA fallback for client-side routes.

Set the Worker’s `VITE_API_BASE_URL` build variable to the public base URL of the API (for example, `https://your-api-host`). Without it, the frontend makes API requests to its own Cloudflare origin. Configure the API’s allowed CORS origins to include the frontend Worker URL.

Workers Builds runs on commits to its configured branches even when a commit changes only backend files. Add `src/frontend/daas-client/**` as a build watch path in Cloudflare if you want frontend builds to run only when frontend files change. Keep the root directory and build/deploy commands as shown above; the API Docker workflow is separate.

If a Cloudflare build still fails with these settings, use the first error in its build log to identify whether dependency installation, `npm run build`, or `npx wrangler deploy` failed.
