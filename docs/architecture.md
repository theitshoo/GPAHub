# GPAHub — Architecture

Clean Architecture with four production projects plus a test project. The single rule that governs everything: **dependencies point inward, and the Domain depends on nothing.**

---

## Layers & Responsibilities

| Layer | Allowed dependencies | Responsibilities | Explicitly forbidden |
|-------|----------------------|------------------|----------------------|
| **Domain** | *none* (zero NuGet packages) | Entities with invariants, value objects (`CreditHours`, `GpaValue`, `Money`), pure calculation engines, domain exceptions, business constants | Persistence, HTTP, frameworks, `DateTime.Now` in engines |
| **Application** | Domain (+ FluentValidation, AutoMapper) | Use-case services, DTOs, validators, repository/service **ports**, Result/Error model, premium gating, mapping profiles | EF Core, ASP.NET types, HTTP concepts |
| **Infrastructure** | Application, Domain | EF Core DbContext/configurations/migrations, repository implementations, UnitOfWork, Stripe gateway, PDF rendering, token purge background service, seeding | Business decisions |
| **Web** | All | Controllers, JWT issuance/validation, authorization policies, exception/security middleware, Swagger/CORS, composition root | Business rules |

## Dependency Graph

```text
GPAHub.Web ──► GPAHub.Infrastructure ──► GPAHub.Application ──► GPAHub.Domain
      │                                                        ▲
      └────────────────────────────────────────────────────────┘
                (Web may reference Application directly for DTOs/ports)
```

GPAHub.Tests references all projects.

---

## Key Patterns

### 1. Result model instead of leaked exceptions
Application services return `Result` / `Result<T>`. An `Error(ErrorType, Code, Message)` carries a machine-readable code and human message. The Web layer's `ApiControllerBase.FromResult` maps `ErrorType → HTTP status` (Validation→400, NotFound→404, Unauthorized→401, Forbidden→403, Conflict→409, Failure→500) into RFC 7807 ProblemDetails with an extension `code`.

Domain rule violations throw `DomainException`, which services translate via `DomainResult.ToError` → 409 Conflict. Nothing below Web knows about HTTP.

### 2. Aggregate-protected invariants
Cross-entity rules live on aggregates, not services:

- `GradeScale.AddDefinition / UpdateDefinition / RemoveDefinition` eagerly enforce name uniqueness and range overlap — the collection can never hold an invalid pairwise state.
- Whole-collection properties (full coverage, non-empty) are checked by `GradeScale.EnsureValid()`, which Application must call before activation or save of an active scale.
- `Payment` state transitions are terminal-only; creation goes through the owning aggregate or an explicit factory.
- `Course` input-type exclusivity is enforced by API shape (`CreateNumeric/CreateLetterGrade/UpdateAsNumeric/UpdateAsLetter`) — both/neither is unrepresentable.

### 3. Pure domain engines
All GPA math lives as static, side-effect-free functions over immutable input records:

| Engine | Responsibility |
|--------|----------------|
| `GpaCalculator` | Semester GPA, cumulative blend, zero-hour/freshman guards |
| `TargetGpaCalculator` | Required average (spec §4 steps 1–6), feasibility vs scale max (inclusive boundary), max reachable GPA |
| `GradeCombinationGenerator` | Bounded DFS from highest grades with pruning; caps from `CombinationLimits`; results ordered closest-above-target first |

Services resolve grades through `GradeScale.FindDefinitionForMark/ForGradeName` (inclusive boundaries, case-insensitive) — conversion never trusts client-computed points.

### 4. Scale resolution chain
`ScaleResolver.ResolveAsync` implements one authoritative order used by both calculation and prediction:

```text
custom definitions in request  →  owned scale by id  →  student's active scale  →  system default
```

Guests can only reach custom/system-default branches; `scaleId` without authentication is rejected.

### 5. Premium gating — defense in depth (DR-012)
1. **Service gate:** `TargetGpaService` consults `ISubscriptionService.IsPremiumAsync(studentId)` before combination generation; unauthorized callers receive `Error.Forbidden("premium_required")`.
2. **Endpoint policy:** the `Premium` authorization requirement evaluates live subscription state per request (never a stale token claim).

Both must pass; neither alone is sufficient by design.

### 6. Ownership filtering (IDOR protection)
Repository contracts expose `GetByIdForStudentAsync(id, studentId)` style methods. Services require the authenticated student id (from the JWT `sub` claim via `RequireStudentId()`) and treat misses as 404 — existence of another user's resource is never disclosed.

### 7. Server-side recomputation
Saving a calculation (`calculate-and-save`) always recomputes results from inputs through the same engines used for display; client-reported numbers are ignored by contract.

---

## Request Pipeline (Web)

```text
Request
  │
  ├─ ExceptionHandlingMiddleware   ← last-chance: DomainException→409,
  ├─ SecurityHeadersMiddleware       DbUpdateConcurrencyException→409 (concurrency_conflict),
  │                                  unhandled→500 (detail only in Development)
  ├─ Swagger (Development only)
  ├─ CORS
  ├─ Authentication (JWT Bearer)     sub claim ⇒ studentId
  ├─ RateLimiter                     fixed-window 30 req/min/IP on /api/auth/*
  ├─ Authorization                   [Authorize] + "Premium" policy (live subscription check)
  └─ Controller
       └─ Application service ─► Domain engines / repositories (EF Core)
```

Startup composition (`Program.cs`) applies migrations, seeds reference data, then serves.

---

## Cross-Cutting Decisions

Full rationale lives in [DECISIONS.md](DECISIONS.md). Highlights:

| ID | Topic |
|----|-------|
| DR-001/DR-013 | Full-stack scope; Stripe adapter + manual upgrade path; no fake gateway |
| DR-002 | AutoMapper pinned to OSS 13.0.1 with MaxDepth mitigation |
| DR-004–006, DR-014–016, DR-018–019, DR-022 | Subscription state, scale ownership/coverage semantics, course/semester boundaries, concurrency strategy |
| DR-008/DR-009 | Combination caps; decimal math with AwayFromZero rounding at API edge only |
| DR-011/DR-020 | PBKDF2 hashing; rotating refresh tokens with reuse detection |
| DR-017 | Secrets fail-fast at startup; nothing sensitive committed |
