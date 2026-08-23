# GPAHub — Folder Hierarchy

Complete solution layout. Every folder maps to a responsibility defined in [architecture.md](architecture.md).

```text
GPAHub/
│
├── GPAHub.slnx                          # .NET 10 XML solution (DR-003)
├── docker-compose.yml                   # SQL Server + API, healthchecks, env wiring
├── .env.example                         # Template for compose secrets
├── README.md
│
├── docs/
│   ├── assets/
│   │   └── banner.svg
│   ├── architecture.md                  # Layers, dependency rules, request pipeline
│   ├── database-erd.md                  # ERD + indexes/constraints catalog
│   ├── folder-hierarchy.md              # This file
│   └── Deployment.md                    # Docker Compose production guide
│
├── GPAHub.Domain/                       # ◆ Zero dependencies - pure business core
│   ├── Constants/
│   │   ├── CombinationLimits.cs         # Caps for premium combination search (DR-008)
│   │   ├── FeatureFlags.cs              # Plan capability flags (reference data)
│   │   ├── GpaConstants.cs              # Display decimal places
│   │   ├── MarkRange.cs                 # Absolute mark bounds 0–100
│   │   └── RefreshTokenDefaults.cs      # Refresh lifetime (14 days)
│   ├── DomainServices/                  # Pure calculation engines
│   │   ├── GpaCalculator.cs             # Semester + cumulative GPA (+ result records)
│   │   ├── TargetGpaCalculator.cs       # Required average / feasibility / max reachable
│   │   └── GradeCombinationGenerator.cs # Bounded DFS combination search
│   ├── Entities/
│   │   ├── Student.cs                   # Profile, baseline, password hash
│   │   ├── GradeScale.cs                # Aggregate root: definitions, active flag, EnsureValid()
│   │   ├── GradeDefinition.cs           # Mark range → points; overlap helpers
│   │   ├── Semester.cs                  # Grouping label entity
│   │   ├── Course.cs                    # Numeric/letter input exclusivity via factories
│   │   ├── Subscription.cs              # Type/status/dates + payment collection
│   │   ├── Payment.cs                   # Terminal state machine (Pending→Completed/Failed)
│   │   ├── Plan.cs                      # Free/Premium reference data + feature flags
│   │   ├── GpaRecord.cs                 # Saved calculation header
│   │   ├── GpaRecordCourseLine.cs       # Immutable per-course snapshot line
│   │   ├── TargetPlan.cs                # Saved prediction header
│   │   ├── TargetPlanUpcomingCourse.cs  # Upcoming course snapshot line
│   │   └── RefreshToken.cs              # Hashed refresh token with revocation
│   ├── Enums/
│   │   ├── CalculationType.cs           # Gpa | TargetPrediction
│   │   ├── GradeInputType.cs            # NumericMark | LetterGrade
│   │   ├── PaymentStatus.cs             # Pending | Completed | Failed
│   │   ├── SubscriptionStatus.cs        # Active | Expired
│   │   └── SubscriptionType.cs          # Free | Premium
│   ├── Exceptions/
│   │   ├── DomainException.cs           # Base domain error
│   │   └── InvalidGradeScaleException.cs# Carries Errors list for save-time validation
│   ├── ValueObjects/
│   │   ├── CreditHours.cs               # > 0 enforced
│   │   ├── GpaValue.cs                  # ≥ 0 + AwayFromZero display rounding
│   │   └── Money.cs                     # ≥ 0 amount + ISO-style currency
│   └── GPAHub.Domain.csproj             # No PackageReferences (verified)
│
├── GPAHub.Application/                  # ◆ Use cases — depends only on Domain
│   ├── Common/
│   │   ├── Error.cs / ErrorType.cs      # Typed failures (Validation…Failure)
│   │   ├── Result.cs                    # Result / Result<TValue>
│   │   └── PagedResultDto.cs            # Generic pagination envelope
│   ├── DependencyInjection.cs           # AddApplication(): services + validators + mapper
│   ├── Interfaces/
│   │   ├── Repositories/                # Persistence ports (ownership-safe by design)
│   │   │   ├── ICourseRepository.cs     #   incl. ListBySemesterTrackedAsync (detach path)
│   │   │   ├── IGpaRecordRepository.cs  #   paged lists + ForStudent fetch
│   │   │   ├── IGradeScaleRepository.cs #   active/system-default lookups
│   │   │   ├── IPaymentRepository.cs    #   external-reference lookup
│   │   │   ├── IRefreshTokenRepository.cs # hash lookup + purge operations
│   │   │   ├── ISemesterRepository.cs
│   │   │   ├── IStudentRepository.cs
│   │   │   ├── ISubscriptionRepository.cs
│   │   │   ├── ITargetPlanRepository.cs
│   │   │   └── IUnitOfWork.cs           # SaveChanges abstraction
│   │   └── Services/                    # High-level ports
│   │       ├── IAcademicRecordService.cs
│   │       ├── IAuthService.cs
│   │       ├── ICourseService.cs
│   │       ├── IGpaCalculationService.cs
│   │       ├── IGradeScaleService.cs
│   │       ├── IHistoryService.cs
│   │       ├── IPaymentGateway.cs       # Provider abstraction (Stripe today)
│   │       ├── IPaymentService.cs
│   │       ├── IPremiumActivationService.cs
│   │       ├── IReportService.cs
│   │       ├── ISemesterService.cs
│   │       ├── ISubscriptionService.cs
│   │       ├── ITargetGpaService.cs
│   │       └── ITokenService.cs         # JWT issuance port (implemented in Web)
│   ├── DTOs/
│   │   ├── Course/                      # CourseDto, CourseInputDto, SemesterOptionDto
│   │   ├── Gpa/                         # Calculate request/response, per-course results
│   │   ├── GradeScale/                  # Scale + definition DTOs
│   │   ├── History/                     # Record/plan summaries, details, paging request
│   │   ├── Payments/                    # BeginUpgradeResponseDto
│   │   ├── Report/                      # ReportDto + target section
│   │   ├── Semester/                    # Create/update DTOs
│   │   ├── Student/                     # Profile, auth (register/login/refresh), baseline DTOs
│   │   ├── Subscription/                # Subscription/Payment/UpgradeToPremium DTOs
│   │   └── Target/                      # Prediction request/response + combinations
│   ├── Mappings/
│   │   └── MappingProfile.cs            # Entity↔DTO maps, MaxDepth(32) everywhere (DR-002)
│   ├── Services/
│   │   ├── AcademicRecordService.cs     # Profile + baseline management
│   │   ├── AuthService.cs               # Register/login/refresh/logout, reuse detection
│   │   ├── CourseService.cs             # CRUD + semester attach validation
│   │   ├── DomainResult.cs              # DomainException → Error mapping helpers
│   │   ├── GpaCalculationService.cs     # Stateless/authenticated/save calculations
│   │   ├── GradeScaleService.cs         # CRUD, definitions, activation gating
│   │   ├── HistoryService.cs            # Paged history + deletes
│   │   ├── PaymentService.cs            # Checkout begin + webhook event application
│   │   ├── PremiumActivationService.cs  # Shared "activate premium" logic
│   │   ├── ReportService.cs             # JSON report composition
│   │   ├── ScaleResolver.cs             # custom → owned → active → default chain
│   │   ├── SemesterService.cs           # CRUD + tracked detach on delete
│   │   ├── SubscriptionService.cs       # Manual upgrade/cancel/current
│   │   └── TargetGpaService.cs          # Prediction + premium gate + plan saving
│   ├── Validators/                      # FluentValidation — one per write model,
│   │                                    # stable string error codes
│   └── GPAHub.Application.csproj
│
├── GPAHub.Infrastructure/               # ◆ Persistence & external concerns
│   ├── DependencyInjection.cs           # AddInfrastructure(configuration)
│   ├── Payments/
│   │   ├── StripeOptions.cs (in provider file)
│   │   └── StripePaymentProvider.cs     # Checkout sessions + HMAC webhook verification
│   ├── PdfGeneration/
│   │   └── ReportPdfGenerator.cs        # QuestPDF rendering of ReportDto
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   ├── ConcurrencyConfiguration.cs  # shadow rowversion helper
│   │   │   ├── CourseConfiguration.cs       # + Semester config
│   │   │   ├── GradeScaleConfiguration.cs   # + GradeDefinition config
│   │   │   ├── HistoryConfiguration.cs      # records/lines/plans/upcoming
│   │   │   ├── RefreshTokenConfiguration.cs
│   │   │   ├── StudentConfiguration.cs
│   │   │   └── SubscriptionConfiguration.cs # + Payment + Plan configs
│   │   ├── Migrations/                  # InitialCreate, ProductionHardening, snapshot
│   │   ├── DbSeeder.cs                  # System default scale + Free/Premium plans
│   │   ├── GpaHubDbContext.cs
│   │   ├── GpaHubDbContextFactory.cs    # Design-time factory for `dotnet ef`
│   │   ├── RefreshTokenCleanupService.cs# Daily purge background service
│   │   └── UnitOfWork.cs
│   ├── Repositories/                    # One implementation per Application port
│   │   ├── CourseRepository.cs
│   │   ├── GpaRecordRepository.cs
│   │   ├── GradeScaleRepository.cs
│   │   ├── PaymentRepository.cs
│   │   ├── RefreshTokenRepository.cs
│   │   ├── SemesterRepository.cs
│   │   ├── StudentRepository.cs
│   │   ├── SubscriptionRepository.cs
│   │   └── TargetPlanRepository.cs
│   └── GPAHub.Infrastructure.csproj
│
├── GPAHub.Web/                          # ◆ HTTP host & composition root
│   ├── Auth/
│   │   ├── JwtOptions.cs                # Bound from "Jwt" configuration section
│   │   ├── JwtTokenService.cs           # ITokenService implementation (HS256)
│   │   └── PremiumAuthorizationHandler.cs # Live-subscription "Premium" policy
│   ├── Controllers/
│   │   ├── ApiControllerBase.cs         # Result→HTTP mapping, current-student helpers
│   │   ├── AuthController.cs            # register/login/refresh/logout (rate limited)
│   │   ├── CoursesController.cs
│   │   ├── GpaController.cs             # guest + authenticated + save-to-history
│   │   ├── GradeScalesController.cs     # scales, definitions, activate/deactivate
│   │   ├── HistoryController.cs         # gpa-records & target-plans paging/detail/delete
│   │   ├── PaymentsController.cs        # checkout begin + Stripe webhook (raw body)
│   │   ├── ReportsController.cs         # JSON + PDF reports
│   │   ├── SemestersController.cs
│   │   ├── StudentsController.cs        # profile + baseline
│   │   ├── SubscriptionController.cs    # current/manual upgrade/cancel
│   │   └── TargetController.cs          # predict (optional auth) / predict-and-save
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs # ProblemDetails + concurrency/dev-detail handling
│   │   └── SecurityHeadersMiddleware.cs
│   ├── Properties/launchSettings.json
│   ├── appsettings.json                 # Non-secret defaults (connection string: LocalDB)
│   ├── appsettings.Development.json     # Development JWT secret placeholder
│   ├── Dockerfile                       # Multi-stage build (SDK → ASP.NET runtime)
│   ├── Program.cs                       # Composition, migrations+seed on start, pipeline
│   └── GPAHub.Web.csproj
│
├── GPAHub.Tests/                        # ◆ xUnit + Moq - 314 tests
│   ├── IntegrationTests/
│   │   ├── LocalDbFixture.cs            # Per-test real SQL Server LocalDB database
│   │   ├── GpaHubApiFactory.cs          # WebApplicationFactory w/ isolated DB + config
│   │   ├── ApiTestBase.cs               # Client/token helpers, ProblemDetails reader
│   │   ├── AuthAndSeedIntegrationTests.cs
│   │   ├── AuthEndpointTests.cs
│   │   ├── CalculationAndPremiumEndpointTests.cs
│   │   ├── HardeningEndpointTests.cs    # Rate limiting (isolated factory) + headers
│   │   ├── HistoryAndReportEndpointTests.cs
│   │   ├── OwnershipAndScaleEndpointTests.cs
│   │   ├── PaymentsEndpointTests.cs     # Webhook signature/idempotency/replay
│   │   ├── RepositoryIntegrationTests.cs
│   │   └── SemesterAndPdfEndpointTests.cs
│   ├── UnitTests/
│   │   ├── Domain/                      # Engines, entities, value objects, conversions
│   │   ├── Services/                    # All services w/ Moq'd repositories, DI smoke test
│   │   └── Validators/                  # FluentValidation rule coverage
│   └── GPAHub.Tests.csproj
```

## Layer Rules Recap

| Project may reference | Domain | Application | Infrastructure | Web |
|-----------------------|--------|-------------|----------------|-----|
| **Domain** | — | ✗ | ✗ | ✗ |
| **Application** | ✔ | — | ✗ | ✗ |
| **Infrastructure** | ✔ | ✔ | — | ✗ |
| **Web** | ✔ | ✔ | ✔ | — |

Anything not listed is forbidden by project-reference enforcement - the compiler is the first architecture reviewer.
