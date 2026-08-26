# E-Learning MVP — Base Repository

Production-minded **skeleton** for a school E-Learning MVP using **.NET 10 + Vue 3 + PostgreSQL**.

> This repository intentionally contains **no business feature implementation**. It provides the project boundaries, development tooling, Docker setup, CI, Husky hooks and AI-agent documentation needed to start feature work safely.

## Stack

- Server: ASP.NET Core Web API, .NET 10, EF Core 10, PostgreSQL, ASP.NET Core Identity packages
- Client: Vue 3, TypeScript, Vite, Vue Router, Pinia, Axios, Tailwind CSS
- Quality: ESLint, Prettier, xUnit + WebApplicationFactory, Testcontainers, Vitest, Playwright, Husky, GitHub Actions
- Local infrastructure: Docker Compose + PostgreSQL

## Repository layout

```text
.


├── .github/                     # CI, Dependabot, PR template
├── .husky/                      # Git hooks
├── server/                      # .NET 10 solution projects
├── client/                      # Vue 3 application shell
├── docs/                       # Product/architecture/security/API/testing contracts
└── docker-compose.yml
```

## Prerequisites

- .NET SDK 10.x
- Node.js 22+
- npm 10+
- Docker Desktop / Docker Engine with Compose plugin
- Git

## First-time setup

```bash
cp .env.example .env
npm ci
dotnet tool restore
dotnet restore Elearning.sln
```

`npm ci` installs the pinned direct dependencies and enables Husky through the root `prepare` script when the repository has Git metadata. Use `npm install` only when intentionally modifying dependencies.

The root `.env` belongs to Docker Compose. Client overrides belong in `client/.env.local` (start from `client/.env.example`). Later server secrets should use environment variables or .NET user-secrets/secret storage rather than a committed `.env`.

## Run locally

Start PostgreSQL:

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

- Client dev server: `http://localhost:5173`
- Server: `http://localhost:8080`
- Health check: `http://localhost:8080/api/health`
- PostgreSQL: `localhost:5432`

The Vite dev server proxies `/api` to the ASP.NET Core server.

## Run the entire container skeleton

```bash
docker compose up --build
```

Then open `http://localhost:5173`.

## Quality commands

```bash
npm run format:check
npm run lint:client
npm run typecheck:client
npm run test:client
npm run build:client

dotnet build Elearning.sln -c Release
dotnet test Elearning.sln -c Release
```

Full verification:

```bash
npm run verify
```

## Business implementation status

Nothing below is implemented yet by design:

- Authentication / authorization
- Course / Lesson
- Student / Enrollment
- Exercise / Question
- Learning path
- Lesson progress / Course progress
- Admin reports
- EF Core DbContext / migrations / seed

## Start feature work

1. Read `AGENTS.md`.
2. Read module docs if applicable.
3. Read the relevant contracts under `docs/`.
4. Implement one vertical slice only.
5. Add tests when the slice is actually proven.

Do not treat this skeleton as evidence that business behavior already exists.
