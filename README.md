# GPAHub

**GPAHub** is a production-grade ASP.NET Core Web API for academic planning: configurable grading scales, credit-weighted GPA calculation, target-GPA prediction with feasibility analysis, and a Stripe-billed Premium tier that unlocks grade-combination suggestions and PDF reports.

<p align="center">
  <img src="./docs/assets/banner.svg" alt="GPAHub banner" width="100%" />
</p>

---

## Project Overview

GPAHub gives students (and anonymous guests) a single, secure API to:

- Define **custom grading scales** or use the seeded system default.
- Calculate **semester GPA** and **cumulative GPA** against an academic baseline.
- Run **target-GPA predictions** - required average across upcoming courses, feasibility vs. the scale's maximum, and the maximum reachable GPA when the target is out of reach.
- Generate **grade combinations** (Premium) that satisfy a target, ordered closest-to-target first.
- Persist calculations and plans to **history**, and export them as JSON or **PDF reports**.

The system is built on **Clean Architecture**: every business rule lives in a dependency-free Domain layer, application services orchestrate use cases behind interfaces, and both persistence (EF Core) and HTTP (ASP.NET Core) are replaceable details.

**Engineering highlights**

- TDD throughout — **314 automated tests** (unit + real SQL Server integration + full endpoint suites via `WebApplicationFactory`).
- Pure domain engines (`GpaCalculator`, `TargetGpaCalculator`, `GradeCombinationGenerator`) with hand-verified exact `decimal` math.
- Dual-layer premium enforcement (application service gate **and** authorization policy backed by live subscription state).
- Refresh-token rotation with **reuse detection** (stolen token revokes the entire session family).
- Stripe Checkout + signature-verified webhooks with idempotent subscription activation.
- Optimistic concurrency (`rowversion`) on all mutable tables → clean HTTP 409s.
- RFC 7807 Problem Details, security headers, auth rate limiting, health checks, Docker Compose deployment.

---

## Features

### Calculation & Planning
- Semester GPA (`Σ quality points ÷ Σ credit hours`) with fractional credit support.
- Cumulative GPA blending previous baseline + current semester (freshman-safe at zero hours).
- Mark→grade→points and grade→points conversion through the active scale, inclusive boundaries, case-insensitive lookups.
- Target prediction: required average, achievable/infeasible verdict, maximum reachable GPA.
- Bounded grade-combination search (DFS from highest grades, pruned, capped) — Premium only.
- Guest mode: all calculations work without an account; nothing is persisted.

### Grading Scales
- Full CRUD for scales and definitions; overlap and duplicate-name rules enforced inside the aggregate on every mutation.
- Opt-in full-coverage validation at activation/save time.
- Per-student active scale (single-active guarantee via filtered unique index) plus a seeded system default.

### Accounts & Persistence
- Email/password registration with PBKDF2 hashing (`PasswordHasher`), anti-enumeration login errors.
- JWT access tokens + rotating refresh tokens stored as SHA-256 hashes; logout and family-revocation on reuse.
- Academic baseline management; course/semester CRUD; calculation & plan history with pagination.

### Subscriptions & Payments
- Free/Premium plans; implicit Free representation before any payment exists.
- Stripe Checkout session creation and webhook-driven activation with idempotent replay handling.
- Manual/simulated upgrade path for development and testing.

### Reporting & Platform
- Structured JSON reports and QuestPDF-rendered PDF documents per record/plan.
- Health endpoint with database probe; daily refresh-token purge background service.
- OpenAPI/Swagger UI (Development), security headers, per-IP rate limiting on auth endpoints.

---

## Tech Stack

### Backend

- .NET 10 / ASP.NET Core (controller-based Web API)
- Entity Framework Core 10 (code-first migrations)
- FluentValidation (request validation)
- AutoMapper 13 (entity↔DTO projections)
- ASP.NET Core Rate Limiting, Authorization policies, Health Checks

### Database

- SQL Server (LocalDB for development, containerized 2022 for production)
- Code-first migrations applied automatically at startup

### Infrastructure & Libraries

- Stripe REST API (no SDK — direct integration with HMAC webhook verification)
- QuestPDF (PDF report rendering)
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.Extensions.Identity.Core (PBKDF2 password hashing)
- Swashbuckle.AspNetCore (OpenAPI/Swagger UI)

### Testing

- xUnit + Moq
- EF Core against real SQL Server LocalDB for repository/integration tests
- `WebApplicationFactory` end-to-end endpoint suites

---

## Project Structure

The solution follows Clean Architecture. Dependencies point inward; the Domain has zero package references.

```text
GPAHub/
├── GPAHub.slnx
├── docker-compose.yml
├── .env.example
├── docs/
│   ├── assets/banner.svg
│   ├── architecture.md
│   ├── database-erd.md
│   ├── folder-hierarchy.md
│   └── Deployment.md
└── src projects:
    ├── GPAHub.Domain/               # Entities, value objects, domain engines — no dependencies
    ├── GPAHub.Application/          # Use cases, DTOs, validators, ports, premium gating
    ├── GPAHub.Infrastructure/       # EF Core, repositories, migrations, Stripe, PDF, background jobs
    ├── GPAHub.Web/                  # Controllers, auth plumbing, middleware, composition root
    └── GPAHub.Tests/                # Unit + integration + endpoint test suites
```

**Dependency flow (inward only):**

```text
┌─────────────────────────────────────────────────────────────┐
│                       GPAHub.Web                            │
│   Controllers · Auth/JWT · Middleware · Composition · Docs  │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                   GPAHub.Infrastructure                     │
│   EF Core · Repositories · Migrations · Stripe · PDF · Jobs │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                   GPAHub.Application                        │
│   Services · DTOs · Validators · Ports · Result model       │
└───────────────────────────┬─────────────────────────────────┘
                            │
              ┌─────────────▼──────────────┐
              │        GPAHub.Domain       │
              │  Entities · Engines · VOs  │
              └────────────────────────────┘
```

See [docs/architecture.md](docs/architecture.md) for layer rules and request flow,
[docs/folder-hierarchy.md](docs/folder-hierarchy.md) for the full tree.

---

## Main Modules

| Module | Responsibility |
|--------|----------------|
| **Identity & Authentication** | Registration, login, JWT issuance, refresh-token rotation/reuse detection, logout |
| **Academic Records** | Student profile and academic baseline (current GPA, completed hours) |
| **Grade Scales** | Scale/definition CRUD, active-scale switching, coverage validation, system default |
| **Courses & Semesters** | Course CRUD with mark-or-grade input exclusivity; semester grouping and safe deletion |
| **GPA Calculation** | Stateless guest calculation, authenticated calculation using the stored baseline, save-to-history |
| **Target Prediction** | Required-average prediction, feasibility, max reachable GPA, premium combinations, plan saving |
| **Subscription & Payments** | Plan state, manual upgrade path, Stripe checkout sessions, webhook activation |
| **History & Reports** | Paged history, detail views, JSON reports, PDF export |
| **Platform** | Result/Error model, exception middleware, security headers, rate limiting, seeding, token cleanup |

---

## Authentication Flow

Protected endpoints require a JWT Bearer token. Typical client flow:

1. **Register** - `POST /api/auth/register` creates the account and returns the initial token pair.
2. **Login** - `POST /api/auth/login` returns `{ accessToken, refreshToken }`.
3. **Authorize requests** - send `Authorization: Bearer <accessToken>` on protected routes.
4. **Refresh** - `POST /api/auth/refresh` rotates the pair; the old refresh token becomes invalid.
5. **Reuse detection** - presenting an already-used refresh token revokes *all* sessions for that account.
6. **Logout** - `POST /api/auth/logout` revokes the presented refresh token (idempotent).

Refresh tokens are opaque random values stored server-side as SHA-256 hashes; they never appear in logs or responses after issuance.

---

## GPA Workflows

### Quick calculation (guest — no account needed)

```http
POST /api/gpa/calculate
{
  "courses": [
    { "name": "Math", "creditHours": 3, "inputType": "NumericMark", "numericMark": 90 },
    { "name": "Art",  "creditHours": 3, "inputType": "LetterGrade", "letterGrade": "B" }
  ],
  "customScaleDefinitions": [
    { "name": "A", "minMark": 85, "maxMark": 100, "points": 4 },
    { "name": "B", "minMark": 70, "maxMark": 84,  "points": 3 }
  ]
}
```
→ `200 OK` with `semesterGpa: 3.50`, totals, and per-course breakdown.

Scale resolution order: custom definitions in the request → explicit owned scale (`scaleId`) → student's active scale → system default. Baselines may be supplied inline or (for authenticated users) pulled from the stored profile.

### Target planning

```http
POST /api/target/predict?includeCombinations=true   (authenticated)
```
Returns required average, feasibility, max reachable GPA, and (Premium) concrete grade combinations ordered closest-to-target first.

### Persistence & reports

- `POST /api/gpa/calculate-and-save` recomputes server-side and stores a history record (client results are never trusted).
- `GET /api/history/...`, `GET /api/reports/...` for review; `/pdf` variants download rendered documents.

### Going Premium

1. `POST /api/payments/checkout` → returns a Stripe Checkout URL (payment recorded as Pending).
2. Complete payment on Stripe-hosted page.
3. Stripe calls `POST /api/payments/webhook/stripe`; the signature-verified event activates Premium transactionally. Replays are ignored safely.

A manual/simulated path (`POST /api/subscription/upgrade`) remains available for development without Stripe keys.

---

## Installation

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (Visual Studio default) or any SQL Server 2019+
- Git

### Steps

```bash
git clone <repository-url>
cd GPAHub
dotnet restore
dotnet build -c Release
```

Run the API:

```bash
dotnet run --project GPAHub.Web
```

Default local URL: **http://localhost:5185** (HTTPS: https://localhost:7102).

Migrations apply automatically on startup in Development, along with reference data (system default scale, Free/Premium plans).

### Docker Compose (production-shaped)

```bash
cp .env.example .env      # fill in SQL_SA_PASSWORD, JWT_SECRET, STRIPE_* keys
docker compose up -d --build
curl http://localhost:8080/health
```

Full guide: [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).

---

## Configuration

Local secrets live in user-secrets or environment variables — **never committed**.

| Section | Purpose |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Jwt:Issuer` / `Audience` | Token audience/issuer validation |
| `Jwt:SecretKey` | HS256 signing key (≥32 chars, required at startup) |
| `Jwt:ExpiryMinutes` | Access-token lifetime (default 120) |
| `Stripe:SecretKey` / `WebhookSecret` | Payment provider credentials |
| `Stripe:SuccessUrl` / `CancelUrl` | Post-checkout redirect templates |
| `Cors:AllowedOrigins` | Allowed browser origins |

**Example — set secrets locally:**

```bash
dotnet user-secrets init --project GPAHub.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=GPAHub;Trusted_Connection=True;TrustServerCertificate=True" --project GPAHub.Web
dotnet user-secrets set "Jwt:SecretKey" "<at-least-32-characters-random>" --project GPAHub.Web
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project GPAHub.Web
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project GPAHub.Web
```

In Production, missing or short signing keys cause a fail-fast startup error by design.

---

## Usage

1. Start the API and open **Swagger UI** at `/swagger` (Development).
2. Register via `POST /api/auth/register` and copy `accessToken` + `refreshToken`.
3. Click **Authorize** in Swagger and paste the access token.
4. Try the guest calculation above without any token, then explore scales, courses, semesters, predictions, history, and reports.
5. Simulate Premium locally: `POST /api/subscription/upgrade` (or run the full Stripe flow with test keys).

**Seeded data (first run):** a Standard A–F scale (max 4.0, marked as system default) and the Free/Premium plans.

---

## Database

- **Engine:** SQL Server (LocalDB dev / containerized production)
- **ORM:** Entity Framework Core 10, code-first
- **Migrations:** `GPAHub.Infrastructure/Persistence/Migrations/`
- **Context:** `GpaHubDbContext`

**Common commands:**

```bash
# Apply pending migrations manually (also automatic on startup)
dotnet ef database update --project GPAHub.Infrastructure

# Add a migration after model changes
dotnet ef migrations add <Name> --project GPAHub.Infrastructure --output-dir Persistence/Migrations
```

Entity-relationship diagram and constraint catalog: [docs/database-erd.md](docs/database-erd.md).

Highlights: unique filtered indexes enforce one active scale per student and per-student scale-name uniqueness; CHECK constraints guard mark ranges, credit hours, non-negative points/payments, and subscription date order; `rowversion` optimistic concurrency on every mutable table.

---

## Testing

```bash
dotnet test
```

**314 tests** across three suites:

| Suite | What it covers |
|-------|----------------|
| Unit (Domain/Application) | Exact-value engine math, aggregate invariants and violations, service orchestration with mocked repositories, validators, DI composition |
| Integration (LocalDB) | Repository round-trips, ownership filters, unique/check constraints (including raw-SQL bypass attempts), delete behaviors, pagination, seeder idempotency |
| Endpoint (WebApplicationFactory) | Full HTTP journeys: register→login→refresh/logout, rotation & reuse detection, IDOR guards, free-vs-premium gating, guest mode, scale lifecycle, history/report content, Stripe webhook signature + idempotent activation, rate limiting, security headers |

TDD was used throughout development (tests written before implementation), and every bug found during review has regression protection.

---

## Security

- PBKDF2 password hashing; generic `invalid_credentials` responses prevent user enumeration.
- Short-lived HS256 access tokens; hashed, rotating refresh tokens with family revocation on reuse.
- Ownership filtering built into repository contracts — cross-student reads return 404.
- Premium features gated at the service layer **and** via authorization policy checking live subscription state.
- Per-IP fixed-window rate limiting on authentication endpoints.
- Security headers (`nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `no-store`) and HSTS outside Development.
- RFC 7807 Problem Details everywhere; no stack traces leak outside Development.
- Secrets excluded from source control; startup fails fast on weak/missing signing keys.

---

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/architecture.md) | Layers, dependency rules, request flow, key patterns |
| [Database ERD](docs/database-erd.md) | Entity-relationship diagram and constraint catalog |
| [Folder hierarchy](docs/folder-hierarchy.md) | Complete solution layout |
| [Deployment guide](docs/Deployment.md) | Docker Compose setup, secrets, Stripe walkthrough |

**Interactive docs (Development):** Swagger UI at `/swagger`.

---

## Future Improvements

- Paymob adapter behind the existing `IPaymentGateway` interface.
- CI pipeline (build + test + container publish) on push.
- Redis output caching for hot read endpoints.
- Admin role and dashboard for plan/payment oversight.
- Email confirmation and password-reset flows on top of the existing identity core.
