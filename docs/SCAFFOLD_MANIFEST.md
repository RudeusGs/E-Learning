# Scaffold Manifest

This file records what the base repository provides before business implementation begins.

## Foundation included

- Solution/project boundaries
- Central .NET package versions
- API bootstrap + health endpoint
- Vue bootstrap + router/store/http infrastructure
- PostgreSQL container
- Dockerfiles and reverse proxy
- Root npm workspace (root package-lock.json present)
- CI uses npm ci
- client Docker build uses npm ci
- foundation checker requires lockfile
- Prettier / ESLint / TypeScript checks
- xUnit test projects with API health smoke + Vitest runner + Testcontainers/Playwright foundations
- Husky hooks
- GitHub Actions CI
- Dependabot
- Project contracts

## Business code intentionally absent

No entity, DbContext, migration, repository, use case, controller, auth handler, page, feature store or feature API service has been generated.

The first implementation slice should follow `docs/PROJECT_SPEC.md` priority.
