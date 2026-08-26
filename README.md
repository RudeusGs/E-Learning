# E-Learning MVP

Production-minded **foundation/skeleton** for a school E-Learning MVP built with **.NET 10, Vue 3 and PostgreSQL**.

> Business features are intentionally not implemented yet. This repository provides the project boundaries, tooling, CI, Docker setup, test foundations and AI-agent contracts needed to implement features incrementally and safely.

## Stack

- Server: ASP.NET Core Web API (.NET 10), EF Core 10, PostgreSQL
- Client: Vue 3, TypeScript strict, Vite, Vue Router, Pinia, Axios, Tailwind CSS
- Tests: xUnit, WebApplicationFactory, Testcontainers PostgreSQL, Vitest, Playwright
- Tooling: npm workspaces, Prettier, ESLint, Husky, GitHub Actions, Dependabot

## Repository

```text
.


├── docs/
├── server/
├── client/
├── e2e/
├── .github/
├── .husky/
├── docker-compose.yml
├── Elearning.sln
└── package.json
```

## Prerequisites

- .NET SDK 10.x
- Node.js 22+
- npm 10+
- Docker with Docker Compose
- Git

## First setup

```bash
cp .env.example .env
npm ci
dotnet tool restore
dotnet restore Elearning.sln
```

`dotnet-ef` is pinned as a local repository tool; do not require a global installation. The root `.env` is consumed by Docker Compose only. Vite defaults already work for local development; copy `client/.env.example` to `client/.env.local` only when you need to override them.

Use `npm install` only when intentionally modifying npm dependencies.

## Run local development

PostgreSQL:

```bash
docker compose up -d postgres
```

Server:

```bash
dotnet run --project server/src/Elearning.Api/Elearning.Api.csproj
```

Client:

```bash
npm run dev:client
```

Defaults:

- Client: `http://localhost:5173`
- API: `http://localhost:8080`
- Health: `http://localhost:8080/api/health`

## Run container skeleton

```bash
docker compose up --build
```

Then verify:

- `http://localhost:5173`
- `http://localhost:5173/api/health`

## Quality

```bash
npm run format:check
npm run verify:client
npm run test:e2e

dotnet restore Elearning.sln
dotnet format Elearning.sln --verify-no-changes --no-restore
dotnet build Elearning.sln -c Release --no-restore
dotnet test Elearning.sln -c Release --no-build
```

Full local verification when .NET is installed:

```bash
npm run verify
```

## Documentation

Start here:

3. [`docs/PROJECT_SPEC.md`](docs/PROJECT_SPEC.md) — MVP product contract
4. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
5. [`docs/SECURITY.md`](docs/SECURITY.md)
6. [`docs/DATABASE.md`](docs/DATABASE.md)
7. [`docs/API_CONTRACT.md`](docs/API_CONTRACT.md)
8. [`docs/TESTING_AND_DOD.md`](docs/TESTING_AND_DOD.md)
9. [`docs/HANDOVER.md`](docs/HANDOVER.md)

## Current state

**SKELETON / FOUNDATION ONLY.** Authentication, database entities/DbContext/migrations, Course, Lesson, Student, Enrollment, Exercise, Progress and business UI are intentionally absent.

The first business slice should be Authentication only after the foundation verification commands pass in the working environment.
